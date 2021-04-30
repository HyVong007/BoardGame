using BoardGames.Utils;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace BoardGames.Gomoku
{
	public sealed class OfflineConfig : MonoBehaviour
	{
		[SerializeField] private Dropdown size;
		private const string SMALL = "NHỎ (16X16)", MEDIUM = "TRUNG BÌNH (32X32)", LARGE = "LỚN (48X48)", GIANT = "KHỔNG LỒ (100X100)";
		private static readonly IReadOnlyDictionary<string, Vector2Int> SIZES = new Dictionary<string, Vector2Int>
		{
			[SMALL] = new Vector2Int(16, 16),
			[MEDIUM] = new Vector2Int(32, 32),
			[LARGE] = new Vector2Int(48, 48),
			[GIANT] = new Vector2Int(100, 100)
		};
		private static readonly List<Dropdown.OptionData> OPTIONS = new List<Dropdown.OptionData>
		{
			new Dropdown.OptionData(SMALL),
			new Dropdown.OptionData(MEDIUM),
			new Dropdown.OptionData(LARGE),
			new Dropdown.OptionData(GIANT)
		};
		[SerializeField] private Sprite icon;

		private Board.Config config = new Board.Config();
		private void Awake()
		{
			size.options = OPTIONS;
			SizeChanged(size.value);
			size.onValueChanged.AddListener(SizeChanged);
			"BOARD_CONFIG".SetValue(config);


			void SizeChanged(int index)
			{
				config.size = SIZES[size.options[index].text];
				"BOARD_CONFIG".SetValue(config);
			}
		}


		public static async UniTask<bool> ShowPopup()
		{
			var p = await "Popup".Instantiate<Popup>();
			var offlineConfig = await "Gomoku Offline Config".Instantiate<OfflineConfig>();
			p.title.text = "CÀI ĐẶT CỜ CA RÔ";
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

