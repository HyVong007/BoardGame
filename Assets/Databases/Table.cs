using Cysharp.Threading.Tasks;
using System;
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

		public IReadOnlyList<GamePlayer> players { get; set; }

		public GamePlayer host { get; set; }

		public int money { get; set; }

		public string password { get; set; }

		public bool isPlaying { get; set; }


		public static async UniTask<Table> Find(MiniGame game, int tableLocalID) => throw new NotImplementedException();
	}
}