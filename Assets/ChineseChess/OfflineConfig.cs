using BoardGames.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


namespace BoardGames.ChineseChess
{
	public sealed class OfflineConfig : MonoBehaviour
	{
		[SerializeField] private Toggle toggleHiddenRule, toggleSymbol;
		[SerializeField] private Sprite icon;
		private Board.Config config = new Board.Config();
		private void Awake()
		{
			var HIDDEN_MAILBOX = Core.CloneDefaultMailBox();
			for (int x = 0; x < 9; ++x)
				for (int y = 0; y < 10; ++y)
				{
					var p = HIDDEN_MAILBOX[x][y];
					if (p == null || p.Value.name == PieceName.General) continue;
					HIDDEN_MAILBOX[x][y] = new Piece(p.Value.color, p.Value.name, true);
				}
			config.mailBox = toggleHiddenRule.isOn ? HIDDEN_MAILBOX : null;
			PieceGUI.isSymbol = toggleSymbol.isOn;
			"BOARD_CONFIG".SetValue(config);
			toggleHiddenRule.onValueChanged.AddListener(isOn =>
			{
				config.mailBox = isOn ? HIDDEN_MAILBOX : null;
				"BOARD_CONFIG".SetValue(config);
			});
			toggleSymbol.onValueChanged.AddListener(isOn => PieceGUI.isSymbol = isOn);
		}


		public static async UniTask<bool> ShowPopup()
		{
			var p = await "Popup".Instantiate<Popup>();
			var offlineConfig = await "ChineseChess Offline Config".Instantiate<OfflineConfig>();
			p.title.text = "CÀI ĐẶT CỜ TƯỚNG";
			p.icon.sprite = offlineConfig.icon;
			p.AddContent(offlineConfig.transform);
			p.AddContent((await "Offline Chess Turn Config".Instantiate()).transform, 20);
			bool? ok = null;
			p.ok += () => ok = true;
			p.cancel += () => ok = false;
			await UniTask.WaitUntil(() => ok != null);
			return ok == true;
		}
	}
}