using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace BoardGames
{
	public sealed class P2PTurnManager : TurnManager
	{
		public new sealed class Config : TurnManager.Config
		{

		}


		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{

		}


		private new void Awake()
		{
			base.Awake();
			if (!gameObject.activeSelf) return;

			var config = "TURNBASE_CONFIG".GetValue<Config>();
			PhotonNetwork.AddCallbackTarget(photonPun = new PhotonPun(this));
			PhotonNetwork.NetworkingClient.LoadBalancingPeer.ReuseEventInstance = true;
			playerIDGenerator = PlayerIDGenerator(2);
			history.execute += (data, mode) => moveQueues.Enqueue((data, mode));
		}


		private void OnDisable()
		{
			PhotonNetwork.RemoveCallbackTarget(photonPun);
		}


		#region Network Time
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
			throw new NotImplementedException();
		}
		#endregion


		#region currentPlayerID
		public override int currentPlayerID => playerIDGenerator.Current;

		private static IEnumerator<int> PlayerIDGenerator(int maxPlayer)
		{
			while (true) for (int i = 0; i < maxPlayer; ++i) yield return i;
		}
		private IEnumerator<int> playerIDGenerator;
		#endregion


		protected override void BeginTurn()
		{
			playerIDGenerator.MoveNext();
			foreach (var listener in listeners) listener.OnTurnBegin();

			if (!PhotonNetwork.IsMasterClient)
				PhotonNetwork.RaiseEvent((byte)EventCode.DoneBeginTurn, null,
					new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient, CachingOption = EventCaching.AddToRoomCache },
					SendOptions.SendReliable);
		}


		private readonly Queue<(IMoveData data, History.Mode mode)> moveQueues
			= new Queue<(IMoveData data, History.Mode mode)>();
		public override async UniTask Play(IMoveData data, bool endTurn)
		{
			if (lastEventCode != EventCode.Play)
				PhotonNetwork.RaiseEvent((byte)EventCode.Play,
				new Hashtable { ["DATA"] = data, ["END_TURN"] = endTurn },
				  new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache }
				  , SendOptions.SendReliable);
			else lastEventCode = null;

			// Không cần đợi phản hồi

			if (data == null)
			{
				FinishTurn();
				return;
			}

			history.Play(data);
			await ExecuteMoveQueues();
			if (endTurn) FinishTurn();
		}


		public override void Quit()
		{
			//PhotonNetwork.RaiseEvent((byte)EventCode.PlayerQuit, null,
			//	new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache }
			//	, SendOptions.SendReliable);

			foreach (var listener in listeners) listener.OnGameOver();
			Destroy(gameObject);
		}


		public override async UniTask<bool> SendRequest(Request request)
		{
			//PhotonNetwork.RaiseEvent((byte)EventCode.Request, (byte)request,
			//	new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache }
			//	, SendOptions.SendReliable);


			return true;
		}


		protected override void FinishTurn()
		{
			foreach (var listener in listeners) listener.OnTurnEnd();
			if (IsGameOver())
			{
				foreach (var listener in listeners) listener.OnGameOver();
				Destroy(gameObject);
			}
			else UniTask.Yield(PlayerLoopTiming.Update, default).ContinueWith(BeginTurn).Forget();
		}


		private async UniTask ExecuteMoveQueues()
		{
			do
			{
				var (data, mode) = moveQueues.Dequeue();
				foreach (var listener in listeners) await listener.OnPlayerMove(data, mode);
			} while (moveQueues.Count != 0);
		}



		private enum EventCode : byte
		{
			StartGame = 1,
			Play,
			Request,
			ResponseRequest,
			PlayerQuit,
			TurnTimeOver,
			PlayerTimeOver,
			DoneBeginTurn
		}

		private EventCode? lastEventCode;



		private sealed class PhotonPun : IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks, IOnEventCallback
		{
			private readonly P2PTurnManager p2p;
			public PhotonPun(P2PTurnManager p2p) => this.p2p = p2p;


			private const string PREFIX = "P2P TURN MANAGER: ";


			#region Connection Callbacks
			public void OnConnected()
			{
				print($"{PREFIX}Connected");
			}


			public void OnConnectedToMaster()
			{
				print($"{PREFIX}Connected To Master");
				PhotonNetwork.JoinOrCreateRoom("Tam", null, null);
			}


			public void OnCustomAuthenticationFailed(string debugMessage)
			{
				print($"{PREFIX}Custom Authentication Failed: {debugMessage}");
			}


			public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
			{

			}


			public void OnDisconnected(DisconnectCause cause)
			{
				print($"{PREFIX}Disconnected: {cause}");
			}


			public void OnRegionListReceived(RegionHandler regionHandler)
			{
			}
			#endregion


			#region Matchmaking Callbacks
			public void OnCreatedRoom()
			{
				print($"{PREFIX}Created Room");
			}

			public void OnCreateRoomFailed(short returnCode, string message)
			{
				print($"{PREFIX}Create Room Failed: {returnCode}, {message}");
			}

			public void OnFriendListUpdate(List<FriendInfo> friendList)
			{
			}

			public void OnJoinedRoom()
			{
				print($"{PREFIX}Joined Room");
			}

			public void OnJoinRandomFailed(short returnCode, string message)
			{
				print($"{PREFIX}Join Random Failed: {returnCode}, {message}");
			}

			public void OnJoinRoomFailed(short returnCode, string message)
			{
				print($"{PREFIX}Join Room Failed: {returnCode}, {message}");
			}

			public void OnLeftRoom()
			{
				print($"{PREFIX}Left Room");
			}
			#endregion


			#region InRoom Callbacks
			public void OnMasterClientSwitched(Player newMasterClient)
			{
			}


			public async void OnPlayerEnteredRoom(Player newPlayer)
			{
				// Test
				await UniTask.Yield(PlayerLoopTiming.LastUpdate);
				if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
				{
					p2p.BeginTurn();

					p2p.lastEventCode = null;
					PhotonNetwork.RaiseEvent((byte)EventCode.StartGame, null,
						new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache },
						SendOptions.SendReliable);

					await UniTask.WaitUntil(() => p2p.lastEventCode == EventCode.DoneBeginTurn);
					print($"{PREFIX} All other clients done turn begin !");
				}
			}


			public void OnPlayerLeftRoom(Player otherPlayer)
			{
			}

			public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
			{
			}

			public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
			{
			}
			#endregion


			public async void OnEvent(EventData photonEvent)
			{
				if (photonEvent.Code >= 200) return;
				switch (p2p.lastEventCode = (EventCode)photonEvent.Code)
				{
					case EventCode.StartGame:
						p2p.BeginTurn();
						break;

					case EventCode.DoneBeginTurn:
						break;

					case EventCode.Play:
						var hash = photonEvent.CustomData as Hashtable;
						await p2p.Play(hash["DATA"] as IMoveData, (bool)hash["END_TURN"]);
						break;


					default: throw new ArgumentOutOfRangeException();
				}
			}
		}
		private PhotonPun photonPun;
	}
}
