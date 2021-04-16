using Cysharp.Threading.Tasks;
using UnityEngine;


namespace BoardGames.KingChess
{
	public sealed class Board : MonoBehaviour, ITurnListener
	{
		public void OnGameOver()
		{
			throw new System.NotImplementedException();
		}

		public UniTask OnPlayerMove(IMoveData data, History.Mode mode)
		{
			throw new System.NotImplementedException();
		}

		public void OnPlayerQuit(int playerID)
		{
			throw new System.NotImplementedException();
		}

		public void OnPlayerTimeOver(int playerID)
		{
			throw new System.NotImplementedException();
		}

		public UniTask<bool> OnReceiveRequest(int playerID, Request request)
		{
			throw new System.NotImplementedException();
		}

		public void OnTurnBegin()
		{
			throw new System.NotImplementedException();
		}

		public void OnTurnEnd()
		{
			throw new System.NotImplementedException();
		}

		public void OnTurnTimeOver()
		{
			throw new System.NotImplementedException();
		}
	}
}