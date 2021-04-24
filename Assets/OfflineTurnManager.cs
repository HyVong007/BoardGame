using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace BoardGames
{
	public sealed class OfflineTurnManager : TurnManager
	{
		[Serializable]
		public new class Config : TurnManager.Config
		{
			public readonly IReadOnlyDictionary<int, bool> isHumanPlayer = new Dictionary<int, bool>();
		}


		[Serializable]
		public struct SaveData
		{
			public Config config;
			public int turn, currentPlayerID;
			public History history;


			public SaveData(OfflineTurnManager t)
			{
				config = "TURNBASE_CONFIG".GetValue<Config>();
				turn = t.turn;
				currentPlayerID = t.currentPlayerID;
				history = new History(t.history);
			}
		}


		private readonly Queue<(IMoveData data, History.Mode mode)> moveQueues
			= new Queue<(IMoveData data, History.Mode mode)>();

		private readonly IReadOnlyDictionary<int, bool> isHumanPlayer = new Dictionary<int, bool>();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsHumanPlayer(int playerID) => isHumanPlayer[playerID];
		[SerializeField] private AIAgent aiPrefab;


		private async new void Awake()
		{
			base.Awake();
			if (!gameObject.activeSelf) return;
			Config config = null;

			#region Load SaveData
			if ("TURN_SAVE_DATA".TryGetValue(out SaveData data))
			{
				"TURN_SAVE_DATA".Remove();
				config = data.config;
				checked { turn = data.turn - 1; }
				playerIDGenerator = PlayerIDGenerator(data.config.isHumanPlayer.Count, data.currentPlayerID);
				history = data.history;
			}
			#endregion

			history.execute += (data, mode) => moveQueues.Enqueue((data, mode));
			config ??= "TURNBASE_CONFIG".GetValue<Config>();
			for (int i = config.isHumanPlayer.Count - 1; i >= 0; --i)
			{
				playerStartTimes[i] = float.MaxValue;
				elapsedPlayerTimes[i] = 0;
			}
			playerIDGenerator ??= PlayerIDGenerator(config.isHumanPlayer.Count);

			#region isHumanPlayer và sinh AI
			var isHumanPlayer = this.isHumanPlayer as Dictionary<int, bool>;
			bool allIsHuman = true;
			foreach (var kvp in config.isHumanPlayer) allIsHuman &= isHumanPlayer[kvp.Key] = kvp.Value;
			if (!allIsHuman) Instantiate(aiPrefab, transform);
			#endregion

			await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
			BeginTurn();
		}


		public sealed override int currentPlayerID => playerIDGenerator.Current;
		private IEnumerator<int> playerIDGenerator;
		private static IEnumerator<int> PlayerIDGenerator(int maxPlayer, int currentPlayerID = 0)
		{
			for (int i = currentPlayerID; i < maxPlayer; ++i) yield return i;
			while (true) for (int i = 0; i < maxPlayer; ++i) yield return i;
		}


		protected override void BeginTurn()
		{
#if UNITY_EDITOR
			try
			{
				// Kiểm tra xem có bị Destroy chưa ?
				var _ = gameObject;
			}
			catch { return; }
#endif
			checked { ++turn; }
			playerIDGenerator.MoveNext();
			foreach (var listener in listeners) listener.OnTurnBegin();
			cacheElapsedTurnTime = 0;
			countTime = true;
		}


		protected override void FinishTurn()
		{
			countTime = false;
			foreach (var listener in listeners) listener.OnTurnEnd(false);
			if (IsGameOver())
			{
				foreach (var listener in listeners) listener.OnGameOver();
				Destroy(gameObject);
			}
			else UniTask.Yield(PlayerLoopTiming.Update, default).ContinueWith(BeginTurn).Forget();
		}


		public async override UniTask Play(IMoveData data, bool endTurn)
		{
			if (data == null)
			{
				FinishTurn();
				return;
			}

			history.Play(data);
			await ExecuteMoveQueue();
			if (endTurn) FinishTurn();
		}


		public override void Quit()
		{
			countTime = false;
			foreach (var listener in listeners) listener.OnGameOver();
			Destroy(gameObject);
		}


		public override async UniTask<bool> SendRequest(Request request)
		{
			switch (request)
			{
				case Request.DRAW:
					foreach (var listener in listeners) listener.OnGameOver();
					Destroy(gameObject);
					break;

				case Request.UNDO:
					history.Undo(currentPlayerID);
					await ExecuteMoveQueue();
					break;

				case Request.REDO:
					history.Redo(currentPlayerID);
					await ExecuteMoveQueue();
					break;
			}

			return true;
		}


		private readonly List<UniTask> moveTasks = new List<UniTask>();
		private async UniTask ExecuteMoveQueue()
		{
			countTime = false;
			do
			{
				var (data, mode) = moveQueues.Dequeue();
				moveTasks.Clear();
				foreach (var listener in listeners) moveTasks.Add(listener.OnPlayerMove(data, mode));
				await UniTask.WhenAll(moveTasks);
			} while (moveQueues.Count != 0);
			countTime = true;
		}


		#region Time
		public sealed override float elapsedTurnTime => countTime ? Time.time - turnStartTime : cacheElapsedTurnTime;
		public sealed override float ElapsedPlayerTime(int playerID)
			=> playerID != currentPlayerID || !countTime ? elapsedPlayerTimes[playerID]
			: Time.time - playerStartTimes[playerID];


		private bool ΔcountTime;
		/// <summary>
		/// <see langword="true"/> : tiếp tục đếm thời gian, giữ nguyên elapse hiện tại<br/>
		/// <see langword="false"/> : tạm ngưng, lưu elapse vào cache
		/// </summary>
		private bool countTime
		{
			get => ΔcountTime;

			set
			{
				if (value == ΔcountTime) return;
				ΔcountTime = value;
				if (value)
				{
					turnStartTime = Time.time - cacheElapsedTurnTime;
					playerStartTimes[currentPlayerID] = Time.time - elapsedPlayerTimes[currentPlayerID];
				}
				else
				{
					cacheElapsedTurnTime = Time.time - turnStartTime;
					elapsedPlayerTimes[currentPlayerID] = Time.time - playerStartTimes[currentPlayerID];
				}
			}
		}
		#endregion


		#region NotSupported
		public sealed override float remainTurnTime => throw new NotSupportedException();

		public sealed override float RemainPlayerTime(int playerID) => throw new NotSupportedException();
		#endregion
	}
}