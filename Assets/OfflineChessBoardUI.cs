using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace BoardGames
{
	public sealed class OfflineChessBoardUI : MonoBehaviour, ITurnListener
	{
		#region UI Fields
		[SerializeField] private Text turn, elapsedTurnTime;
		[SerializeField] private Image currentPlayerImage;
		[SerializeField] private Button buttonUndo, buttonRedo, buttonReplay, buttonBack;
		/// <summary>
		/// event handler: Trước khi bắt đầu công việc: đảm bảo người chơi không thể tương tác với game<br/>
		/// Sau khi hoàn thành công việc: khôi phục tương tác của người chơi với game 
		/// </summary>
		[field: SerializeField] public Button buttonSetting { get; private set; }
		/// <summary>
		/// event handler: Trước khi bắt đầu công việc: đảm bảo người chơi không thể tương tác với game<br/>
		/// Sau khi hoàn thành công việc: khôi phục tương tác của người chơi với game 
		/// </summary>
		[field: SerializeField] public Button buttonEnd { get; private set; }
		/// <summary>
		/// event handler: Trước khi bắt đầu công việc: đảm bảo người chơi không thể tương tác với game<br/>
		/// Sau khi hoàn thành công việc: khôi phục tương tác của người chơi với game 
		/// </summary>
		[field: SerializeField] public Button buttonSave { get; private set; }
		/// <summary>
		/// event handler: Trước khi bắt đầu công việc: đảm bảo người chơi không thể tương tác với game<br/>
		/// Sau khi hoàn thành công việc: khôi phục tương tác của người chơi với game 
		/// </summary>
		[field: SerializeField] public Button buttonLoad { get; private set; }
		#endregion


		public static OfflineChessBoardUI instance { get; private set; }
		private async void Awake()
		{
			instance = instance ? throw new Exception() : this;
			await UniTask.DelayFrame(2);
			var scene = SceneManager.GetActiveScene();
			buttonBack.click += _ => scene.buildIndex.UnloadScene().Forget();
			buttonReplay.click += _ => Util.ReloadActiveScene(true).Forget();
		}


		private void Start()
		{
			var t = TurnManager.instance;
			t.AddListener(this);

			var e = EventSystem.current;
			buttonUndo.click += _ => SendRequest(Request.UNDO);
			buttonRedo.click += _ => SendRequest(Request.REDO);


			async void SendRequest(Request request)
			{
				e.enabled = false;
				await t.SendRequest(request);
				buttonUndo.interactable = t.CanUndo(t.currentPlayerID);
				buttonRedo.interactable = t.CanRedo(t.currentPlayerID);
				e.enabled = true;
			}
		}


		private void FixedUpdate()
		{
			var t = TurnManager.instance;
			if (!t) return;

			var time = new TimeSpan(0, 0, (int)t.elapsedTurnTime);
			elapsedTurnTime.text = $"{time.Hours:00} : {time.Minutes:00} : {time.Seconds:00}";
		}


		#region Listener
		private readonly IReadOnlyDictionary<int, Sprite> playerID_sprite = new Dictionary<int, Sprite>();
		public void SetPlayerSprites(IDictionary<int, Sprite> playerID_sprite)
		{
			var dict = this.playerID_sprite as Dictionary<int, Sprite>;
			foreach (var kvp in playerID_sprite) dict[kvp.Key] = kvp.Value;
		}


		public void OnTurnBegin()
		{
			var t = TurnManager.instance;
			turn.text = $"Turn : {t.turn}";
			currentPlayerImage.sprite = playerID_sprite[t.currentPlayerID];

			if (t.CurrentPlayerIsLocalHuman())
			{
				buttonSave.interactable = true;
				buttonUndo.interactable = t.CanUndo(t.currentPlayerID);
				buttonRedo.interactable = t.CanRedo(t.currentPlayerID);
			}
		}


		public async UniTask OnPlayerMove(IMoveData moveData, History.Mode mode)
		{
			if (TurnManager.instance.IsGameOver()) buttonSave.interactable = false;
		}


		public void OnTurnEnd(bool isTimeOver)
		{
			buttonSave.interactable = buttonUndo.interactable = buttonRedo.interactable = false;
		}


		public void OnGameOver()
		{
		}


		public void OnPlayerQuit(int playerID)
		{
		}


		public async UniTask<bool> OnReceiveRequest(int playerID, Request request) => true;
		#endregion
	}
}