using Cysharp.Threading.Tasks;
using System;


namespace BoardGames.Databases
{
	public sealed class TablePlayer
	{
		public int id { get; set; }

		public User user { get; set; }

		public Table table { get; set; }


		public static TablePlayer local { get; private set; }


		public static TablePlayer Find(int playerID) => throw new NotImplementedException();
	}
}