using BoardGames.Databases;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
		/// Kết thúc trò chơi: cầu hòa hoặc kết thúc trong cờ vây
		/// </summary>
		END,
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
		/// <summary>
		/// <see cref="ITurnListener.OnPlayerMove(IMoveData, History.Mode)"/> của tất cả <see cref="ITurnListener"/> luôn được bắt đầu tuần tự khi Play/Undo/Redo, sau đó <see cref="TurnManager"/> sẽ đợi tất cả task kết thúc !
		/// </summary>
		UniTask OnPlayerMove(IMoveData moveData, History.Mode mode);
		/// <summary>
		/// Nhận được yêu cầu và đợi phản hồi. Người gửi yêu cầu sẽ không nhận yêu cầu<para/>
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
		/// Game kết thúc vì trạng thái game kết thúc hoặc client thoát hoặc chỉ còn lại 1 người chơi<br/>
		/// Đặc biệt: nếu số lượt của bàn chơi đạt đến tối đa (<see cref="int.MaxValue"/>) thì sẽ kết thúc bàn chơi. Mỗi trò chơi tự quyết định kết quả.<para/>
		/// Đợi <see cref="OnPlayerMove(IMoveData, History.Mode)"/> kết thúc sau đó mới gọi
		/// </summary>
		void OnGameOver();
	}



	public static class EventCode
	{
		public const byte
		Ready = 1,

		UpdateTime = 2,

		Play = 3,
		DonePlay = 4,

		SendRequest = 5,
		ResponseRequest = 6,
		ExecuteRequest = 7,
		DoneExecutingRequest = 8,

		Quit = 9,
		GameOver = 10;
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
		private readonly List<IMoveData> recentMoves;
		private readonly List<IMoveData[]> undoneMoves;
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


		public History()
		{
			recentMoves = new List<IMoveData>();
			undoneMoves = new List<IMoveData[]>();
		}


		public History(History history) : this()
		{
			recentMoves.AddRange(history.recentMoves);
			undoneMoves.AddRange(history.undoneMoves);
			for (int i = 0; i < undoneMoves.Count; ++i)
			{
				var moves = undoneMoves[i];
				Array.Copy(moves, undoneMoves[i] = new IMoveData[moves.Length], moves.Length);
			}
		}


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


		private History(List<IMoveData> recentMoves, List<IMoveData[]> undoneMoves)
		{
			this.recentMoves = recentMoves;
			this.undoneMoves = undoneMoves;
		}


		private sealed class JsonConverter : JsonConverter<History>
		{
			private static readonly List<IMoveData> tmp = new List<IMoveData>();
			public override History ReadJson(JsonReader reader, Type objectType, History existingValue, bool hasExistingValue, JsonSerializer serializer)
			{
				reader.Read(); // type
				var type = Type.GetType($"{reader.ReadAsString()}, Assembly-CSharp");

				#region  recentMoves
				reader.Read(); // nameof
				reader.Read(); // [
				reader.Read(); // ] or {
				var recentMoves = new List<IMoveData>();
				while (reader.TokenType != JsonToken.EndArray)
				{
					// {
					recentMoves.Add(serializer.Deserialize(reader, type) as IMoveData);
					reader.Read(); // ] or {
				}
				#endregion

				#region undoneMoves
				reader.Read(); // nameof
				reader.Read(); // [
				reader.Read(); // ] or [
				var undoneMoves = new List<IMoveData[]>();

				lock (tmp)
				{
					while (reader.TokenType != JsonToken.EndArray)
					{
						// [ : quét item: IMoveData[]
						while (reader.TokenType != JsonToken.EndArray)
						{
							// quét item: IMoveData
							reader.Read(); // ] or {
							tmp.Clear();
							while (reader.TokenType != JsonToken.EndArray)
							{
								// {
								tmp.Add(serializer.Deserialize(reader, type) as IMoveData);
								reader.Read(); // ] or {
							}
							undoneMoves.Add(tmp.ToArray());
						}
						reader.Read(); // ] or [
					}
				}
				#endregion

				reader.Read(); // }
				return new History(recentMoves, undoneMoves);
			}


			public override void WriteJson(JsonWriter writer, History value, JsonSerializer serializer)
			{
				writer.WriteStartObject();

				// type
				writer.WritePropertyName("type");
				writer.WriteValue(value.recentMoves.Count > 0 ? value.recentMoves[0].GetType().FullName : "");

				#region recentMoves
				writer.WritePropertyName(nameof(value.recentMoves));
				writer.WriteStartArray(); // [
				for (int i = 0; i < value.recentMoves.Count; ++i)
					serializer.Serialize(writer, value.recentMoves[i]);
				writer.WriteEndArray(); // ]
				#endregion

				#region undoneMoves
				writer.WritePropertyName(nameof(value.undoneMoves));
				writer.WriteStartArray(); // [
				for (int u = 0; u < value.undoneMoves.Count; ++u)
				{
					writer.WriteStartArray(); // [
					var moves = value.undoneMoves[u];
					for (int m = 0; m < moves.Length; ++m) serializer.Serialize(writer, moves[m]);
					writer.WriteEndArray(); // ]
				}
				writer.WriteEndArray(); // ]
				#endregion

				writer.WriteEndObject();
			}
		}
	}



	public abstract class TurnManager : MonoBehaviour, ITime
	{
		[Serializable]
		public class Config
		{

		}


		public static TurnManager instance { get; private set; }
		protected readonly IReadOnlyList<ITurnListener> listeners = new List<ITurnListener>();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddListener(ITurnListener listener) => (instance.listeners as List<ITurnListener>).Add(listener);


		protected void Awake()
		{
			instance = instance ? throw new Exception() : this;
			var config = "TURNBASE_CONFIG".GetValue<Config>();
			history = new History();
		}


		/// <summary>
		/// Khởi động Turn Manager: bắt đầu chơi
		/// </summary>
		public async void StartPlaying()
		{
			if (turn > 0)
				throw new InvalidOperationException("Không thể Start game nữa vì TurnManager đã khởi động rồi !");

			// Đảm bảo scene đã khởi tạo xong hết: tất cả Awake() và Start() đã gọi xong.
			await UniTask.Yield();
			BeginTurn();
		}


		protected abstract void BeginTurn();
		public Func<bool> IsGameOver;
		protected abstract void FinishTurn();
		public abstract int currentPlayerID { get; }
		/// <summary>
		/// Đi 1 nước, nếu không có <paramref name="data"/> thì bỏ lượt, đồng thời có thể kết thúc lượt ngay sau khi đi<para/>
		/// Chú ý: Đảm bảo bắt đầu tuần tự các <see cref="ITurnListener.OnPlayerMove(IMoveData, History.Mode)"/> sau đó đợi tất cả quá trình <see langword="async"/> kết thúc<br/>
		/// Ví dụ: ... <see langword="await"/> <see cref="UniTask.WhenAll(IEnumerable{UniTask})"/>  ...
		/// </summary>
		/// <param name="data">Nếu <see langword="null"/> thì bỏ lượt</param>
		/// <param name="endTurn">Nếu <see langword="true"/> thì kết thúc lượt ngay sau khi đi</param>
		public abstract UniTask Play(IMoveData data, bool endTurn);
		/// <summary>
		/// Gửi yêu cầu cho tất cả người chơi khác (ngoại trừ người chơi đã gửi yêu cầu)<para/>
		/// Chú ý với: Đảm bảo bắt đầu tuần tự các <see cref="ITurnListener.OnReceiveRequest(int, Request)"/> sau đó đợi tất cả quá trình <see langword="async"/> kết thúc<br/>
		/// Ví dụ: ... <see langword="await"/> <see cref="UniTask.WhenAll(IEnumerable{UniTask})"/>  ...<para/>
		/// Tương tự như trên, đảm bảo bắt đầu tuần tự các <see cref="ITurnListener.OnPlayerMove(IMoveData, History.Mode)"/> nếu Undo/Redo<br/>
		/// Nếu undo/redo: khôi phục <see cref="ElapsedPlayerTime(int)"/> của người chơi hiện tại và reset <see cref="elapsedTurnTime"/> = 0
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
		protected History history;
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