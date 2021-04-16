using BoardGames.Gomoku;
using Cysharp.Threading.Tasks;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using UnityEngine;
using UnityEngine.Tilemaps;


namespace BoardGames.GOChess
{
	public sealed class Board : MonoBehaviour, ITurnListener
	{
		public sealed class Config
		{
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
			core = config.mailBox != null ? new Core(config.mailBox, DrawPieceGUI, ClearPieceGUI) : new Core(config.size, DrawPieceGUI, ClearPieceGUI);
			var rect = core.rect;
			backgroundMap.size = gridMap.size = new Vector3Int(rect.width - 1, rect.height - 1, 0);
			pieceMap.size = new Vector3Int(rect.width, rect.height, 0);
			backgroundMap.origin = gridMap.origin = pieceMap.origin = Vector3Int.zero;
			backgroundMap.FloodFill(Vector3Int.zero, backgroundTile);
			gridMap.FloodFill(Vector3Int.zero, gridTile);
			button.transform.localScale = new Vector3(rect.width, rect.height);
			button.transform.localPosition = new Vector3(rect.width / 2f, rect.height / 2f);
			button.click += OnPlayerClick;
			Camera.main.transform.position = new Vector3(rect.width / 2f, rect.height / 2f, -10);
		}


		private void Start()
		{
			var t = TurnManager.instance;
			t.AddListener(this);
			t.IsGameOver += () => core.state != null;
		}


		private async void OnPlayerClick(Vector2 pixel)
		{
			var index = Camera.main.ScreenToWorldPoint(pixel).ToVector2Int();
			var t = TurnManager.instance;
			if (!core.CanMove((Color)t.currentPlayerID, index)) return;

			button.interactable = false;
			await t.Play(core.GenerateMoveData((Color)t.currentPlayerID, index), true);
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

		public void OnPlayerTimeOver(int playerID)
		{
		}

		public async UniTask<bool> OnReceiveRequest(int playerID, Request request)
		{
			throw new NotImplementedException();
		}



		public void OnTurnEnd()
		{
		}

		public void OnTurnTimeOver()
		{
		}
		#endregion
	}
}