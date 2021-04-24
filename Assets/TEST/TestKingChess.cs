using BoardGames;
using BoardGames.Databases;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using RotaryHeart.Lib.SerializableDictionary;
using System.Collections.Generic;
using UnityEngine;
using k = BoardGames.KingChess;


[DefaultExecutionOrder(-1)]
public class TestKingChess : MonoBehaviour
{
	public SerializableDictionaryBase<k.Color, bool> isHumanPlayer;


	private void Awake()
	{
		if (!"BOARD_CONFIG".TryGetValue(out k.Board.Config c) || c.core == null)
			"BOARD_CONFIG".SetValue(new k.Board.Config
		{
			
		});

		var config = new OfflineTurnManager.Config();
		var dict = config.isHumanPlayer as Dictionary<int, bool>;
		foreach (var kvp in isHumanPlayer) dict[(int)kvp.Key] = kvp.Value;
		"TURNBASE_CONFIG".SetValue(config);

		//"AI_CONFIG".SetValue(new BoardGames.AIAgent.Config { level = BoardGames.AIAgent.Level.Easy });
		//"TURNBASE_CONFIG".SetValue(new P2PTurnManager.Config());

		//User.local = new User();
		//TablePlayer.local = new TablePlayer { user = User.local };
	}


	//private async void Start()
	//{
	//	await UniTask.Yield(PlayerLoopTiming.Initialization);
	//	PhotonNetwork.ConnectUsingSettings();
	//}


	//private void OnDisable()
	//{
	//	PhotonNetwork.Disconnect();
	//}
}