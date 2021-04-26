using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;


public class A : MonoBehaviour, IOnEventCallback
{

	public enum E : byte
	{
		A
	}





	private void Awake()
	{
		var hash = new Hashtable();
		hash[E.A] = "This is the value of hash !";
		PhotonNetwork.RaiseEvent(0, hash,
			new RaiseEventOptions { Receivers = ReceiverGroup.All, CachingOption = EventCaching.AddToRoomCache },
			SendOptions.SendReliable);
	}


	public void OnEvent(EventData photonEvent)
	{
		if (photonEvent.Code >= 200) return;

		var hash = photonEvent.CustomData as Hashtable;
		var value = hash[E.A];
	}
}
