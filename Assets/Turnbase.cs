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



	public enum Request
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



	public interface IListener
	{
		UniTask OnTurnBegin();
		UniTask OnTurnEnd();
		UniTask OnPlayerMove(IMoveData data, History.Mode mode);
		UniTask OnTurnTimeOver();
		UniTask OnPlayerTimeOver(int playerID);
		/// <summary>
		/// Nhận được yêu cầu và đợi phản hồi. Người gửi yêu cầu sẽ không nhận yêu cầu<para/>
		/// Có thể gửi yêu cầu bất cứ khi nào khi đang chơi game không cần đợi đến lượt mình
		/// </summary>
		/// <returns><see langword="true"/> nếu chấp nhận yêu cầu</returns>
		UniTask<bool> OnRequestReceive(int playerID, Request request);
		/// <summary>
		/// Người chơi thoát ra.<para/>
		/// Chỉ xảy ra (được gọi) khi <paramref name="playerID"/> thoát thì game còn lại &gt;= 2 người chơi
		/// </summary>
		/// <param name="playerID">Người chơi mới thoát</param>
		void OnPlayerQuit(int playerID);
		/// <summary>
		/// Game kết thúc vì trạng thái game kết thúc hoặc chỉ còn lại 1 người chơi
		/// </summary>
		void OnGameFinish();
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
			recentActions.AddRange(history.recentActions);
			undoneActions.AddRange(history.undoneActions);
		}


		[DataMember]
		private readonly List<IMoveData> recentActions = new List<IMoveData>(CAPACITY), undoneActions = new List<IMoveData>(CAPACITY);

		/// <summary>
		/// Số lượng nước đã đi (Play/Redo).
		/// </summary>
		public int moveCount => recentActions.Count;

		public IMoveData this[int index] => recentActions[index];

		[DataMember]
		public int turn { get; private set; }


		public enum Mode
		{
			Play, Undo, Redo
		}
		public event Action<IMoveData, Mode> execute;


		public void Play(IMoveData data)
		{
			undoneActions.Clear();
			if (recentActions.Count == CAPACITY) recentActions.RemoveAt(0);
			++turn;
			recentActions.Add(data);
			execute(data, Mode.Play);
		}


		public bool CanUndo(int playerID)
		{
			for (int i = recentActions.Count - 1; i >= 0; --i) if (recentActions[i].playerID == playerID) return true;
			return false;
		}


		public void Undo(int playerID)
		{
			int tmp;
			do
			{
				var action = recentActions[recentActions.Count - 1];
				recentActions.RemoveAt(recentActions.Count - 1);
				undoneActions.Add(action);
				--turn;
				execute(action, Mode.Undo);
				tmp = action.playerID;
			} while (tmp != playerID);
		}


		public bool CanRedo(int playerID)
		{
			for (int i = undoneActions.Count - 1; i >= 0; --i) if (undoneActions[i].playerID == playerID) return true;
			return false;
		}


		public void Redo(int playerID)
		{
			int tmp;
			do
			{
				var action = undoneActions[undoneActions.Count - 1];
				undoneActions.RemoveAt(undoneActions.Count - 1);
				recentActions.Add(action);
				++turn;
				execute(action, Mode.Redo);
				tmp = action.playerID;
			} while (tmp != playerID);
		}
	}



	public abstract class TurnManager : MonoBehaviour, ITime
	{
		public class Config
		{
		}


		public static TurnManager instance { get; private set; }
		protected readonly IReadOnlyList<IListener> listeners = new List<IListener>();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddListener(IListener listener) => (instance.listeners as List<IListener>).Add(listener);


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


		protected abstract UniTask BeginTurn();
		public Func<bool> IsGameOver;
		protected abstract UniTask FinishTurn();
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
		public abstract UniTask<bool> Request(Request request);
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
	public abstract class AIAgentBase : MonoBehaviour, IListener
	{
		public enum Level
		{
			Easy, Medium, Hard, Expert
		}

		public class Config
		{
			public readonly Level level;

			public Config(Level level)
			{
				this.level = level;
			}
		}


		public static AIAgentBase instance { get; private set; }
		[SerializeField] protected Level level;

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
		public abstract void OnGameFinish();
		public abstract UniTask OnPlayerMove(IMoveData data, History.Mode mode);
		public abstract UniTask OnTurnBegin();
		public abstract UniTask OnTurnEnd();
		#endregion


		#region Not supported
		public UniTask OnTurnTimeOver() => throw new NotSupportedException();
		public UniTask OnPlayerTimeOver(int playerID) => throw new NotSupportedException();
		public void OnPlayerQuit(int playerID) => throw new NotSupportedException();
		public UniTask<bool> OnRequestReceive(int playerID, Request request) => throw new NotSupportedException();
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