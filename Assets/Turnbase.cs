using BoardGames.Databases;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using UnityEngine;


namespace BoardGames
{
	public interface IMoveData
	{
		int playerID { get; }
	}



	public enum Request : byte
	{
		/// <summary>
		/// Cầu hòa
		/// </summary>
		DRAW,
		/// <summary>
		/// Xin đi lại
		/// </summary>
		UNDO,
		/// <summary>
		/// Đi lại nước đi vừa Undo
		/// </summary>
		REDO
	}



	public interface ITime
	{
		float elapsedTurnTime { get; }
		float remainTurnTime { get; }
		float ElapsedPlayerTime(int playerID);
		float RemainPlayerTime(int playerID);
	}



	public interface ITurnListener
	{
		void OnTurnBegin();
		/// <summary>
		/// Lượt kết thúc do người chơi kết thúc hoặc lượt hết thời gian hoặc người chơi hiện tại hết thời gian<para/>
		/// Kiểm tra <see cref="TurnManager.RemainPlayerTime(int)"/> để tìm người chơi hết thời gian
		/// </summary>
		/// <param name="isTimeOver"><see langword="true"/>: Lượt hết thời gian hoặc người chơi hiện tại hết thời gian</param>
		void OnTurnEnd(bool isTimeOver);
		UniTask OnPlayerMove(IMoveData moveData, History.Mode mode);
		/// <summary>
		/// Nhận được yêu cầu và đợi phản hồi. Người gửi yêu cầu sẽ không nhận yêu cầu<para/>
		/// Có thể gửi yêu cầu bất cứ khi nào khi đang chơi game không cần đợi đến lượt mình
		/// </summary>
		/// <returns><see langword="true"/> nếu chấp nhận yêu cầu</returns>
		UniTask<bool> OnReceiveRequest(int playerID, Request request);
		/// <summary>
		/// Người chơi thoát khỏi bàn chơi (<see cref="Table"/>).<para/>
		/// Được gọi khi <paramref name="playerID"/> thoát thì game còn lại &gt;= 2 người chơi
		/// </summary>
		/// <param name="playerID">Người chơi mới thoát</param>
		void OnPlayerQuit(int playerID);
		/// <summary>
		/// Game kết thúc vì trạng thái game kết thúc hoặc chỉ còn lại 1 người chơi<para/>
		/// Đặc biệt: nếu số lượt của bàn chơi đạt đến tối đa (<see cref="int.MaxValue"/>) thì sẽ kết thúc bàn chơi. Mỗi trò chơi tự quyết định kết quả.
		/// </summary>
		void OnGameOver();
	}



	/// <summary>
	/// Lưu lại lịch sử các nước đi và cho phép Undo/ Redo.<br/>
	/// Trạng thái bàn chơi chỉ được thay đổi thông qua <see cref="Play(IMoveData)"/>, <see cref="Undo(int)"/> và <see cref="Redo(int)"/>
	/// </summary>
	public sealed class History
	{
		/// <summary>
		/// Số lượng nước đi liên tục (Play) tối đa có thể lưu lại. > 0
		/// </summary>
		public const ushort CAPACITY = ushort.MaxValue;
		private readonly List<IMoveData> recentMoves = new List<IMoveData>(CAPACITY);
		private readonly List<IMoveData[]> undoneMoves = new List<IMoveData[]>(CAPACITY);
		/// <summary>
		/// Số lượng nước đã đi (Play/Redo).
		/// </summary>
		public int moveCount => recentMoves.Count;
		public IMoveData this[int index] => recentMoves[index];
		public enum Mode
		{
			Play, Undo, Redo
		}
		/// <summary>
		/// Thực thi 1 nước đi (Play/Undo/Redo)<para/>
		/// Chú ý: không nên sử dụng <see cref="History"/> trong event vì trạng thái <see cref="History"/> đang không hợp lệ !
		/// </summary>
		public event Action<IMoveData, Mode> execute;


		public void Play(IMoveData data)
		{
			undoneMoves.Clear();
			if (recentMoves.Count == CAPACITY) recentMoves.RemoveAt(0);
			recentMoves.Add(data);
			execute(data, Mode.Play);
		}


		public bool CanUndo(int playerID)
		{
			for (int i = recentMoves.Count - 1; i >= 0; --i) if (recentMoves[i].playerID == playerID) return true;
			return false;
		}


		private readonly List<IMoveData> tmpMoves = new List<IMoveData>();
		public void Undo(int playerID)
		{
			tmpMoves.Clear();
			int tmpID;

			do
			{
				var move = recentMoves[recentMoves.Count - 1];
				recentMoves.RemoveAt(recentMoves.Count - 1);
				tmpMoves.Add(move);
				execute(move, Mode.Undo);
				tmpID = move.playerID;
			} while (tmpID != playerID);
			undoneMoves.Add(tmpMoves.ToArray());
		}


