using Cysharp.Threading.Tasks;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using UnityEngine;


namespace BoardGames.ChineseChess
{
	[RequireComponent(typeof(Button), typeof(BoxCollider2D))]
	public sealed class Board : MonoBehaviour, ITurnListener
	{
		public sealed class Config
		{
			public Piece?[][] mailBox;
		}


		private Button button;

		[Serializable]
		private sealed class PieceGUIArray
		{
			[SerializeField] private PieceGUI[] pieces;


			public PieceGUI Show(Vector3 position)
			{
				foreach (var piece in pieces)
					if (!piece.gameObject.activeSelf)
					{
						piece.transform.position = position;
						piece.gameObject.SetActive(true);
						return piece;
					}
				return null;
			}
		}
		[Serializable]
		private sealed class PieceName_ListPieceGUI : SerializableDictionaryBase<PieceName, PieceGUIArray> { }
		[SerializeField] private SerializableDictionaryBase<Color, PieceName_ListPieceGUI> pieces;

		public Core core { get; private set; }
		private readonly PieceGUI[][] mailBox = new PieceGUI[9][];
		public static Board instance { get; private set; }
		private void Awake()
		{
			instance = instance ? throw new Exception() : this;
			var config = "BOARD_CONFIG".GetValue<Config>();
			core = new Core(config.mailBox);
			for (int x = 0; x < 9; ++x)
			{
				mailBox[x] = new PieceGUI[10];
				for (int y = 0; y < 10; ++y)
				{
					var p = core[x, y];
					if (p == null) continue;
					(mailBox[x][y] = pieces[p.Value.color][p.Value.name].Show(new Vector3(x, y))).hidden = p.Value.hidden;
				}
			}
			(button = GetComponent<Button>()).beginDrag += BeginDrag;
		}


		private void Start()
		{
			var t = TurnManager.instance;
			t.AddListener(this);
			t.IsGameOver += () => core.GetState(Color.Red) == Core.State.CheckMate || core.GetState(Color.Black) == Core.State.CheckMate;
		}


		[SerializeField] private Transform cellFlag;
		[SerializeField] private ObjectPool<Transform> hintPool;
		private bool BeginDrag(Vector2 pixel)
		{
			var from = Convert(pixel);
			if (core[from.x, from.y] == null) return false;

			var moves = core.FindLegalMoves(from.x, from.y);
			if (moves.Length == 0) return false;

			// Tô màu các ô có thể đi
			foreach (var move in moves) hintPool.Get(move.ToVector3());
			var t = TurnManager.instance;
			if (!currentPlayerIsLocalHuman || (int)core[from.x, from.y].Value.color != t.currentPlayerID)
			{
				button.endDrag += _;
				return true;

				void _(Vector2 __)
				{
					hintPool.Recycle();
					button.endDrag -= _;
				}
			}

			var piece = mailBox[from.x][from.y];
			piece.IncreaseSortOrder();
			button.dragging += dragging;
			button.endDrag += endDrag;
			return true;


			bool dragging(Vector2 _)
			{
				var pos = Camera.main.ScreenToWorldPoint(_);
				pos.z = 0;
				var f = Vector3Int.FloorToInt(pos);
				if (0 <= f.x && f.x < 9 && 0 <= f.y && f.y < 10) cellFlag.position = f;
				pos.x -= 0.5f; pos.y -= 0.5f;
				piece.transform.position = pos;
				return true;
			}


			async void endDrag(Vector2 _)
			{
				hintPool.Recycle();
				button.dragging -= dragging;
				button.endDrag -= endDrag;
				piece.DecreaseSortOrder();
				cellFlag.position = new Vector3(-1, -1);
				var to = Convert(_);
				if (!moves.Contains(to))
				{
					button.beginDrag -= BeginDrag;
					await piece.transform.Move(from.ToVector3(), 0.15f);
					button.beginDrag += BeginDrag;
					return;
				}

				currentPlayerIsLocalHuman = false;
				await t.Play(core.GenerateMoveData(from, to), true);
			}


			Vector2Int Convert(Vector2 _) => Vector2Int.FloorToInt(Camera.main.ScreenToWorldPoint(_));
		}


		#region Listen
		private bool currentPlayerIsLocalHuman;
		public void OnTurnBegin() => currentPlayerIsLocalHuman = TurnManager.instance.CurrentPlayerIsLocalHuman();


		public void OnTurnEnd(bool isTimeOver) => currentPlayerIsLocalHuman = false;


		[SerializeField] private Transform moveTarget;
		[SerializeField] private float pieceMoveSpeed;
		public async UniTask OnPlayerMove(IMoveData moveData, History.Mode mode)
		{
			var data = moveData as Core.MoveData;
			core.Move(data, mode);

			if (mode != History.Mode.Undo)
			{
				#region DO
				var piece = mailBox[data.from.x][data.from.y];
				mailBox[data.from.x][data.from.y] = null;
				piece.IncreaseSortOrder();

				// Nếu là AI hoặc Remote thì tô màu đích đến trước khi di chuyển quân cờ
				if (!TurnManager.instance.CurrentPlayerIsLocalHuman()) moveTarget.position = data.to.ToVector3();

				await piece.transform.Move(data.to.ToVector3(), pieceMoveSpeed);
				piece.DecreaseSortOrder();

				var opponent = mailBox[data.to.x][data.to.y];
				if (opponent) opponent.gameObject.SetActive(false);
				(mailBox[data.to.x][data.to.y] = piece).hidden = false;
				moveTarget.position = data.to.ToVector3();
				#endregion
			}
			else
			{
				#region UNDO
				var piece = mailBox[data.to.x][data.to.y];
				if (data.capturedPiece != null)
				{
					var opponent = data.capturedPiece.Value;
					(mailBox[data.to.x][data.to.y] = pieces[opponent.color][opponent.name].Show(data.to.ToVector3()))
						.hidden = opponent.hidden;
				}
				else mailBox[data.to.x][data.to.y] = null;

				await piece.transform.Move(data.from.ToVector3(), pieceMoveSpeed);
				(mailBox[data.from.x][data.from.y] = piece).hidden = core[data.from.x, data.from.y].Value.hidden;
				moveTarget.position = data.from.ToVector3();
				#endregion
			}
		}


		public void OnGameOver()
		{
		}


		public void OnPlayerQuit(int playerID)
		{
			throw new NotImplementedException();
		}


		public async UniTask<bool> OnReceiveRequest(int playerID, Request request)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}