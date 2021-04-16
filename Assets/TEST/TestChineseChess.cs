using BoardGames;
using BoardGames.Databases;
using c = BoardGames.ChineseChess;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using RotaryHeart.Lib.SerializableDictionary;
using System.Collections.Generic;
using UnityEngine;
using BoardGames.ChineseChess;
using UnityEngine.InputSystem;


[DefaultExecutionOrder(-1)]
public class TestChineseChess : MonoBehaviour
{
	public SerializableDictionaryBase<c.Color, bool> isHumanPlayer;


	private void Awake()
	{
		var bc = new c.Board.Config();
		bc.mailBox = Core.CloneDefaultMailBox();
		for (int x = 0; x < 9; ++x)
			for (int y = 0; y < 10; ++y)
			{
				//var piece = bc.mailBox[x][y];
				//if (piece == null || piece.Value.name == PieceName.General) continue;
				//bc.mailBox[x][y] = new Piece(piece.Value.color, piece.Value.name, true);
			}



		"BOARD_CONFIG".SetValue(bc);

		//var config = new OfflineTurnManager.Config();
		//var dict = config.isHumanPlayer as Dictionary<int, bool>;
		//foreach (var kvp in isHumanPlayer) dict[(int)kvp.Key] = kvp.Value;
		//"TURNBASE_CONFIG".SetValue(config);

		//"AI_CONFIG".SetValue(new BoardGames.AIAgent.Config { level = BoardGames.AIAgent.Level.Easy });
		"TURNBASE_CONFIG".SetValue(new P2PTurnManager.Config());

		User.local = new User();
		TablePlayer.local = new TablePlayer { user = User.local };
	}


	private async void Start()
	{
		await UniTask.Yield(PlayerLoopTiming.Initialization);
		PhotonNetwork.ConnectUsingSettings();
	}


	private void OnDisable()
	{
		PhotonNetwork.Disconnect();
	}


	private void Update()
	{
		if (Keyboard.current.spaceKey.wasPressedThisFrame) PieceGUI.isSymbol = !PieceGUI.isSymbol;
	}
}