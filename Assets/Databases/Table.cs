using Cysharp.Threading.Tasks;
using System.Collections.Generic;


namespace BoardGames.Databases
{
	/// <summary>
	/// Bàn chơi của 1 minigame, 1 <see cref="User"/> chỉ chơi tối đa 1 bàn chơi tại một thời điểm<para/>
	/// - Nếu <see cref="User"/> vào bàn chưa có chủ bàn (tất cả ghế trống) thì <see cref="User"/> là chủ bàn và có thể cài đặt bàn chơi, tạo mật khẩu bàn<br/>
	/// - Nếu bàn đã có chủ và còn ghế trống thì <see cref="User"/> khác từ bên ngoài có thể vào bàn (nếu bàn có mật khẩu thì <see cref="User"/> phải nhập đúng mật khẩu)
	/// </summary>
	public sealed class Table
	{
		public MiniGame game { get; set; }

		/// <summary>
		/// ID duy nhất trong phòng chơi<br/>
		/// Chỉ có ý nghĩa trong phòng chơi hiện tại
		/// </summary>
		public int localID { get; set; }

		public int chair { get; set; }

		public readonly IReadOnlyList<TablePlayer> players = new List<TablePlayer>();

		public TablePlayer host { get; set; }

		public int money { get; set; }

		public string password { get; set; }

		public bool isPlaying { get; set; }







		public TablePlayer FindPlayer(int playerID)
		{
			foreach (var player in players) if (player.id == playerID) return player;
			return null;
		}


		// test
		private static readonly List<Table> tables = new List<Table>();
		public Table()
		{
			tables.Add(this);
		}


		/// <summary>
		/// Bàn đang chơi hiện tại của 1 client<para/>
		/// Vào bàn thì cài, ra khỏi bàn thì reset <see langword="null"/>
		/// </summary>
		public static Table current { get; set; }

		public static async UniTask<Table> FindTable(MiniGame game, int tableLocalID)
		{
			// test
			foreach (var table in tables)
				if (table.game == game && table.localID == tableLocalID) return table;
			return null;
		}
	}
}