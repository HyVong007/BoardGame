using ExitGames.Client.Photon;
using Newtonsoft.Json;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using UnityEngine;


namespace BoardGames.GOChess
{
	public enum Color
	{
		White = 0, Black = 1
	}



	[DataContract]
	public sealed class Land
	{
		[DataMember] public int airHole;
		[DataMember] public readonly List<Vector2Int> indexes = new List<Vector2Int>();

		public Land() { }

		public Land(Land land)
		{
			airHole = land.airHole;
			indexes.AddRange(land.indexes);
		}
	}



	public sealed class Piece
	{
		public readonly Color color;
		internal Land land;

		public Piece(Color color) => this.color = color;

		public Piece(Piece piece)
		{
			color = piece.color;
			land = new Land(piece.land);
		}


		public override string ToString() => $"({color}, {land})";
	}



	[DataContract]
	public sealed class Core
	{
		#region Khai báo dữ liệu và khởi tạo
		private static readonly Vector2Int[] DIRECTIONS = new Vector2Int[]
		{
			Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down
		};
		private readonly IReadOnlyDictionary<Color, List<Land>> lands = new Dictionary<Color, List<Land>>
		{
			[Color.White] = new List<Land>(),
			[Color.Black] = new List<Land>()
		};
		private Piece[][] mailBox;
		public Rect rect { get; private set; }


		private static Core instance;
		public Core(Vector2Int size, Action<Vector3Int, Color> drawPieceGUI, Action<Vector3Int> clearPieceGUI)
		{
			if (size.x < 2 || size.y < 2) throw new ArgumentOutOfRangeException($"Size phải >= (2, 2). size= {size}");
			if (size.x * size.y > 10_000) throw new OutOfMemoryException($"Size quá lớn. size= {size}");
			instance = this;
			mailBox = new Piece[size.x][];
			for (int x = 0; x < size.x; ++x) mailBox[x] = new Piece[size.y];
			rect = new Rect(0, 0, size.x - 1, size.y - 1);
			this.drawPieceGUI = drawPieceGUI;
			this.clearPieceGUI = clearPieceGUI;
		}


		public Core(Color?[][] mailBox, Action<Vector3Int, Color> drawPieceGUI, Action<Vector3Int> clearPieceGUI) : this(new Vector2Int(mailBox.Length, mailBox[0].Length), drawPieceGUI, clearPieceGUI)
		{
			var index = new Vector2Int();
			for (index.x = 0; index.x < rect.width; ++index.x)
				for (index.y = 0; index.y < rect.height; ++index.y)
					if (mailBox[index.x][index.y] != null)
						Move(new MoveData(this, mailBox[index.x][index.y].Value, index), History.Mode.Play);
		}


		public Core(Core core, Action<Vector3Int, Color> drawPieceGUI, Action<Vector3Int> clearPieceGUI) : this(new Vector2Int(core.rect.width, core.rect.height), drawPieceGUI, clearPieceGUI)
		{
			if (core.state != null) throw new InvalidOperationException("Không thể copy bàn cờ đã kết thúc !");

			var oldLand_newLand = new Dictionary<Land, Land>();
			foreach (var color_list in core.lands)
			{
				var list = lands[color_list.Key];
				foreach (var land in color_list.Value)
					list.Add(oldLand_newLand[land] = new Land(land));
			}

			for (int x = 0; x < rect.width; ++x)
				for (int y = 0; y < rect.height; ++y)
				{
					var piece = core.mailBox[x][y];
					mailBox[x][y] = new Piece(piece.color) { land = oldLand_newLand[piece.land] };
				}
		}


		public Piece this[int x, int y] => mailBox[x][y];
		public Piece this[Vector2Int index] => mailBox[index.x][index.y];
		#endregion


		#region State
		private readonly Dictionary<Color, int> pieceCounts = new Dictionary<Color, int>
		{
			[Color.White] = -1,
			[Color.Black] = -1
		};


		public int PieceCount(Color color)
		{
			if (pieceCounts[0] >= 0) return pieceCounts[color];

			int c = 0;
			var list = lands[color];
			for (int i = 0; i < list.Count; ++i) c += list[i].indexes.Count;
			return pieceCounts[color] = c;
		}


		public enum State
		{
			White_Win, Black_Win, Draw
		}
		public State? state { get; private set; }
		public event Action<State> onFinished;


