using BoardGames.Databases;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
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
		void OnTurnEnd();
		UniTask OnPlayerMove(IMoveData moveData, History.Mode mode);
		void OnTurnTimeOver();
		void OnPlayerTimeOver(int playerID);
		/// <summary>
		/// Nhận được yêu cầu và đợi phản hồi. Người gửi yêu cầu sẽ không nhận yêu cầu<para/>
		/// Có thể gửi yêu cầu bất cứ khi nào khi đang chơi game không cần đợi đến lượt mình
		/// </summary>
		/// <returns><see langword="true"/> nếu chấp nhận yêu cầu</returns>
		UniTask<bool> OnReceiveRequest(int playerID, Request request);
		/// <summary>
		/// Người chơi thoát khỏi bàn chơi (<see cref="Table"/>).<para/>
		/// Chỉ xảy ra (được gọi) khi <paramref name="playerID"/> thoát thì game còn lại &gt;= 2 người chơi
		/// </summary>
		/// <param name="playerID">Người chơi mới thoát</param>
		void OnPlayerQuit(int playerID);
		/// <summary>
		/// Game kết thúc vì trạng thái game kết thúc hoặc chỉ còn lại 1 người chơi
		/// </summary>
		void OnGameOver();
	}



	/// <summary>
	/// Lưu lại lịch sử các nước đi và cho phép Undo/ Redo.<br/>
	/// Trạng thái bàn chơi chỉ được thay đổi thông qua <see cref="Play(IMoveData)"/>, <see cref="Undo(int)"/> và <see cref="Redo(int)"/>
	/// </summary>
	[DataContract]
	public sealed class History
	{
		/// <summary>
		/// Số lượng nước đi liên tục (Play) tối đa có thể lưu lại. > 0
		/// </summary>
		public const ushort CAPACITY = ushort.MaxValue;


		public History() { }


		public History(History history)
		{
			turn = history.turn;
			recentMoves.AddRange(history.recentMoves);
			undoneMoves.AddRange(history.undoneMoves);
		}


		[DataMember]
		private readonly List<IMoveData> recentMoves = new List<IMoveData>(CAPACITY), undoneMoves = new List<IMoveData>(CAPACITY);

		/// <summary>
		/// Số lượng nước đã đi (Play/Redo).
		/// </summary>
		public int moveCount => recentMoves.Count;

		public IMoveData this[int index] => recentMoves[index];

		[DataMember]
		public int turn { get; private set; }


		public enum Mode
		{
			Play, Undo, Redo
		}
		public event Action<IMoveData, Mode> execute;


		public void Play(IMoveData data)
		{
			undoneMoves.Clear();
			if (recentMoves.Count == CAPACITY) recentMoves.RemoveAt(0);
			++turn;
			recentMoves.Add(data);
			execute(data, Mode.Play);
		}


		public bool CanUndo(int playerID)
		{
			for (int i = recentMoves.Count - 1; i >= 0; --i) if (recentMoves[i].playerID == playerID) return true;
			return false;
		}


		public void Undo(int playerID)
		{
			int tmp;
			do
			{
				var move = recentMoves[recentMoves.Count - 1];
				recentMoves.RemoveAt(recentMoves.Count - 1);
				undoneMoves.Add(move);
				--turn;
				execute(move, Mode.Undo);
				tmp = move.playerID;
			} while (tmp != playerID);
		}


		public bool CanRedo(int playerID)
		{
			for (int i = undoneMoves.Count - 1; i >= 0; --i) if (undoneMoves[i].playerID == playerID) return true;
			return false;
		}


		public void Redo(int playerID)
		{
			int tmp;
			do
			{
				var move = undoneMoves[undoneMoves.Count - 1];
				undoneMoves.RemoveAt(undoneMoves.Count - 1);
				recentMoves.Add(move);
				++turn;
				execute(move, Mode.Redo);
				tmp = move.playerID;
			} while (tmp != playerID);
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
		public int turn => history.turn;
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
		public abstract void OnTurnEnd();
		#endregion


		#region Not supported
		public void OnTurnTimeOver() => throw new NotSupportedException();
		public void OnPlayerTimeOver(int playerID) => throw new NotSupportedException();
		public void OnPlayerQuit(int playerID) => throw new NotSupportedException();
		public UniTask<bool> OnReceiveRequest(int playerID, Request request) => throw new NotSupportedException();
		#endregion
	}



	public sealed class Server : ITime
	{
		private readonly History history;
		private readonly IEnumerator<int> playerIDGenerator;


		public Server() => throw new NotImplementedException();

		public int currentPlayerID => playerIDGenerator.Current;


		#region Network Time
		public float elapsedTurnTime => throw new NotImplementedException();
		public float remainTurnTime => maxTurnTime - elapsedTurnTime;
		public float ElapsedPlayerTime(int playerID) => throw new NotImplementedException();
		public float RemainPlayerTime(int playerID) => maxPlayerTimes[playerID] - ElapsedPlayerTime(playerID);

		private float turnStartTime, maxTurnTime;
		private readonly Dictionary<int, float> playerStartTimes = new Dictionary<int, float>();
		private readonly Dictionary<int, float> maxPlayerTimes = new Dictionary<int, float>();
		#endregion


		#region Validations
		private UniTask CheckTurnBegin() => throw new NotImplementedException();
		private UniTask CheckTurnEnd() => throw new NotImplementedException();
		private UniTask CheckPlayerMove(IMoveData data) => throw new NotImplementedException();
		private UniTask CheckRequest(int playerID, Request request) => throw new NotImplementedException();
		#endregion
	}
}