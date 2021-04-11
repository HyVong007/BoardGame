using Cysharp.Threading.Tasks;
using UnityEngine;


namespace BoardGames
{
	public sealed class OfflineChessBoardUI : MonoBehaviour, IListener
	{
		private void Awake()
		{
			
		}




		private void Start()
		{
			TurnManager.instance.AddListener(this);
		}


		#region Listener
		public void OnGameFinish()
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

		public async UniTask<bool> OnReceiveRequest(int playerID, Request request)
		{
			return true;
		}

		public async UniTask OnTurnBegin()
		{
		}

		public void OnTurnEnd()
		{
		}

		public void OnTurnTimeOver()
		{
		}
		#endregion
	}
}