		/// <summary>
		/// Kết thúc ván chơi và quyết định kết quả.
		/// <para>Chú ý: Không thể Undo Finish !</para>
		/// </summary>
		public State Finish()
		{
			int w = PieceCount(Color.White), b = PieceCount(Color.Black);
			state = w > b ? State.White_Win : b > w ? State.Black_Win : State.Draw;
			onFinished?.Invoke(state.Value);
			return state.Value;
		}
		#endregion


		#region DEBUG
		public override string ToString()
		{
			string s = "";
			for (int y = rect.yMax; y >= 0; --y)
			{
				s += $"{y}    ";
				for (int x = 0; x < mailBox.Length; ++x)
					s += mailBox[x][y] == null ? "  *  " : mailBox[x][y].color == Color.White ? "  W  " : "  B  ";
				s += "\n\n";
			}

			s += "\n     ";
			for (int x = 0; x <= rect.xMax; ++x) s += $"  {x}  ";
			return s;
		}


		public void Print()
		{
			for (int y = rect.yMax; y >= 0; --y)
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write($"{y}    ");
				for (int x = 0; x < mailBox.Length; ++x)
				{
					Console.ForegroundColor = mailBox[x][y] == null ? ConsoleColor.DarkYellow : mailBox[x][y].color == Color.White ? ConsoleColor.Red : ConsoleColor.Green;
					Console.Write(mailBox[x][y] == null ? "  *  " : mailBox[x][y].color == Color.White ? "  O  " : "  X  ");
				}
				Console.WriteLine("\n");
			}

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("\n     ");
			for (int x = 0; x <= rect.xMax; ++x) Console.Write($"  {x}  ");
			Console.WriteLine();
		}
		#endregion


		#region CanMove
		/// <summary>
		/// <c>[<see cref="Color"/>] = { [<see cref="Land"/>] = point}</c>
		/// </summary>
		private static readonly IReadOnlyDictionary<Color, Dictionary<Land, int>> tmp = new Dictionary<Color, Dictionary<Land, int>>
		{
			[Color.White] = new Dictionary<Land, int>(),
			[Color.Black] = new Dictionary<Land, int>()
		};


		public bool CanMove(in Color color, in Vector2Int index)
		{
			if (mailBox[index.x][index.y] != null) return false;

			tmp[Color.White].Clear();
			tmp[Color.Black].Clear();
			for (int d = 0, x, y; d < 4; ++d)
			{
				var direction = DIRECTIONS[d];
				x = index.x + direction.x; y = index.y + direction.y;
				if (!rect.Contains(x, y)) goto CONTINUE_LOOP_DIRECTIONS;
				var piece = mailBox[x][y];
				if (piece == null) return true;

				if (!tmp[piece.color].ContainsKey(piece.land)) tmp[piece.color][piece.land] = 1;
				else ++tmp[piece.color][piece.land];
				CONTINUE_LOOP_DIRECTIONS:;
			}

			for (int c = 0; c < 2; ++c)
				foreach (var land_point in tmp[(Color)c])
					if ((c == (int)color && land_point.Key.airHole > land_point.Value) || (c != (int)color && land_point.Key.airHole == land_point.Value)) return true;

			return false;
		}
		#endregion


		#region Move
		public sealed class MoveData : IMoveData
		{
			public int playerID { get; }
			public readonly Vector2Int index;
			internal readonly int emptyHole;
			internal readonly List<Land> deadEnemies;

			/// <summary>
			/// <c>[<see cref="Land"/>] == airHole của <see cref="Land"/></c>
			/// </summary>
			internal readonly Dictionary<Land, int> enemies, allies;

			/// <summary>
			/// <c>[<see cref="Color"/>] == { [<see cref="Land"/>] == point}</c>
			/// </summary>
			private static readonly IReadOnlyDictionary<Color, Dictionary<Land, int>> tmp = new Dictionary<Color, Dictionary<Land, int>>
			{
				[Color.White] = new Dictionary<Land, int>(),
				[Color.Black] = new Dictionary<Land, int>()
			};


