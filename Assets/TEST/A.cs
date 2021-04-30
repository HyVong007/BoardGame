using BoardGames;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;


public class A : MonoBehaviour
{
	private async void Awake()
	{
		var p = await "Popup".Instantiate();
		print(p);
	}
}
