using BoardGames;
using BoardGames.Gomoku;
using RotaryHeart.Lib.SerializableDictionary;
using System.Collections.Generic;
using UnityEngine;


[DefaultExecutionOrder(-1)]
public class TestGomoku : MonoBehaviour
{
	public Vector2Int size;
	public SerializableDictionaryBase<Symbol, bool> isHumanPlayer;


	private void Awake()
	{
		"BOARD_CONFIG".SetValue(new Board.Config
		{
			size = size,
		});

		var config = new OfflineTurnManager.Config();
		var dict = config.isHumanPlayer as Dictionary<int, bool>;
		foreach (var kvp in isHumanPlayer) dict[(int)kvp.Key] = kvp.Value;
		//"TURNBASE_CONFIG".SetValue(config);

		//"AI_CONFIG".SetValue(new BoardGames.AIAgent.Config { level = BoardGames.AIAgent.Level.Easy });
		"TURNBASE_CONFIG".SetValue(new P2PTurnManager.Config());
	}
}