			internal MoveData(Core core, in Color color, in Vector2Int index)
			{
				this.index = index;
				playerID = (int)color;
				deadEnemies = new List<Land>();
				enemies = new Dictionary<Land, int>();
				allies = new Dictionary<Land, int>();
				tmp[Color.White].Clear();
				tmp[Color.Black].Clear();
				emptyHole = 0;

				#region Tìm lổ trống và tất cả land.
				for (int d = 0; d < 4; ++d)
				{
					var direction = DIRECTIONS[d];
					int x = index.x + direction.x, y = index.y + direction.y;
					if (!core.rect.Contains(x, y)) continue;
					var piece = core.mailBox[x][y];
					if (piece == null) { ++emptyHole; continue; }

					if (!tmp[piece.color].ContainsKey(piece.land)) tmp[piece.color][piece.land] = 1;
					else ++tmp[piece.color][piece.land];
				}
				#endregion

				#region Xác định land địch sắp chết, land địch còn sống và land mình.
				for (int c = 0; c < 2; ++c)
					foreach (var land_point in tmp[(Color)c])
						if (c != (int)color && land_point.Key.airHole == land_point.Value) deadEnemies.Add(land_point.Key);
						else if (c != (int)color) enemies[land_point.Key] = land_point.Value;
						else allies[land_point.Key] = land_point.Value;
				#endregion

				tmp[Color.White].Clear();
				tmp[Color.Black].Clear();
			}


			#region Json notsupported
			[OnSerializing]
			private void _(StreamingContext _) => throw new NotSupportedException();

			[OnDeserializing]
			private void __(StreamingContext _) => throw new NotSupportedException();
			#endregion
		}


		static Core()
		{
			object @lock = new object();
			PhotonPeer.RegisterType(typeof(MoveData), Util.NextCustomTypeCode(),
				obj =>
				{
					lock (@lock)
					{
						var data = obj as MoveData;
						using var stream = new MemoryStream();
						using var writer = new BinaryWriter(stream);
						writer.Write(data.playerID);
						writer.Write(data.index.x);
						writer.Write(data.index.y);
						writer.Flush();
						return stream.ToArray();
					}
				},
				array =>
				{
					lock (@lock)
					{
						using var stream = new MemoryStream(array);
						using var reader = new BinaryReader(stream);
						return new MoveData(instance, (Color)reader.ReadInt32(), new Vector2Int(reader.ReadInt32(), reader.ReadInt32()));
					}
				});
		}



		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public MoveData GenerateMoveData(in Color color, in Vector2Int index) => new MoveData(this, color, index);


		private Action<Vector3Int, Color> drawPieceGUI;
		private Action<Vector3Int> clearPieceGUI;
		public void Move(MoveData data, History.Mode mode)
		{
			pieceCounts[Color.White] = pieceCounts[Color.Black] = -1;
			var enemyColor = data.playerID == (int)Color.White ? Color.Black : Color.White;
			if (mode != History.Mode.Undo)
			{
				#region DO
				#region Liên kết các land mình hiện tại tạo land mới cho cờ ở {data.index}
				var piece = mailBox[data.index.x][data.index.y] = new Piece((Color)data.playerID);
				piece.land = new Land() { airHole = data.emptyHole };
				piece.land.indexes.Add(data.index);
				lands[(Color)data.playerID].Add(piece.land);
				foreach (var land_point in data.allies)
				{
					piece.land.airHole += (land_point.Key.airHole - land_point.Value);
					piece.land.indexes.AddRange(land_point.Key.indexes);
					var indexes = land_point.Key.indexes;
					for (int i = 0; i < indexes.Count; ++i)
					{
						var index = indexes[i];
						mailBox[index.x][index.y].land = piece.land;
					}
					lands[(Color)data.playerID].Remove(land_point.Key);
				}
				#endregion

				#region Trừ lổ thở land địch
				foreach (var enemy_point in data.enemies) enemy_point.Key.airHole -= enemy_point.Value;
				for (int e = data.deadEnemies.Count - 1; e >= 0; --e)
				{
					var enemy = data.deadEnemies[e];

					// Giết land địch
					lands[enemyColor].Remove(enemy);

					for (int i = enemy.indexes.Count - 1; i >= 0; --i)
					{
						var index = enemy.indexes[i];
						mailBox[index.x][index.y] = null;
						clearPieceGUI(index.ToVector3Int());
						for (int d = 0, __x, __y; d < 4; ++d)
						{
							__x = index.x + DIRECTIONS[d].x; __y = index.y + DIRECTIONS[d].y;
							if (!rect.Contains(__x, __y)) continue;
							var land = mailBox[__x][__y]?.land;
							if (land != null && land != enemy) ++land.airHole;
						}
					}
				}
				#endregion

				drawPieceGUI(data.index.ToVector3Int(), (Color)data.playerID);
				#endregion
			}
			else
			{
				#region UNDO
				mailBox[data.index.x][data.index.y] = null;

				// Khôi phục con trỏ land mình
				foreach (var land in data.allies.Keys)
					foreach (var index in land.indexes) mailBox[index.x][index.y].land = land;

				// Khôi phục lổ thở land địch còn sống
				foreach (var enemy_point in data.enemies) enemy_point.Key.airHole += enemy_point.Value;

				// Khôi phục land địch bị giết
				for (int e = data.deadEnemies.Count - 1; e >= 0; --e)
				{
					var enemy = data.deadEnemies[e];
					lands[enemyColor].Add(enemy);
					for (int i = enemy.indexes.Count - 1; i >= 0; --i)
					{
						var index = enemy.indexes[i];
						(mailBox[index.x][index.y] = new Piece(enemyColor)).land = enemy;
						drawPieceGUI(index.ToVector3Int(), enemyColor);
					}
				}

				clearPieceGUI(data.index.ToVector3Int());
				#endregion
			}
		}
		#endregion


