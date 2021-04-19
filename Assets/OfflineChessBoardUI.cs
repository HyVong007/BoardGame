using Cysharp.Threading.Tasks;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace BoardGames
{
	public sealed class OfflineChessBoardUI : MonoBehaviour, ITurnListener
	{
		#region UI Fields
		[SerializeField] private Text turn, elapsedTurnTime;
		[SerializeField] private Image currentPlayerImage;
		[SerializeField] private Button buttonUndo, buttonRedo, buttonReplay, buttonBack;
		[field: SerializeField] public Button buttonSetting { get; private set; }
		[field: SerializeField] public Button buttonEnd { get; private set; }
		[field: SerializeField] public Button buttonSave { get; private set; }
		[field: SerializeField] public Button buttonLoad { get; private set; }
		#endregion


		public static OfflineChessBoardUI instance { get; private set; }
		private void Awake()
		{
			instance = instance ? throw new Exception() : this;
		}


		private void Start()
		{
			TurnManager.instance.AddListener(this);
		}


		private void FixedUpdate()
		{
			var t = TurnManager.instance;
			if (!t) return;

			var time = new TimeSpan(0, 0, (int)t.elapsedTurnTime);
			elapsedTurnTime.text = $"{time.Hours:00} : {time.Minutes:00} : {time.Seconds:00}";
		}


		#region Listener
		private readonly Dictionary<int, Sprite> playerID_sprite = new Dictionary<int, Sprite>();
		public void SetPlayerSprites(IDictionary<int, Sprite> playerID_sprite)
		{
			foreach (var kvp in playerID_sprite) this.playerID_sprite[kvp.Key] = kvp.Value;
		}


		public void OnTurnBegin()
		{
#if DEBUG
			if (playerID_sprite.Count == 0)
				throw new Exception("Board.Start() phải cài playerID_sprite: sprite của quân cờ/người chơi tương ứng playerID !");
#endif
			var t = TurnManager.instance;
			turn.text = $"Turn : {t.turn + 1}";
			currentPlayerImage.sprite = playerID_sprite[t.currentPlayerID];
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

		public void OnPlayerTimeOver(int playerID)
		{
		}

		public async UniTask<bool> OnReceiveRequest(int playerID, Request request) => true;



		public void OnTurnEnd()
		{
		}

		public void OnTurnTimeOver()
		{
		}
		#endregion
	}
}