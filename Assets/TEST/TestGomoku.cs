using BoardGames;
using BoardGames.Databases;
using BoardGames.Gomoku;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.Collections.Generic;
using UnityEngine;


[DefaultExecutionOrder(-1)]
public class TestGomoku : MonoBehaviour, IMatchmakingCallbacks, IConnectionCallbacks, IInRoomCallbacks, IOnEventCallback
{
	public Vector2Int size;
	public SerializableDictionaryBase<Symbol, bool> isHumanPlayer;
	public bool online;

	public float maxTurnTime, maxPlayerTime;


	[Serializable]
	private sealed class PlayerInfo
	{
		public Sprite avatar;
		public string name;
		public User.Sex sex;
		public int money, betMoney;
	}
	[SerializeField] private PlayerInfo[] playerInfos;


	private void Awake()
	{
		if (!"BOARD_CONFIG".TryGetValue(out Board.Config c) || c.core == null)
			"BOARD_CONFIG".SetValue(new Board.Config
			{
				size = size,
			});

		var config = new OfflineTurnManager.Config();
		var dict = config.isHumanPlayer as Dictionary<int, bool>;
		foreach (var kvp in isHumanPlayer) dict[(int)kvp.Key] = kvp.Value;
		if (!online) "TURNBASE_CONFIG".SetValue(config);

		"TURNBASE_CONFIG".SetValue(new P2PTurnManager.Config
		{
			playerCount = 2,
			maxTurnTime = maxTurnTime,
			maxPlayerTime = maxPlayerTime
		});

		// test
		Table.current = new Table { chair = 2, game = MiniGame.Gomoku, localID = 0, isPlaying = true };

		for (int i = 0; i < 2; ++i)
		{
			// cài local
			var user = new User
			{
				id = i,
				avatar = playerInfos[i].avatar,
				money = playerInfos[i].money,
				name = playerInfos[i].name,
				sex = playerInfos[i].sex
			};

			var player = new TablePlayer
			{
				id = i,
				betMoney = playerInfos[i].betMoney,
				table = Table.current,
				user = user
			};

			(Table.current.players as List<TablePlayer>).Add(player);
			if (i == 0) Table.current.host = player;
		}

		PhotonNetwork.AddCallbackTarget(this);
	}


	private void Start()
	{
		if (!online) return;
		PhotonNetwork.NetworkingClient.LoadBalancingPeer.ReuseEventInstance = true;
		PhotonNetwork.ConnectUsingSettings();
	}


	private void OnDisable()
	{
		if (!online) return;
		PhotonNetwork.Disconnect();
	}


	#region Connection
	public void OnConnected()
	{
	}

	public void OnConnectedToMaster()
	{
		PhotonNetwork.JoinOrCreateRoom("Tam", null, null);
	}

	public void OnDisconnected(DisconnectCause cause)
	{
	}

	public void OnRegionListReceived(RegionHandler regionHandler)
	{
	}

	public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
	{
	}

	public void OnCustomAuthenticationFailed(string debugMessage)
	{
	}
	#endregion


	#region Match Matching
	public void OnJoinedRoom()
	{
		User.local = Table.current.FindPlayer(PhotonNetwork.IsMasterClient ? 0 : 1).user;
	}


	public void OnFriendListUpdate(List<FriendInfo> friendList)
	{
	}

	public void OnCreatedRoom()
	{
	}

	public void OnCreateRoomFailed(short returnCode, string message)
	{
	}


	public void OnJoinRoomFailed(short returnCode, string message)
	{
	}

	public void OnJoinRandomFailed(short returnCode, string message)
	{
	}

	public void OnLeftRoom()
	{
	}
	#endregion


	#region In Room
	public void OnPlayerEnteredRoom(Player newPlayer)
	{
		if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
		{
			PhotonNetwork.RaiseEvent(BoardGames.EventCode.Ready, null,
				new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache },
				SendOptions.SendReliable);

			TurnManager.instance.StartPlaying();
		}
	}

	public void OnPlayerLeftRoom(Player otherPlayer)
	{
	}

	public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
	{
	}

	public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
	{
	}

	public void OnMasterClientSwitched(Player newMasterClient)
	{
	}
	#endregion


	public void OnEvent(EventData photonEvent)
	{
		if (photonEvent.Code >= 200) return;
		if (photonEvent.Code == BoardGames.EventCode.Ready)
			TurnManager.instance.StartPlaying();
	}
}