using UnityEngine;


public class A : MonoBehaviour
{
	private void Awake()
	{
		Destroy(this);
		print(this != null);
	}
}