		public bool CanRedo(int playerID)
		{
			for (int i = undoneMoves.Count - 1; i >= 0; --i)
			{
				var moves = undoneMoves[i];
				if (moves[moves.Length - 1].playerID == playerID) return true;
			}
			return false;
		}


		public void Redo(int playerID)
		{
			int tmpID;

			do
			{
				var moves = undoneMoves[undoneMoves.Count - 1];
				undoneMoves.RemoveAt(undoneMoves.Count - 1);
				for (int i = moves.Length - 1; i >= 0; --i)
				{
					var move = moves[i];
					execute(move, Mode.Redo);
					recentMoves.Add(move);
				}

				tmpID = moves[moves.Length - 1].playerID;
			} while (tmpID != playerID);
		}
	}



	public abstract class TurnManager : MonoBehaviour, ITime
	{
		public class Config
		{
		}


		public static TurnManager instance { get; private set; }
		protected readonly IReadOnlyList<ITurnListener> listeners = new List<ITurnListener>();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddListener(ITurnListener listener) => (instance.listeners as List<ITurnListener>).Add(listener);


		/// <summary>
		/// gameObject có thể bị Destroy
		/// </summary>
		protected void Awake()
		{
			instance = instance ? throw new Exception() : this;
			Config config;
			try { config = "TURNBASE_CONFIG".GetValue<Config>(); } catch { Destroy(gameObject); return; }
			history = new History();
		}


		protected abstract void BeginTurn();
		public Func<bool> IsGameOver;
		protected abstract void FinishTurn();
		public abstract int currentPlayerID { get; }
		/// <summary>
		/// Đi 1 nước, nếu không có <paramref name="data"/> thì bỏ lượt, đồng thời có thể kết thúc lượt ngay sau khi đi.
		/// </summary>
		/// <param name="data">Nếu <see langword="null"/> thì bỏ lượt</param>
		/// <param name="endTurn">Nếu <see langword="true"/> thì kết thúc lượt ngay sau khi đi</param>
		public abstract UniTask Play(IMoveData data, bool endTurn);
		/// <summary>
		/// Gửi yêu cầu cho tất cả người chơi khác (ngoại trừ người chơi đã gửi yêu cầu)
		/// </summary>
		/// <returns><see langword="true"/> nếu tất cả người chơi khác chấp nhận yêu cầu</returns>
		public abstract UniTask<bool> SendRequest(Request request);
		/// <summary>
		/// Thoát trò chơi/ đầu hàng<para/>
		/// Nếu chỉ còn lại 1 người chơi thì trò chơi sẽ kết thúc ngay lập tức
		/// </summary>
		public abstract void Quit();


		#region Time
		public abstract float elapsedTurnTime { get; }
		public abstract float remainTurnTime { get; }
		public abstract float ElapsedPlayerTime(int playerID);
		public abstract float RemainPlayerTime(int playerID);

		protected float turnStartTime = float.MaxValue, cacheElapsedTurnTime;
		protected readonly Dictionary<int, float> playerStartTimes = new Dictionary<int, float>(),
			elapsedPlayerTimes = new Dictionary<int, float>();
		#endregion


		#region Lịch sử
		protected History history { get; private set; }
		/// <summary>
		/// Sử dụng checked{ } khi thay đổi value. Ví dụ: <c>checked{ ++turn; }</c>
		/// </summary>
		public int turn { get; protected set; }
		public int moveCount => history.moveCount;
		public IMoveData this[int index] => history[index];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CanUndo(int playerID) => history.CanUndo(playerID);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CanRedo(int playerID) => history.CanRedo(playerID);
		#endregion
	}



	/// <summary>
	/// Chỉ tồn tại khi chơi offline
	/// </summary>
	public abstract class AIAgent : MonoBehaviour, ITurnListener
	{
		public enum Level
		{
			Easy, Medium, Hard, Expert
		}

		public class Config
		{
			public Level level;
		}


		public static AIAgent instance { get; private set; }
		protected Level level;

		protected void Awake()
		{
			instance = instance ? throw new Exception() : this;
			level = "AI_CONFIG".GetValue<Config>().level;
		}


		protected void Start()
		{
			TurnManager.instance.AddListener(this);
		}


		public abstract UniTask<IMoveData> GenerateMoveData();


		#region Listen
		public abstract void OnGameOver();
		public abstract UniTask OnPlayerMove(IMoveData data, History.Mode mode);
		public abstract void OnTurnBegin();
		public abstract void OnTurnEnd(bool isTimeOver);
		#endregion


		#region Not supported
		public void OnPlayerQuit(int playerID) => throw new NotSupportedException();
		public UniTask<bool> OnReceiveRequest(int playerID, Request request) => throw new NotSupportedException();
		#endregion
	}
}