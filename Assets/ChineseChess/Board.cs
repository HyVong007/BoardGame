using Cysharp.Threading.Tasks;
using UnityEngine;


namespace BoardGames.ChineseChess
{
	public sealed class Board : MonoBehaviour, IListener
	{
		public void OnGameFinish()
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

		public UniTask OnPlayerTimeOver(int playerID)
		{
			throw new System.NotImplementedException();
		}

		public UniTask<bool> OnRequestReceive(int playerID, Request request)
		{
			throw new System.NotImplementedException();
		}

		public UniTask OnTurnBegin()
		{
			throw new System.NotImplementedException();
		}

		public UniTask OnTurnEnd()
		{
			throw new System.NotImplementedException();
		}

		public UniTask OnTurnTimeOver()
		{
			throw new System.NotImplementedException();
		}
	}
}