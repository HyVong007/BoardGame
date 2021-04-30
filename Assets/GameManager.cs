using BoardGames.Databases;
using BoardGames.Utils;
using Cysharp.Threading.Tasks;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace BoardGames
{
	public enum MiniGame
	{
		Gomoku,
		GOChess,
		ChineseChess,
		KingChess,
		BattleShip
	}



	public sealed class GameManager : MonoBehaviour
	{
		[Serializable]
		private sealed class TopPannel
		{
			[SerializeField] private Button back;


			public void Awake()
			{
				//back.click += _ => Application.Quit();
			}
		}
		[SerializeField] private TopPannel topPannel;


		[Serializable]
		private sealed class BottomPannel
		{
		}
		[SerializeField] private BottomPannel bottomPannel;


		[Serializable]
		private sealed class OnlineTableView
		{
			public GameObject gameObject;


			[Serializable]
			public sealed class GameTables
			{
				public GameObject gameObject;
				[SerializeField] private ObjectPool<TableIcon> pool;
				private readonly Dictionary<int, TableIcon> tableUIs = new Dictionary<int, TableIcon>();


				public async UniTask CreateEmptyTableUIs(int count)
				{
					for (int i = 0; i < count; ++i)
					{
						var ui = tableUIs[i] = pool.Get();
						ui.transform.localScale = new Vector3(1, 1, 1);
						ui.displayID = i + 1;
						ui.money = 0;
						ui.isPlaying = false;
						ui.hasPassword = false;
						ui.chairCount = 2;
						ui.SetPlayers(null);
						await UniTask.Yield();
					}

					var t0 = tableUIs[0];
					t0.chairCount = 8;
					t0.hasPassword = true;
					t0.money = 123456;
					t0.isPlaying = true;
					t0.SetPlayers(new User.Sex[]
					{
						User.Sex.Boy, User.Sex.Girl,
						User.Sex.Boy, User.Sex.Girl,
						User.Sex.Boy, User.Sex.Girl,
						User.Sex.Boy, User.Sex.Girl
					});
				}
			}
			public SerializableDictionaryBase<MiniGame, GameTables> gameTables;


			[Serializable]
			public sealed class BottomPanel
			{
				[SerializeField] private GameObject gameObject;
				[SerializeField] private Button playOfflineButton, chatButton, findTableButton;
			}
			public BottomPanel bottomPanel;
		}
		[SerializeField] private OnlineTableView onlineTableView;


		[SerializeField] private SerializableDictionaryBase<MiniGame, Button> gameButtons;
		[SerializeField] private ScrollRect gameMenu;


		private static GameManager instance;
		private void Awake()
		{
			DontDestroyOnLoad(instance = instance ? throw new Exception() : this);
			//DontDestroyOnLoad(Camera.main); // test
			topPannel.Awake();
			foreach (var game_button in gameButtons) game_button.Value.click += _ => SelectGame(game_button.Key);
		}


		private void Start()
		{
			var canvas = GetComponent<Canvas>();
			SceneManager.sceneLoaded += (_, __) => canvas.enabled = false;
			var main = SceneManager.GetActiveScene();
			SceneManager.sceneUnloaded += _ => canvas.enabled = SceneManager.GetActiveScene() == main;
		}







		private async void SelectGame(MiniGame game)
		{
			switch (game)
			{
				case MiniGame.Gomoku:
					if (await Gomoku.OfflineConfig.ShowPopup())
						await "Gomoku/Scene/Offline Board".LoadScene(true);
					break;

				case MiniGame.GOChess:
					if (await GOChess.OfflineConfig.ShowPopup())
						await "GOChess/Scene/Offline Board".LoadScene(true);
					break;

				case MiniGame.ChineseChess:
					if (await ChineseChess.OfflineConfig.ShowPopup())
						await "ChineseChess/Scene/Offline Board".LoadScene(true);
					break;

				case MiniGame.KingChess:
					if (await KingChess.OfflineConfig.ShowPopup())
						await "KingChess/Scene/Offline Board".LoadScene(true);
					break;

				case MiniGame.BattleShip:
					print("battleship");
					break;

				default: throw new ArgumentOutOfRangeException();
			}
		}


		private IReadOnlyList<Table> TestData(MiniGame game)
		{
			var tables = new List<Table>(100);
			for (int t = 0; t < tables.Capacity; ++t)
			{
				var table = new Table()
				{
					localID = t,
					chair = 8,
					game = game,
					isPlaying = true,
					money = 123456,
					password = "123"
				};
				tables.Add(table);

				var list = new List<TablePlayer>();
				for (int i = 0; i < 8; ++i)
					list.Add(new TablePlayer()
					{
						table = table,
						user = new User() { sex = (User.Sex)(i % 2) }
					});
				//table.players = list;
				table.host = list[0];
			}
			return tables;
		}












		// Đang có BUG: Nếu đóng bằng taskbaricon hoặc Alt+F4 thì có vấn đề không nhấn vô popup được.
		#region Xác nhận khi người chơi muốn thoát
		[SerializeField] private Popup confirmQuitPopup;

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
		private static void δ()
		{
			Popup p = null;
			Application.wantsToQuit += Confirm;


			bool Confirm()
			{
				WinStandalone.Maximize();
				if (!instance || p) return false;

				p = Instantiate(instance.confirmQuitPopup);
				p.ok += () =>
				{
					Application.wantsToQuit -= Confirm;
					Application.Quit(); // Nên xử lý trước khi thoát !
				};
				p.gameObject.SetActive(true);
				return false;
			}
		}
		#endregion
	}
}