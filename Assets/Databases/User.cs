using UnityEngine;


namespace BoardGames.Databases
{
	public sealed class User
	{
		public int id { get; set; }

		public enum Sex
		{
			Boy, Girl, Other
		}
		public Sex sex { get; set; }

		public Sprite avatar;

		public string name { get; set; }

		public int money { get; set; }




		public static User local { get; set; }
	}
}