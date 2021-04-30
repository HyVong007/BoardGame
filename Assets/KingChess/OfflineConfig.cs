using BoardGames.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace BoardGames.KingChess
{
	public sealed class OfflineConfig : MonoBehaviour
	{
		[SerializeField] private Sprite icon;
		private void Awake()
		{
			"BOARD_CONFIG".SetValue(config);


		}


		private Board.Config config = new Board.Config();
		public static async UniTask<bool> ShowPopup()
		{
			var p = await "Popup".Instantiate<Popup>();
			var offlineConfig = await "KingChess Offline Config".Instantiate<OfflineConfig>();
			p.title.text = "CÀI ĐẶT CỜ VUA";
			p.icon.sprite = offlineConfig.icon;
			p.AddContent(offlineConfig.transform);
			p.AddContent((await "Offline Chess Turn Config".Instantiate()).transform);
			bool? ok = null;
			p.ok += () => ok = true;
			p.cancel += () => ok = false;
			await UniTask.WaitUntil(() => ok != null);
			return ok == true;
		}
	}
}