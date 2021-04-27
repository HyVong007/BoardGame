using BoardGames;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;


public class Test : MonoBehaviour, IConnectionCallbacks, IMatchmakingCallbacks, IOnEventCallback, IInRoomCallbacks
{
	private void OnEnable()
	{
		PhotonNetwork.AddCallbackTarget(this);
	}


	private void OnDisable()
	{
		PhotonNetwork.Disconnect();
		PhotonNetwork.RemoveCallbackTarget(this);
	}


	private void Start()
	{
		PhotonNetwork.NetworkingClient.LoadBalancingPeer.ReuseEventInstance = true;
		PhotonNetwork.ConnectUsingSettings();
	}


	public void OnConnected()
	{
	}

	public void OnConnectedToMaster()
	{
		PhotonNetwork.JoinOrCreateRoom("tam", null, null);
	}

	public void OnCreatedRoom()
	{
	}

	public void OnCreateRoomFailed(short returnCode, string message)
	{
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

	public void OnRegionListReceived(RegionHandler regionHandler)
	{
	}

	public void OnEvent(EventData photonEvent)
	{
		
	}


	public void OnPlayerEnteredRoom(Player newPlayer)
	{
		
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
}
