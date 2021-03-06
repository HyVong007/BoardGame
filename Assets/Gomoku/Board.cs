using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;


namespace BoardGames.Gomoku
{
	public sealed class Board : MonoBehaviour, ITurnListener
	{
		public struct Config
		{
			public Core core;
			public Symbol?[][] mailBox;
			public Vector2Int size;
		}


		[SerializeField] private Button button;
		[SerializeField] private Tilemap grid, pieceMap;
		[SerializeField] private TileBase tileGrid;
		[SerializeField] private SerializableDictionaryBase<Symbol, Piece> pieces;
		public static Board instance { get; private set; }
		public Core core { get; private set; }
		private void Awake()
		{
			instance = instance ? throw new Exception() : this;
			var config = "BOARD_CONFIG".GetValue<Config>();
			core = config.core ?? (config.mailBox != null ? new Core(config.mailBox) : new Core(config.size));

			var rect = core.rect;
			button.transform.localScale = new Vector3(rect.width, rect.height);
			button.click += OnPlayerClick;
			Camera.main.transform.position = new Vector3(rect.width / 2f, rect.height / 2f, -10);
			grid.origin = pieceMap.origin = Vector3Int.zero;
			grid.size = pieceMap.size = new Vector3Int(rect.width, rect.height, 0);
			grid.FloodFill(Vector3Int.zero, tileGrid);

			if (config.core != null || config.mailBox != null)
			{
				Vector3Int index = default;
				for (index.x = 0; index.x < rect.width; ++index.x)
					for (index.y = 0; index.y < rect.height; ++index.y)
					{
						var symbol = core[index.x, index.y];
						if (symbol != null) pieceMap.SetTile(index, pieces[symbol.Value]);
					}
			}

			if (config.core != null)
			{
				config.core = null;
				"BOARD_CONFIG".SetValue(config);
			}
		}


		[SerializeField] private SerializableDictionaryBase<int, Sprite> playerID_sprite;
		private void Start()
		{
			var t = TurnManager.instance;
			t.AddListener(this);
			t.IsGameOver += () => core.state != Core.State.Normal;

			if (t is OfflineTurnManager)
			{
				var ui = OfflineChessBoardUI.instance;
				ui.SetPlayerSprites(playerID_sprite);

				ui.buttonSave.click += _ =>
					  File.WriteAllLines($"{Application.persistentDataPath}/SaveData.txt", new string[]
					  {
						  new OfflineTurnManager.SaveData(t as OfflineTurnManager).ToJson(),
						  core.ToJson()
					  });

				ui.buttonLoad.click += _ =>
				  {
					  var lines = File.ReadAllLines($"{Application.persistentDataPath}/SaveData.txt");
					  "TURN_SAVE_DATA".SetValue(lines[0].FromJson<OfflineTurnManager.SaveData>());
					  var c = new Config { core = lines[1].FromJson<Core>() };
					  "BOARD_CONFIG".SetValue(c);
					  SceneManager.LoadScene("Test");
				  };
			}
			else
			{
				var ui = OnlineChessTableUI.instance;
				ui.SetPlayerSprite(playerID_sprite);
			}
		}


		private async void OnPlayerClick(Vector2 pixel)
		{
			var index = Camera.main.ScreenToWorldPoint(pixel).ToVector2Int();
			if (core[index.x, index.y] != null) return;

			button.interactable = false;
			var t = TurnManager.instance;
			await t.Play(new Core.MoveData((Symbol)t.currentPlayerID, index), true);
		}


		#region Listen
		public void OnTurnBegin()
		{
			button.interactable = TurnManager.instance.CurrentPlayerIsLocalHuman();
		}


		[SerializeField] private Transform flag;
		public async UniTask OnPlayerMove(IMoveData moveData, History.Mode mode)
		{
			var data = moveData as Core.MoveData;
			core.Move(data, mode);

			if (mode != History.Mode.Undo)
			{
				pieceMap.SetTile(data.index.ToVector3Int(), pieces[(Symbol)data.playerID]);
				flag.position = data.index.ToVector3();
			}
			else
			{
				pieceMap.SetTile(data.index.ToVector3Int(), null);
				var t = TurnManager.instance;
				flag.position = t.moveCount != 0 ? (t[t.moveCount - 1] as Core.MoveData).index.ToVector3() : new Vector3(-1, -1);
			}
		}


		[SerializeField] private ObjectPool<LineRenderer> linePool;
		public void OnGameOver()
		{
			if (core.state != Core.State.Draw)
			{
				foreach (var positions in core.winLines) linePool.Get().SetPositions(positions);
			}
		}


		public void OnPlayerQuit(int playerID)
		{
		}


		public async UniTask<bool> OnReceiveRequest(int playerID, Request request)
		{
			return true;
		}


		public void OnTurnEnd(bool isTimeOver)
		{
		}
		#endregion
	}
}