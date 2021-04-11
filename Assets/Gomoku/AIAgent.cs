using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;


namespace BoardGames.Gomoku
{
	public sealed class AIAgent : BoardGames.AIAgent
	{
		private new void Start()
		{
			base.Start();
			var core = Board.instance.core;
			var rect = core.rect;
			for (int x = 0; x < rect.width; ++x)
				for (int y = 0; y < rect.height; ++y)
					if (core[x, y] == null) emptyCells.Add(new Vector2Int(x, y));
		}


		private readonly List<Vector2Int> emptyCells = new List<Vector2Int>();
		[SerializeField] private int delay;

		public override async UniTask<IMoveData> GenerateMoveData()
		{
			await UniTask.Delay(delay);
			return emptyCells.Count == 0 ? null
				: new Core.MoveData((Symbol)TurnManager.instance.currentPlayerID,
					emptyCells[Random.Range(0, emptyCells.Count)]);
		}


		#region Listen
		public async override UniTask OnTurnBegin()
		{
#if UNITY_EDITOR
			try
			{
				// Kiểm tra xem có bị Destroy chưa ?
				var _ = gameObject;
			}
			catch { return; }
#endif
			var t = TurnManager.instance as OfflineTurnManager;
			if (t.IsHumanPlayer(t.currentPlayerID)) return;

			//__();
			//async void __()
			//{
			//	await t.Play(await GenerateMoveData(), true);
			//}

			t.Play(await GenerateMoveData(), true).Forget();
		}


		public async override UniTask OnPlayerMove(IMoveData moveData, History.Mode mode)
		{
			var data = moveData as Core.MoveData;
			if (mode == History.Mode.Undo) emptyCells.Add(data.index);
			else emptyCells.Remove(data.index);
		}


		public override void OnTurnEnd()
		{
		}


		public override void OnGameFinish()
		{
			Destroy(gameObject);
		}
		#endregion
	}
}