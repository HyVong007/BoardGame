using UnityEditor;
using UnityEngine;


namespace BoardGames.Editor
{
	internal static class Menu
	{
		[MenuItem("Ingame Debug Console", menuItem = "GameObject/Ingame Debug Console", priority = 0)]
		private static void Create_IngameDebugConsole()
		{
			Selection.activeObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<Object>(
				"packages/com.yasirkula.ingamedebugconsole/Plugins/IngameDebugConsole/IngameDebugConsole.prefab"),
				Selection.activeTransform);
		}
	}
}