		#region Json
		[DataContract]
		private sealed class ΔJsonTemp
		{
			[DataMember] private readonly Dictionary<int, Land> id_land = new Dictionary<int, Land>();
			[DataMember] private (Color color, int id)?[][] mailBox;

			[JsonConstructor]
			private ΔJsonTemp() { }


			private static readonly Dictionary<Land, int> land_id = new Dictionary<Land, int>();
			public ΔJsonTemp(Core core)
			{
				if (core.state != null) throw new InvalidOperationException("Bàn cờ đã kết thúc, không thể lưu json !");
				land_id.Clear();

				#region Khởi tạo {id_land}, {land_id}
				int id = 0;
				foreach (var color_list in core.lands)
					foreach (var land in color_list.Value)
					{
						land_id[id_land[id] = land] = id;
						++id;
					}
				#endregion

				#region Khởi tạo {mailBox}
				mailBox = new (Color color, int id)?[core.rect.width][];
				for (int x = 0; x < core.rect.width; ++x)
				{
					mailBox[x] = new (Color color, int id)?[core.rect.height];
					for (int y = 0; y < core.rect.height; ++y)
					{
						var piece = core.mailBox[x][y];
						mailBox[x][y] = piece != null ? (piece.color, land_id[piece.land]) : ((Color color, int id)?)null;
					}
				}
				#endregion

				land_id.Clear();
			}


			public void Deserialize(Core core)
			{
				core.rect = new Rect(0, 0, mailBox.Length - 1, mailBox[0].Length - 1);
				core.mailBox = new Piece[core.rect.width][];
				for (int x = 0; x < core.rect.width; ++x)
				{
					core.mailBox[x] = new Piece[core.rect.height];
					for (int y = 0; y < core.rect.height; ++y)
					{
						var p = mailBox[x][y];
						var piece = core.mailBox[x][y] = p != null ? new Piece(p.Value.color) { land = id_land[p.Value.id] } : null;
						if (piece != null) core.lands[piece.color].Add(piece.land);
					}
				}

				core.Δtmp = null;
			}
		}
		[DataMember] private ΔJsonTemp Δtmp;


		[JsonConstructor]
		private Core() { }


		[OnSerializing]
		private void OnSerializing(StreamingContext _) => Δtmp = new ΔJsonTemp(this);


		[OnDeserialized]
		private void OnDeserialized(StreamingContext _) => Δtmp.Deserialize(this);
		#endregion
	}



	public static class Extensions
	{
		/// <summary>
		/// Lấy màu ngược với màu nhập vào.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color Opponent(this Color color) => (Color)(1 - (int)color);
	}



	/// <summary>
	/// Định nghĩa cách lưu json cho <see cref="History"/> vì <see cref="Core.MoveData"/> không thể lưu json 
	/// </summary>
	public sealed class HistoryJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType) => objectType == typeof(History);


		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}


		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
}