using Cysharp.Threading.Tasks;
using UnityEngine;


namespace BoardGames
{
	public sealed class OfflineChessBoardUI : MonoBehaviour, ITurnListener
	{
		private void Awake()
		{
			
		}




		private void Start()
		{
			TurnManager.instance.AddListener(this);
		}


		#region Listener
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

		public async UniTask<bool> OnReceiveRequest(int playerID, Request request)
		{
			return true;
		}

		public async void OnTurnBegin()
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