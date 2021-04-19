using BoardGames.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Test : MonoBehaviour
{
	private void Update()
	{
		if (Keyboard.current.spaceKey.wasPressedThisFrame) UniTask.RunOnThreadPool(WinStandalone.Maximize);
	}
}