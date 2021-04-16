using BoardGames;
using UnityEngine;


public class Test : MonoBehaviour
{
	private void Awake()
	{
		var v = new Vector2();
		A(ref v);
		print(v);
	}


	void A(ref Vector2 v)
	{
		var t = v;
		t.x = 123;
	}
}


