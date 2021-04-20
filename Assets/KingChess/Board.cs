using Cysharp.Threading.Tasks;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using UnityEngine;


namespace BoardGames.KingChess
{
	[RequireComponent(typeof(Button), typeof(BoxCollider2D))]
	public sealed class Board : MonoBehaviour, ITurnListener
	{
		public sealed class Config
		{
			public (Color playerID, PieceName name)?[][] mailBox;
		}


		private Button button;
		[Serializable] private sealed class PieceName_Pieces : SerializableDictionaryBase<PieceName, ObjectPool<Piece>> { }
		[SerializeField] private SerializableDictionaryBase<Color, PieceName_Pieces> pieces;
		public Core core { get; private set; }
		private readonly Piece[][] mailBox = new Piece[8][];
		public static Board instance { get; private set; }
		private void Awake()
		{
			instance = instance ? throw new Exception() : this;
			var config = "BOARD_CONFIG".GetValue<Config>();
			core = new Core(config.mailBox);
			for (int x = 0; x < 8; ++x)
			{
				mailBox[x] = new Piece[8];
				for (int y = 0; y < 8; ++y)
				{
					var p = core[x, y];
					if (p != null) mailBox[x][y] = pieces[p.Value.color][p.Value.name].Get(new Vector3(x, y));
				}
			}

			(button = GetComponent<Button>()).beginDrag += BeginDrag;
		}


		private void Start()
		{
			var t = TurnManager.instance;
			t.AddListener(this);
			t.IsGameOver += () => core.GetState(Color.White) == Core.State.CheckMate || core.GetState(Color.Black) == Core.State.CheckMate;
		}


		[SerializeField] private Transform cellFlag;
		[SerializeField] private ObjectPool<Transform> hintPool;
		private bool BeginDrag(Vector2 pixel)
		{
			var from = Convert(pixel);
			if (core[from.x, from.y] == null) return false;

			var moves = core.FindLegalMoves(from);
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
				if (0 <= f.x && f.x < 8 && 0 <= f.y && f.y < 8) cellFlag.position = f;
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
				await t.Play(await core.GenerateMoveData(from, to), true);
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

			var from = data.from.ToMailBoxIndex();
			var to = data.to.ToMailBoxIndex();
			if (mode != History.Mode.Undo)
			{
				#region DO
				var piece = mailBox[from.x][from.y];
				mailBox[from.x][from.y] = null;

				// Nếu là AI hoặc Remote thì tô màu đích đến trước khi di chuyển quân cờ
				if (!TurnManager.instance.CurrentPlayerIsLocalHuman()) moveTarget.position = to.ToVector3();

				await piece.transform.Move(to.ToVector3(), pieceMoveSpeed);

				#region Đặt quân vào ô vị trí {to}
				if (data.promotedName != null)
				{
					pieces[piece.color][piece.name].Recycle(piece);
					piece = pieces[(Color)data.playerID][data.promotedName.Value].Get(to.ToVector3());
				}

				var opponent = mailBox[to.x][to.y];
				if (opponent) pieces[opponent.color][opponent.name].Recycle(opponent);
				mailBox[to.x][to.y] = piece;
				moveTarget.position = to.ToVector3();
				#endregion

				#region Xử lý nếu có bắt quân đối phương
				if (data.capturedName != null)
					if (data.enpassantCapturedIndex != null)
					{
						var index = data.enpassantCapturedIndex.Value.ToMailBoxIndex();
						opponent = mailBox[index.x][index.y];
						mailBox[index.x][index.y] = null;
						pieces[opponent.color][opponent.name].Recycle(opponent);
					}
				#endregion

				if (data.castling != Core.MoveData.Castling.None)
				{
					var r = Core.CASTLING_ROOK_MOVEMENTS[(Color)data.playerID][data.castling];
					var rook = mailBox[r.m_from.x][r.m_from.y];
					mailBox[r.m_from.x][r.m_from.y] = null;
					await rook.transform.Move(r.m_to.ToVector3(), pieceMoveSpeed);
					mailBox[r.m_to.x][r.m_to.y] = rook;
				}
				#endregion
			}
			else
			{
				#region UNDO
				throw new NotImplementedException();

				#endregion
			}
		}


		public void OnGameOver()
		{
		}


		public void OnPlayerQuit(int playerID)
		{
			throw new System.NotImplementedException();
		}


		public UniTask<bool> OnReceiveRequest(int playerID, Request request)
		{
			throw new System.NotImplementedException();
		}
		#endregion
	}
}