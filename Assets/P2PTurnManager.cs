using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;


namespace BoardGames
{
	public sealed class P2PTurnManager : TurnManager
	{
		public new sealed class Config : TurnManager.Config
		{

		}


		/// <summary>
		/// gameObject có thể bị Destroy
		/// </summary>
		private new void Awake()
		{
			base.Awake();
			if (!gameObject.activeSelf) return;

			var config = "TURNBASE_CONFIG".GetValue<Config>();
		}


		#region Network Time
		private float maxTurnTime;
		private readonly Dictionary<int, float> maxPlayerTimes = new Dictionary<int, float>();


		public override sealed float remainTurnTime => maxTurnTime - elapsedTurnTime;


		public override sealed float RemainPlayerTime(int playerID) => maxPlayerTimes[playerID] - ElapsedPlayerTime(playerID);


		public override float elapsedTurnTime => throw new NotImplementedException();


		public override float ElapsedPlayerTime(int playerID)
		{
			throw new NotImplementedException();
		}
		#endregion



		public override int currentPlayerID => throw new NotImplementedException();


		public override UniTask Play(IMoveData data, bool endTurn)
		{
			throw new NotImplementedException();
		}

		public override void Quit()
		{
			throw new NotImplementedException();
		}


		public override UniTask<bool> SendRequest(Request request)
		{
			throw new NotImplementedException();
		}

		protected override UniTask BeginTurn()
		{
			throw new NotImplementedException();
		}

		protected override void FinishTurn()
		{
			throw new NotImplementedException();
		}
	}
}