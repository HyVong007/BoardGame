using BoardGames.Databases;
using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;


namespace BoardGames
{
	public sealed class P2PTurnManager : TurnManager
	{
		public new sealed class Config : TurnManager.Config
		{

		}


		private new void Awake()
		{
			base.Awake();
			PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
			var config = "TURNBASE_CONFIG".GetValue<Config>();
			playerIDGenerator = PlayerIDGenerator(2);
			history.execute += (data, mode) => moveQueues.Enqueue((data, mode));
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
			playerIDGenerator.MoveNext();
			foreach (var listener in listeners) listener.OnTurnBegin();

			if (!PhotonNetwork.IsMasterClient)
			{
				sendHash.Clear();
				sendHash[HashKey.Turn] = turn;
				sendHash[HashKey.PlayerID] = TablePlayer.local.id;
				PhotonNetwork.RaiseEvent(EventCode.DoneBeginTurn, sendHash,
					new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient, CachingOption = EventCaching.AddToRoomCache },
					SendOptions.SendReliable);
			}
		}


		private readonly Queue<(IMoveData data, History.Mode mode)> moveQueues = new Queue<(IMoveData data, History.Mode mode)>();
		public override async UniTask Play(IMoveData data, bool endTurn)
		{
			if (lastEventCode != EventCode.Play)
			{
				sendHash.Clear();
				sendHash[HashKey.Turn] = turn;
				//sendHash[HashKey.Order] = ...
				sendHash[HashKey.PlayerID] = TablePlayer.local.id;
				sendHash[HashKey.Data] = data;
				sendHash[HashKey.IsEndTurn] = endTurn;
				PhotonNetwork.RaiseEvent(EventCode.Play, sendHash,
				  new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache }
				  , SendOptions.SendReliable);
			}
			else lastEventCode = null;

			if (data == null)
			{
				FinishTurn();
				return;
			}

			history.Play(data);
			await ExecuteMoveQueue();
			if (!PhotonNetwork.IsMasterClient)
			{
				sendHash.Clear();
				sendHash[HashKey.Turn] = turn;
				//sendHash[HashKey.Order]= ...
				sendHash[HashKey.PlayerID] = TablePlayer.local.id;
				PhotonNetwork.RaiseEvent(EventCode.DonePlay, sendHash,
				  new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient, CachingOption = EventCaching.AddToRoomCache }
				  , SendOptions.SendReliable);
			}

			if (endTurn || IsGameOver()) FinishTurn();
		}


		public override void Quit()
		{
			sendHash.Clear();
			sendHash[HashKey.Turn] = turn;
			sendHash[HashKey.PlayerID] = TablePlayer.local.id;
			PhotonNetwork.RaiseEvent(EventCode.Quit, sendHash,
				  new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache }
				  , SendOptions.SendReliable);

			foreach (var listener in listeners) listener.OnGameOver();
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
			await UniTask.WaitWhile(() => sendTurn == turn && resultRequest == null);
			if (sendTurn != turn || resultRequest != true)
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

			foreach (var listener in listeners) listener.OnTurnEnd(false); // test
			if (IsGameOver())
			{
				foreach (var listener in listeners) listener.OnGameOver();
				Destroy(gameObject);
			}
			else UniTask.Yield(PlayerLoopTiming.Update, default).ContinueWith(BeginTurn).Forget();
		}


		private readonly List<UniTask> moveTasks = new List<UniTask>();
		private async UniTask ExecuteMoveQueue()
		{
			do
			{
				var (data, mode) = moveQueues.Dequeue();
				moveTasks.Clear();
				foreach (var listener in listeners) moveTasks.Add(listener.OnPlayerMove(data, mode));
				await UniTask.WhenAll(moveTasks);
			} while (moveQueues.Count != 0);
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
		private float maxTurnTime;
		private readonly Dictionary<int, float> maxPlayerTimes = new Dictionary<int, float>();


		public override sealed float remainTurnTime => maxTurnTime - elapsedTurnTime;


		public override sealed float RemainPlayerTime(int playerID) => maxPlayerTimes[playerID] - ElapsedPlayerTime(playerID);


		public override float elapsedTurnTime
		{
			get
			{
				return 0;
			}
		}


		public override float ElapsedPlayerTime(int playerID)
		{
			return 0;
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
			Data = 4;
		}
		private readonly List<UniTask<bool>> requestResults = new List<UniTask<bool>>();


		private async void OnPhotonEvent(EventData photonEvent)
		{
			if (photonEvent.Code >= 200) return;
			var data = photonEvent.CustomData as Hashtable;
			switch (lastEventCode = photonEvent.Code)
			{
				case EventCode.DoneBeginTurn: break;
				case EventCode.DonePlay: break;
				case EventCode.GameOver: break;
				case EventCode.Play:
					await Play(data[HashKey.Data] as IMoveData, (bool)data[HashKey.IsEndTurn]);
					break;

				case EventCode.PlayerTimeOver: break;
				case EventCode.TurnTimeOver: break;
				case EventCode.Quit:
					// game cờ 2 người
					foreach (var listener in listeners) listener.OnGameOver();
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
			}
		}
	}
}