using BoardGames.Databases;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace BoardGames
{
	public sealed class TableIcon : MonoBehaviour
	{
		[SerializeField] private Text _displayID;
		public int displayID { set => _displayID.text = value.ToString(); }

		[SerializeField] private Text _money;
		public int money { set => _money.text = value != 0 ? $"CƯỢC: {value}$" : ""; }

		[SerializeField] private GameObject _playingText;
		public bool isPlaying { set => _playingText.SetActive(value); }

		[SerializeField] private GameObject _locker;
		public bool hasPassword { set => _locker.SetActive(value); }
		[SerializeField] private Button _button;
		public Button button => _button;


		/// <summary>
		/// [số lượng ghế/player] = {tọa độ ghế/player}
		/// </summary>
		private static readonly IReadOnlyDictionary<int, int[]> count_indexs = new Dictionary<int, int[]>
		{
			[0] = Array.Empty<int>(),
			[1] = new int[] { 0 },
			[2] = new int[] { 0, 4 },
			[3] = new int[] { 0, 3, 5 },
			[4] = new int[] { 0, 2, 4, 6 },
			[5] = new int[] { 0, 1, 3, 5, 7 },
			[6] = new int[] { 0, 1, 3, 4, 5, 7 },
			[7] = new int[] { 0, 1, 2, 3, 4, 5, 6 },
			[8] = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 },
		};
		[SerializeField] private GameObject[] chairs;
		public int chairCount
		{
			set
			{
				foreach (var chair in chairs) chair.SetActive(false);
				foreach (int index in count_indexs[value]) chairs[index].SetActive(true);
			}
		}


		[SerializeField] private Image[] players;
		[SerializeField] private SerializableDictionaryBase<User.Sex, Sprite> sexSprites;
		public void SetPlayers(User.Sex[] sexes = null)
		{
			foreach (var player in players) player.gameObject.SetActive(false);
			if (sexes == null) return;
			int i = 0;
			foreach (int index in count_indexs[sexes.Length])
			{
				var player = players[index];
				player.sprite = sexSprites[sexes[i++]];
				player.gameObject.SetActive(true);
			}
		}


		private static readonly IReadOnlyDictionary<int, User.Sex[]> length_sexes = new Dictionary<int, User.Sex[]>();
		static TableIcon()
		{
			var length_sexes = TableIcon.length_sexes as Dictionary<int, User.Sex[]>;
			for (int i = 0; i < 9; ++i) length_sexes[i] = new User.Sex[i];
		}


		public void Import(Table table)
		{
			displayID = table.localID + 1;
			money = table.money;
			isPlaying = table.isPlaying;
			hasPassword = table.password.Length != 0;
			chairCount = table.chair;
			var sexes = length_sexes[table.players.Count];
			for (int i = 0; i < sexes.Length; ++i) sexes[i] = table.players[i].user.sex;
			SetPlayers(sexes);
		}
	}
}