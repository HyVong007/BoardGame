using BoardGames;
using go = BoardGames.GOChess;
using UnityEngine;


public class Test : MonoBehaviour
{
	private void Awake()
	{
		var core = new go.Core(new go.Color?[][]
		{
			new go.Color?[]{go.Color.White, null},
			new go.Color?[]{null, go.Color.Black}
		});

		string s = core.ToJson();
		print(s.FromJson<go.Core>());
	}
}
