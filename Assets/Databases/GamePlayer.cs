using Cysharp.Threading.Tasks;
using System;


namespace BoardGames.Databases
{
	public sealed class GamePlayer
	{
		public int id { get; set; }

		public User user { get; set; }

		public Table table { get; set; }


		public static GamePlayer local { get; private set; }


		public static GamePlayer Find(int playerID) => throw new NotImplementedException();
	}
}