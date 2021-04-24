using Cysharp.Threading.Tasks;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;


namespace BoardGames.GOChess
{
	public sealed class Board : MonoBehaviour, ITurnListener
	{
		public struct Config
		{
			public Core core;
			public Color?[][] mailBox;
			public Vector2Int size;
		}


		[SerializeField] private Button button;
		[SerializeField] private Tilemap backgroundMap, gridMap, pieceMap;
		[SerializeField] private TileBase backgroundTile, gridTile;
		public Core core { get; private set; }
		public static Board instance { get; private set; }
		private void Awake()
		{
			instance = instance ? throw new Exception() : this;
			var config = "BOARD_CONFIG".GetValue<Config>();
			core = Core.main = config.core ??
				(config.mailBox != null ? new Core(config.mailBox)
				: new Core(config.size));
			var rect = core.rect;

			#region Vẽ quân cờ
			if (config.core != null || config.mailBox != null)
			{
				Vector3Int index = default;
				for (index.x = 0; index.x < rect.width; ++index.x)
					for (index.y = 0; index.y < rect.height; ++index.y)
					{
						var color = core[index.x, index.y]?.color;
						if (color != null) DrawPieceGUI(index, color.Value);
					}
			}
			#endregion

			core.drawPieceGUI += DrawPieceGUI;
			core.clearPieceGUI += ClearPieceGUI;
			backgroundMap.size = gridMap.size = new Vector3Int(rect.width - 1, rect.height - 1, 0);
			pieceMap.size = new Vector3Int(rect.width, rect.height, 0);
			backgroundMap.origin = gridMap.origin = pieceMap.origin = Vector3Int.zero;
			backgroundMap.FloodFill(Vector3Int.zero, backgroundTile);
			gridMap.FloodFill(Vector3Int.zero, gridTile);
			button.transform.localScale = new Vector3(rect.width, rect.height);
			button.transform.localPosition = new Vector3(rect.width / 2f, rect.height / 2f);
			button.click += OnPlayerClick;
			Camera.main.transform.position = new Vector3(rect.width / 2f, rect.height / 2f, -10);

			if (config.core != null)
			{
				config.core = null;
				"BOARD_CONFIG".SetValue(config);
			}
		}


		private void OnDisable() => Core.main = null;


		[SerializeField] private SerializableDictionaryBase<int, Sprite> playerID_sprite;
		private void Start()
		{
			var t = TurnManager.instance;
			t.AddListener(this);
			t.IsGameOver += () => core.state != null;

			if (t is OfflineTurnManager)
			{
				var ui = OfflineChessBoardUI.instance;
				ui.SetPlayerSprites(playerID_sprite);

				ui.buttonSave.click += _ =>
				{
					var turnData = new OfflineTurnManager.SaveData(t as OfflineTurnManager)
					{
						history = new History()
					};

					File.WriteAllLines($"{Application.persistentDataPath}/SaveData.txt", new string[]
					{
						turnData.ToJson(),
						 core.ToJson()
					});
				};

				ui.buttonLoad.click += _ =>
				{
					var lines = File.ReadAllLines($"{Application.persistentDataPath}/SaveData.txt");
					"TURN_SAVE_DATA".SetValue(lines[0].FromJson<OfflineTurnManager.SaveData>());
					var c = new Config { core = lines[1].FromJson<Core>() };
					"BOARD_CONFIG".SetValue(c);
					SceneManager.LoadScene("Test");
				};
			}
		}


		private async void OnPlayerClick(Vector2 pixel)
		{
			var index = Camera.main.ScreenToWorldPoint(pixel).ToVector2Int();
			var t = TurnManager.instance;
			if (!core.CanMove((Color)t.currentPlayerID, index)) return;

			button.interactable = false;
			await t.Play(new Core.MoveData(core, (Color)t.currentPlayerID, index), true);
		}


		[SerializeField] private SerializableDictionaryBase<Color, PieceGUI> pieceTiles;
		private void DrawPieceGUI(Vector3Int index, Color color)
		{
			pieceMap.SetTile(index, pieceTiles[color]);
		}


		private void ClearPieceGUI(Vector3Int index)
		{
			pieceMap.SetTile(index, null);
		}


		#region Listener
		public void OnTurnBegin()
		{
			button.interactable = TurnManager.instance.CurrentPlayerIsLocalHuman();
		}


		[SerializeField] private Transform flag;
		public async UniTask OnPlayerMove(IMoveData moveData, History.Mode mode)
		{
			var data = moveData as Core.MoveData;
			core.Move(data, mode);

			if (mode != History.Mode.Undo) flag.position = data.index.ToVector3();
			else
			{
				var t = TurnManager.instance;
				flag.position = t.moveCount != 0 ? (t[t.moveCount - 1] as Core.MoveData).index.ToVector3() : new Vector3(-1, -1);
			}
		}


		public void OnGameOver()
		{
		}


		public void OnPlayerQuit(int playerID)
		{
		}

		public async UniTask<bool> OnReceiveRequest(int playerID, Request request)
		{
			throw new NotImplementedException();
		}


		public void OnTurnEnd(bool isTimeOver)
		{
		}
		#endregion
	}
}