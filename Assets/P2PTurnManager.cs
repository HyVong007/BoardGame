using BoardGames.Databases;
using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace BoardGames
{
	public sealed class P2PTurnManager : TurnManager
	{
		public new sealed class Config : TurnManager.Config
		{
			public int playerCount;
			public float maxTurnTime, maxPlayerTime;
		}


		private int PLAYER_COUNT;
		private new void Awake()
		{
			base.Awake();
			PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
			history.execute += (data, mode) => moveQueues.Enqueue((data, mode));
			var config = "TURNBASE_CONFIG".GetValue<Config>();
#if DEBUG
			if (config.playerCount < 2 || config.maxTurnTime < 0 || config.maxPlayerTime < config.maxTurnTime)
				throw new ArgumentOutOfRangeException();
#endif
			PLAYER_COUNT = config.playerCount;
			playerIDGenerator = PlayerIDGenerator(PLAYER_COUNT);
			for (int i = 0; i < PLAYER_COUNT; ++i)
			{
				playerStartTimes[i] = float.MaxValue;
				elapsedPlayerTimes[i] = 0;
			}
			MAX_TURN_TIME = config.maxTurnTime;
			MAX_PLAYER_TIME = config.maxPlayerTime;
		}


		private void OnDisable()
		{
			PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
		}


		#region Turn Manager
		protected override void BeginTurn()
		{
#if UNITY_EDITOR
			try
			{
				// Kiểm tra xem có bị Destroy chưa ?
				var _ = gameObject;
			}
			catch { return; }
#endif
			checked { ++turn; }

			// Tìm người chơi tiếp theo còn trong bàn (người chơi chưa hết thời gian)
			do playerIDGenerator.MoveNext();
			while (elapsedPlayerTimes[currentPlayerID] >= MAX_PLAYER_TIME);

			cacheElapsedTurnTime = 0;
			countTime = true;
			foreach (var listener in listeners) listener.OnTurnBegin();
		}


		private readonly Queue<(IMoveData data, History.Mode mode)> moveQueues = new Queue<(IMoveData data, History.Mode mode)>();
		private int reportCount_DonePlay;
		public override async UniTask Play(IMoveData data, bool endTurn)
		{
			if (PhotonNetwork.IsMasterClient)
			{
				#region Master điều phối Play cho tất cả client khác
				if (!countTime) return;

				// Gửi tin Play cho other
				countTime = false;
				reportCount_DonePlay = 0;
				sendHash.Clear();
				sendHash[HashKey.Turn] = turn;
				//sendHash[HashKey.Order] = ...
				sendHash[HashKey.PlayerID] = TablePlayer.local.id;
				sendHash[HashKey.Data] = data;
				sendHash[HashKey.IsEndTurn] = endTurn;
				PhotonNetwork.RaiseEvent(EventCode.Play, sendHash,
				  new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache }
				  , SendOptions.SendReliable);

				// Play
				if (data != null)
				{
					history.Play(data);
					await ExecuteMoveQueue();
				}

				// Đợi other play xong
				await UniTask.WaitWhile(() => reportCount_DonePlay < PLAYER_COUNT - 1);
				countTime = true;

				// Kết thúc ?
				if (data == null || endTurn || IsGameOver()) FinishTurn();
				#endregion
			}
			else if (lastEventCode != EventCode.Play)
			{
				// Người chơi gửi tin nhắn cho Master
				sendHash.Clear();
				sendHash[HashKey.Turn] = turn;
				//sendHash[HashKey.Order] = ...
				sendHash[HashKey.PlayerID] = TablePlayer.local.id;
				sendHash[HashKey.Data] = data;
				sendHash[HashKey.IsEndTurn] = endTurn;
				PhotonNetwork.RaiseEvent(EventCode.Play, sendHash,
				  new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient, CachingOption = EventCaching.AddToRoomCache }
				  , SendOptions.SendReliable);
			}
			else
			{
				lastEventCode = null;
				if (data == null)
				{
					ReportDone();
					FinishTurn();
					return;
				}

				history.Play(data);
				await ExecuteMoveQueue();
				ReportDone();
				if (endTurn || IsGameOver()) FinishTurn();
			}


			void ReportDone()
			{
				sendHash.Clear();
				sendHash[HashKey.Turn] = turn;
				//sendHash[HashKey.Order]= ...
				sendHash[HashKey.PlayerID] = TablePlayer.local.id;
				PhotonNetwork.RaiseEvent(EventCode.DonePlay, sendHash,
				  new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient, CachingOption = EventCaching.AddToRoomCache }
				  , SendOptions.SendReliable);
			}
		}


		public override void Quit()
		{
			countTime = false;
			sendHash.Clear();
			sendHash[HashKey.Turn] = turn;
			sendHash[HashKey.PlayerID] = TablePlayer.local.id;
			PhotonNetwork.RaiseEvent(EventCode.Quit, sendHash,
				  new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache }
				  , SendOptions.SendReliable);

			foreach (var listener in listeners) listener.OnGameOver();
			gameObject.SetActive(false);
			Destroy(gameObject);
		}


		#region Request
		private int requestOrder;
		private bool? resultRequest;

		public override async UniTask<bool> SendRequest(Request request)
		{
			sendHash.Clear();
			sendHash[HashKey.Turn] = turn;
			sendHash[HashKey.Order] = requestOrder;
			sendHash[HashKey.PlayerID] = TablePlayer.local.id;
			sendHash[HashKey.Data] = request;

			// Gửi yêu cầu
			resultRequest = null;
			int sendTurn = turn;
			PhotonNetwork.RaiseEvent(EventCode.SendRequest, sendHash,
				  new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache }
				  , SendOptions.SendReliable);

			// Đợi kết quả phản hồi (trong khi turn hiện tại chưa kết thúc)
			await UniTask.WaitWhile(()
				=> resultRequest == null && sendTurn == turn && remainTurnTime > 0);
			if (resultRequest != true || sendTurn != turn || remainTurnTime <= 0)
			{
				checked { ++requestOrder; }
				return false;
			}

			// Gửi thông báo ExecuteRequest cho người chơi khác
			sendHash.Clear();
			sendHash[HashKey.Turn] = turn;
			sendHash[HashKey.Order] = requestOrder;
			sendHash[HashKey.PlayerID] = TablePlayer.local.id;
			sendHash[HashKey.Data] = request;
			PhotonNetwork.RaiseEvent(EventCode.ExecuteRequest, sendHash,
			new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache }
			, SendOptions.SendReliable);

			// Thực thi yêu cầu
			await ExecuteRequest(request);
			checked { ++requestOrder; }
			return true;
		}


		private async UniTask ExecuteRequest(Request request)
		{
			switch (request)
			{
				case Request.END: throw new NotImplementedException();
				case Request.REDO: throw new NotImplementedException();
				case Request.UNDO:

					history.Undo(currentPlayerID);
					await ExecuteMoveQueue();
					elapsedPlayerTimes[currentPlayerID] -= elapsedTurnTime;
					cacheElapsedTurnTime = 0;
					RefreshStartTime();
					break;
			}

			// Gửi phản hồi DoneExecutingRequest
			if (!PhotonNetwork.IsMasterClient)
			{
				sendHash.Clear();
				sendHash[HashKey.Turn] = turn;
				sendHash[HashKey.Order] = requestOrder;
				sendHash[HashKey.PlayerID] = TablePlayer.local.id;
				sendHash[HashKey.Data] = request;
				PhotonNetwork.RaiseEvent(EventCode.DoneExecutingRequest, sendHash,
				new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient, CachingOption = EventCaching.AddToRoomCache }
				, SendOptions.SendReliable);
			}

			// Nếu GameOver thì kết thúc !
			if (IsGameOver())
			{
				FinishTurn();
				return;
			}
		}
		#endregion


		protected override void FinishTurn()
		{
			requestOrder = 0;
			countTime = false;
			if (PhotonNetwork.IsMasterClient)
			{
				sendHash.Clear();
				sendHash[HashKey.Turn] = turn;
				sendHash[HashKey.PlayerID] = currentPlayerID;
				sendHash[HashKey.ElapseTurnTime] = elapsedTurnTime;
				sendHash[HashKey.Data] = elapsedPlayerTimes;
				PhotonNetwork.RaiseEvent(EventCode.UpdateTime, sendHash,
					new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache },
					SendOptions.SendReliable);
			}

			bool isTimeOver = remainTurnTime <= 0 || RemainPlayerTime(currentPlayerID) <= 0;
			foreach (var listener in listeners) listener.OnTurnEnd(isTimeOver);
			if (!IsGameOver())
			{
				// Nếu còn ít nhất 2 người còn thời gian -> trò chơi còn tiếp tục
				int count = 0;
				for (int i = 0; i < PLAYER_COUNT; ++i) count += (RemainPlayerTime(i) > 0) ? 1 : 0;
				if (count >= 2)
				{
					// Chỉ có Master mới có thể BeginTurn trực tiếp
					if (PhotonNetwork.IsMasterClient)
						UniTask.Yield(PlayerLoopTiming.Update, default).ContinueWith(BeginTurn);
					return;
				}
			}

			foreach (var listener in listeners) listener.OnGameOver();
			gameObject.SetActive(false);
			Destroy(gameObject);
		}


		private readonly List<UniTask> moveTasks = new List<UniTask>();
		private async UniTask ExecuteMoveQueue()
		{
			bool isCountingTime = countTime;
			countTime = false;
			do
			{
				var (data, mode) = moveQueues.Dequeue();
				moveTasks.Clear();
				foreach (var listener in listeners) moveTasks.Add(listener.OnPlayerMove(data, mode));
				await UniTask.WhenAll(moveTasks);
			} while (moveQueues.Count != 0);
			countTime = isCountingTime;
		}


		#region currentPlayerID
		public override int currentPlayerID => playerIDGenerator.Current;

		private static IEnumerator<int> PlayerIDGenerator(int maxPlayer)
		{
			while (true) for (int i = 0; i < maxPlayer; ++i) yield return i;
		}
		private IEnumerator<int> playerIDGenerator;
		#endregion


		#region Time
		private void FixedUpdate()
		{
			if (!PhotonNetwork.IsMasterClient || !countTime) return;

			if (RemainPlayerTime(currentPlayerID) <= 0) FinishTurn();
			else if (remainTurnTime <= 0)
			{
				if (remainTurnTime < 0)
				{
					// Elapse của người chơi hiện tại trừ "độ âm" của lượt
					countTime = false;
					elapsedPlayerTimes[currentPlayerID] -= Mathf.Abs(remainTurnTime);
				}

				FinishTurn();
			}
		}


		private float MAX_TURN_TIME, MAX_PLAYER_TIME;
		public sealed override float elapsedTurnTime => countTime ? Time.time - turnStartTime : cacheElapsedTurnTime;


		public override float remainTurnTime => MAX_TURN_TIME - elapsedTurnTime;


		public sealed override float ElapsedPlayerTime(int playerID)
			=> playerID != currentPlayerID || !countTime ? elapsedPlayerTimes[playerID]
			: Time.time - playerStartTimes[playerID];


		public override float RemainPlayerTime(int playerID) => MAX_PLAYER_TIME - ElapsedPlayerTime(playerID);


		private bool ΔcountTime;
		/// <summary>
		/// <see langword="true"/> : tiếp tục đếm thời gian, giữ nguyên elapse hiện tại<br/>
		/// <see langword="false"/> : tạm ngưng, lưu elapse vào cache
		/// </summary>
		private bool countTime
		{
			get => ΔcountTime;

			set
			{
				if (value == ΔcountTime) return;
				ΔcountTime = value;
				if (value)
				{
					turnStartTime = Time.time - cacheElapsedTurnTime;
					playerStartTimes[currentPlayerID] = Time.time - elapsedPlayerTimes[currentPlayerID];
				}
				else
				{
					cacheElapsedTurnTime = Time.time - turnStartTime;
					elapsedPlayerTimes[currentPlayerID] = Time.time - playerStartTimes[currentPlayerID];
				}
			}
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void RefreshStartTime()
		{
			ΔcountTime = false;
			countTime = true;
		}
		#endregion
		#endregion


		private byte? lastEventCode;
		private readonly Hashtable sendHash = new Hashtable();
		public static class HashKey
		{
			public const byte
			Turn = 0,
			Order = 1,
			PlayerID = 2,
			IsEndTurn = 3,
			ElapseTurnTime = 4,
			Data = 5;
		}
		private readonly List<UniTask<bool>> requestResults = new List<UniTask<bool>>();


		private async void OnPhotonEvent(EventData photonEvent)
		{
			if (photonEvent.Code >= 200) return;
			var data = photonEvent.CustomData as Hashtable;
			switch (lastEventCode = photonEvent.Code)
			{
				case EventCode.GameOver: break;
				case EventCode.Play:
					// Master xác thực thời gian
					if (PhotonNetwork.IsMasterClient && (
						(int)data[HashKey.Turn] != turn
						// || kiểm tra Order
						|| (int)data[HashKey.PlayerID] != currentPlayerID
						|| remainTurnTime <= 0
						|| RemainPlayerTime(currentPlayerID) <= 0)) break;

					await Play(data[HashKey.Data] as IMoveData, (bool)data[HashKey.IsEndTurn]);
					break;

				case EventCode.DonePlay:
					++reportCount_DonePlay;
					break;

				case EventCode.Quit:
					// game cờ 2 người
					foreach (var listener in listeners) listener.OnGameOver();
					gameObject.SetActive(false);
					Destroy(gameObject);
					break;

				case EventCode.SendRequest:
					{
						requestResults.Clear();
						foreach (var listener in listeners)
							requestResults.Add(listener.OnReceiveRequest((int)data[HashKey.PlayerID], (Request)data[HashKey.Data]));
						await UniTask.WhenAll(requestResults);

						bool result = true;
						foreach (var task in requestResults) result &= await task;
						sendHash.Clear();
						sendHash[HashKey.Turn] = turn;
						sendHash[HashKey.Order] = requestOrder;
						sendHash[HashKey.PlayerID] = TablePlayer.local.id;
						sendHash[HashKey.Data] = result;
						PhotonNetwork.RaiseEvent(EventCode.ResponseRequest, sendHash,
							new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache },
							SendOptions.SendReliable);
					}
					break;

				case EventCode.ResponseRequest:
					resultRequest = (bool)data[HashKey.Data];
					break;

				case EventCode.ExecuteRequest:
					await ExecuteRequest((Request)data[HashKey.Data]);
					break;

				case EventCode.DoneExecutingRequest: break;

				case EventCode.UpdateTime:
					cacheElapsedTurnTime = (float)data[HashKey.ElapseTurnTime];
					turnStartTime = Time.time - cacheElapsedTurnTime;
					var dict = data[HashKey.Data] as IDictionary<int, float>;
					for (int i = 0; i < PLAYER_COUNT; ++i) elapsedPlayerTimes[i] = dict[i];
					playerStartTimes[currentPlayerID] = Time.time - elapsedPlayerTimes[currentPlayerID];

					if (!countTime)
					{
						// Đã FinishTurn trước đó
						int count = 0;
						for (int i = 0; i < PLAYER_COUNT; ++i) count += (RemainPlayerTime(i) > 0) ? 1 : 0;
						if (count >= 2) BeginTurn();
						else
						{
							foreach (var listener in listeners) listener.OnGameOver();
							gameObject.SetActive(false);
							Destroy(gameObject);
						}
					}
					else
					{
						FinishTurn();
						if (gameObject.activeSelf) BeginTurn();
					}
					break;
			}
		}
	}
}