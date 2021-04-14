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


		/// <summary>
		/// gameObject có thể bị Destroy
		/// </summary>
		private new void Awake()
		{
			base.Awake();
			if (!gameObject.activeSelf) return;

			var config = "TURNBASE_CONFIG".GetValue<Config>();
			PhotonNetwork.AddCallbackTarget(photonPun = new PhotonPun(this));
		}


		private void OnDisable()
		{
			PhotonNetwork.RemoveCallbackTarget(photonPun);
		}


		private void Start()
		{
			PhotonNetwork.ConnectUsingSettings();
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
				throw new NotImplementedException();
			}
		}

		public override float ElapsedPlayerTime(int playerID)
		{
			throw new NotImplementedException();
		}
		#endregion



		public override int currentPlayerID => throw new NotImplementedException();


		public override async UniTask Play(IMoveData data, bool endTurn)
		{
		}


		public override void Quit()
		{
		}


		public override async UniTask<bool> SendRequest(Request request)
		{
			return true;
		}


		protected override async UniTask BeginTurn()
		{
		}


		protected override void FinishTurn()
		{
		}



		private sealed class PhotonPun : IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks, IOnEventCallback
		{
			private readonly P2PTurnManager p2p;
			public PhotonPun(P2PTurnManager p2p) => this.p2p = p2p;


			#region Connection Callbacks
			public void OnConnected()
			{
			}

			public void OnConnectedToMaster()
			{
				print("Tam: connect to master");
			}

			public void OnCustomAuthenticationFailed(string debugMessage)
			{
			}

			public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
			{
			}

			public void OnDisconnected(DisconnectCause cause)
			{
			}

			public void OnRegionListReceived(RegionHandler regionHandler)
			{
			}
			#endregion


			#region Matchmaking Callbacks
			public void OnCreatedRoom()
			{
			}

			public void OnCreateRoomFailed(short returnCode, string message)
			{
			}

			public void OnFriendListUpdate(List<FriendInfo> friendList)
			{
			}

			public void OnJoinedRoom()
			{
			}

			public void OnJoinRandomFailed(short returnCode, string message)
			{
			}

			public void OnJoinRoomFailed(short returnCode, string message)
			{
			}

			public void OnLeftRoom()
			{
			}
			#endregion


			#region InRoom Callbacks
			public void OnMasterClientSwitched(Player newMasterClient)
			{
			}

			public void OnPlayerEnteredRoom(Player newPlayer)
			{
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


			public void OnEvent(EventData photonEvent)
			{
			}
		}
		private PhotonPun photonPun;
	}
}
