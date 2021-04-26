using BoardGames.Databases;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace BoardGames
{
	public sealed class OnlineChessTableUI : MonoBehaviour, ITurnListener
	{
		[Serializable]
		private sealed class PlayerInfo
		{
			public Image avatar, background;
			public Text name, money, betMoney, remainPlayerTime;
		}
		[SerializeField] private PlayerInfo[] playerInfos;
		[SerializeField] private Text turn, remainTurnTime;
		[SerializeField] private Image currentPlayerImage;
		[SerializeField] private Button buttonUndo, buttonSkip, buttonBack;
		[field: SerializeField] public Button buttonEnd { get; private set; }
		[field: SerializeField] public Button buttonMenu { get; private set; }


		public static OnlineChessTableUI instance { get; private set; }
		private void Awake()
		{
			instance = instance ? throw new Exception() : this;
			defaultBackgroundColor = playerInfos[0].background.color;

			// Chỉ bật buttonMenu khi client là host của Table

			buttonSkip.click += _ =>
			 {
				 buttonSkip.interactable = false;
				 TurnManager.instance.Play(null, true);
			 };


			// Nhập thông tin Player Info
			for (int i = 0; i < 2; ++i)
			{
				var player = Table.current.FindPlayer(i);
				var info = playerInfos[i];
				info.avatar.sprite = player.user.avatar;
				info.name.text = player.user.name;
				info.money.text = $"{player.user.money} $";
				info.betMoney.text = $"CƯỢC: {player.betMoney} $";
			}
		}


		private void Start()
		{
			var t = TurnManager.instance;
			t.AddListener(this);

			var @event = EventSystem.current;
			buttonUndo.click += async _ =>
			{
				@event.gameObject.SetActive(false);
				buttonUndo.interactable = !await t.SendRequest(Request.UNDO) || t.CanUndo(t.currentPlayerID);
				@event.gameObject.SetActive(true);
			};
		}


		private void FixedUpdate()
		{
			var t = TurnManager.instance;
			if (!t) return;

			var time = new TimeSpan(0, 0, (int)t.elapsedTurnTime);
			remainTurnTime.text = $"{time.Hours:00} : {time.Minutes:00} : {time.Seconds:00}";
		}


		#region Listen
		private readonly IReadOnlyDictionary<int, Sprite> playerID_sprite = new Dictionary<int, Sprite>();
		public void SetPlayerSprite(IDictionary<int, Sprite> playerID_sprite)
		{
			var dict = this.playerID_sprite as Dictionary<int, Sprite>;
			foreach (var kvp in playerID_sprite) dict[kvp.Key] = kvp.Value;
		}


		private Color defaultBackgroundColor;
		[SerializeField] private Color currentBackgroundColor;
		public void OnTurnBegin()
		{
			var t = TurnManager.instance;
			turn.text = $"TURN: {t.turn}";
			currentPlayerImage.sprite = playerID_sprite[t.currentPlayerID];
			playerInfos[1 - t.currentPlayerID].background.color = defaultBackgroundColor;
			playerInfos[t.currentPlayerID].background.color = currentBackgroundColor;

			if (t.CurrentPlayerIsLocalHuman())
			{
				buttonSkip.interactable = true;
				buttonUndo.interactable = t.CanUndo(t.currentPlayerID);
			}
		}


		public void OnTurnEnd(bool isTimeOver)
		{
			buttonSkip.interactable = buttonUndo.interactable = false;
		}


		public void OnGameOver()
		{
		}

		public async UniTask OnPlayerMove(IMoveData moveData, History.Mode mode)
		{

		}

		public void OnPlayerQuit(int playerID)
		{
		}

		public async UniTask<bool> OnReceiveRequest(int playerID, Request request)
		{
			return true;
		}
		#endregion
	}
}