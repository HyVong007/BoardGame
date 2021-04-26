using System;


namespace BoardGames.Databases
{
	public sealed class TablePlayer
	{
		public int id { get; set; }

		public User user { get; set; }

		public Table table { get; set; }

		public int betMoney { get; set; }








		private static TablePlayer _local;
		public static TablePlayer local
		{
			get
			{
				if (Table.current == null) return _local = null;
				if (_local != null) return _local;
				foreach (var player in Table.current.players)
					if (player.user == User.local) return _local = player;
				throw new Exception();
			}
		}
	}
}