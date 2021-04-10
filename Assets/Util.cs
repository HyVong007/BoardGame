using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace BoardGames
{
	public static class Util
	{
		public static bool Contains<T>(this T[] array, T item)
		{
			for (int i = 0; i < array.Length; ++i) if (array[i].Equals(item)) return true;
			return false;
		}


		public static bool Contains(this (int x, int y)[] array, (int x, int y) item)
		{
			for (int i = 0; i < array.Length; ++i) if (array[i] == item) return true;
			return false;
		}


		#region Global Dict
		private static readonly Dictionary<string, object> dict = new Dictionary<string, object>();

		public static bool TryGetValue<TValue>(this string key, out TValue value)
		{
			bool result = dict.TryGetValue(key, out object v);
			value = (TValue)v;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TValue GetValue<TValue>(this string key) => (TValue)dict[key];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ContainsKey(this string key) => dict.ContainsKey(key);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Remove(this string key) => dict.Remove(key);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Write(this string key, object value) => dict[key] = value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ClearAllKeys() => dict.Clear();
		#endregion


		public static ReadOnlyArray<ReadOnlyArray<T>> ToReadOnly<T>(this T[][] array)
		{
			var rows = new ReadOnlyArray<T>[array.Length];
			for (int x = 0; x < array.Length; ++x) rows[x] = new ReadOnlyArray<T>(array[x]);
			return new ReadOnlyArray<ReadOnlyArray<T>>(rows);
		}


		public static IEnumerator<I> EnumValueGenerator<I>() where I : struct, Enum
		{
			var values = (I[])Enum.GetValues(typeof(I));
			while (true) foreach (var value in values) yield return value;
		}


		#region Converts
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3Int ToVector3Int(this in Vector2Int value) => new Vector3Int(value.x, value.y, 0);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2Int ToVector2Int(this in Vector3Int value) => new Vector2Int(value.x, value.y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 ToVector2(this in Vector3Int value) => new Vector2(value.x, value.y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 ToVector3(this in Vector2Int value) => new Vector3(value.x, value.y);

#if !DEBUG
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public static Vector3Int ToVector3Int(this in Vector3 value) =>
#if DEBUG
				value.x < 0 || value.y < 0 || value.z < 0 ? throw new IndexOutOfRangeException($"value= {value} phải là tọa độ không âm !") :
#endif
			new Vector3Int((int)value.x, (int)value.y, (int)value.z);
		#endregion


		#region UniTask
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool isRunning(this in UniTask task) => task.Status == UniTaskStatus.Pending;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool isRunning<T>(this in UniTask<T> task) => task.Status == UniTaskStatus.Pending;


		/// <summary>
		/// Bắt <see cref="Exception"/> ngoại trừ <see cref="OperationCanceledException"/><br/>
		/// Không <see langword="await"/> <paramref name="task"/>, đảm bảo luôn kiểm tra được status của <paramref name="task"/>
		/// </summary>
		public static void Forget(this in UniTask task)
		{
			tasks.Add(task);
			if (tasks.Count == 1) Forget();
		}


		private static readonly List<UniTask> tasks = new List<UniTask>(), tmp = new List<UniTask>();
		private static async void Forget()
		{
			while (true)
			{
				tmp.Clear();
				foreach (var task in tasks)
					if (!task.isRunning())
						if (task.Status == UniTaskStatus.Faulted) await task;
						else tmp.Add(task);

				foreach (var task in tmp) tasks.Remove(task);
				if (tasks.Count == 0) break;
				await UniTask.Yield();
			}
		}


		/// <summary>
		/// Bắt <see cref="Exception"/> ngoại trừ <see cref="OperationCanceledException"/><br/>
		/// Không <see langword="await"/> <paramref name="task"/>, đảm bảo luôn kiểm tra được status của <paramref name="task"/>
		/// </summary>
		public static void Forget<T>(this in UniTask<T> task)
		{
			GenericTasks<T>.tasks.Add(task);
			if (GenericTasks<T>.tasks.Count == 1) GenericTasks<T>.Forget();
		}


		private static class GenericTasks<T>
		{
			public static readonly List<UniTask<T>> tasks = new List<UniTask<T>>();
			private static readonly List<UniTask<T>> tmp = new List<UniTask<T>>();


			public static async void Forget()
			{
				while (true)
				{
					tmp.Clear();
					foreach (var task in tasks)
						if (!task.isRunning())
							if (task.Status == UniTaskStatus.Faulted) await task;
							else tmp.Add(task);

					foreach (var task in tmp) tasks.Remove(task);
					if (tasks.Count == 0) break;
					await UniTask.Yield();
				}
			}
		}
		#endregion


		#region Move
		/// <summary>
		/// Có <see cref="Transform"/> đang di chuyển ?
		/// </summary>
		public static bool hasMovingTransform => moveCount != 0;
		private static int moveCount;

		/// <summary>
		/// <para>Chú ý: KHÔNG đảm bảo luôn luôn <see langword="async"/> !</para>
		/// Nếu trước khi gọi mà <c>dest == transform.position</c> hoặc <c>token.IsCancellationRequested == <see langword="true"/></c> thì sẽ thoát ngay lập tức !
		/// </summary>
		public static async UniTask Move(this Transform transform, Vector3 dest, float speed, CancellationToken token = default)
		{
			if (token.IsCancellationRequested) return;
#if DEBUG
			if (transform.position.z != 0 || dest.z != 0) throw new Exception("Position.Z phải = 0 !");
#endif
			++moveCount;
			while (!token.IsCancellationRequested && transform.position != dest)
			{
				transform.position = Vector3.MoveTowards(transform.position, dest, speed);
				await UniTask.Yield();
#if UNITY_EDITOR
				try
				{
					// Kiểm tra xem có bị Destroy chưa ?
					var _ = transform.gameObject;
				}
				catch { --moveCount; return; }
#endif
			}

			if (!token.IsCancellationRequested) transform.position = dest;
			--moveCount;
		}
		#endregion


		private static readonly Vector3[] corners = new Vector3[4];
		/// <summary>
		/// Chú ý kết quả trả về là ref dẫn đến <see cref="corners"/><br/>
		/// Nên cache kết quả ngay vì nội dung của ref sẽ thay đổi sau mỗi lần gọi hàm.<para/>
		/// Trước khi gọi hàm cần đảm bảo <see cref="Canvas"/> đã update bằng cách đợi callback Start hoặc gọi <see cref="Canvas.ForceUpdateCanvases"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3[] GetWorldCorners(this RectTransform rt)
		{
			rt.GetWorldCorners(corners);
			return corners;
		}


		#region Load Scene
		/// <summary>
		/// <paramref name="relativePath"/> ví dụ: "A/B/C. Không cần "Assets" và ".unity"
		/// </summary>
		public static async UniTask LoadScene(this string relativePath, bool additive = false)
		{
			await SceneManager.LoadSceneAsync(relativePath, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
			await UniTask.Yield();
			SceneManager.SetActiveScene(SceneManager.GetSceneByPath($"Assets/{relativePath}.unity"));
		}


		/// <summary>
		/// <paramref name="relativePath"/> ví dụ: "A/B/C. Không cần "Assets" và ".unity"
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async UniTask UnloadScene(this string relativePath)
			=> await SceneManager.UnloadSceneAsync(relativePath);


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async UniTask ReloadScene(this string relativePath, bool additive = false)
		{
			await UnloadScene(relativePath);
			await LoadScene(relativePath, additive);
		}

		public static async UniTask LoadScene(this int sceneBuildIndex, bool additive = false)
		{
			await SceneManager.LoadSceneAsync(sceneBuildIndex, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
			await UniTask.Yield();
			SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(sceneBuildIndex));
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async UniTask UnloadScene(this int sceneBuildIndex)
			=> await SceneManager.UnloadSceneAsync(sceneBuildIndex);


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async UniTask ReloadScene(this int sceneBuildIndex, bool additive = false)
		{
			await UnloadScene(sceneBuildIndex);
			await LoadScene(sceneBuildIndex, additive);
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async UniTask ReloadActiveScene(bool additive = false)
			=> await ReloadScene(SceneManager.GetActiveScene().buildIndex, additive);
		#endregion


		#region Instantiate Addressables
		/// <summary>
		/// <paramref name="relativePath"/>: Ví dụ A/B/C (không cần Assets và .prefab)
		/// </summary>
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public static async UniTask<T> Instantiate<T>(this string relativePath)
		//	=> (await Addressables.InstantiateAsync($"Assets/{relativePath}.prefab")).GetComponent<T>();

		///// <summary>
		///// <paramref name="relativePath"/>: Ví dụ A/B/C (không cần Assets và .prefab)
		///// </summary>
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public static async UniTask<T> Instantiate<T>(this string relativePath, Vector3 position, Quaternion rotation)
		//	=> (await Addressables.InstantiateAsync($"Assets/{relativePath}.prefab", position, rotation)).GetComponent<T>();

		///// <summary>
		///// <paramref name="relativePath"/>: Ví dụ A/B/C (không cần Assets và .prefab)
		///// </summary>
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public static async UniTask<T> Instantiate<T>(this string relativePath, Transform parent)
		//	=> (await Addressables.InstantiateAsync($"Assets/{relativePath}.prefab", parent)).GetComponent<T>();
		#endregion


		public static bool CurrentPlayerIsLocalHuman(this TurnManager t)
			//=> t is OfflineTurnManager ? (t as OfflineTurnManager).IsHumanPlayer(t.currentPlayerID)
			//	: GamePlayer.Find(t.currentPlayerID).IsLocal();
			=> throw new NotImplementedException();


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToJson(this object obj) => JsonConvert.SerializeObject(obj);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T FromJson<T>(this string json) => JsonConvert.DeserializeObject<T>(json);
	}



	[Serializable]
	[DataContract]
	public struct Rect : ISerializationCallbackReceiver
	{
		#region Data
		[SerializeField]
		[DataMember]
		private int _xMin, _xMax, _yMin, _yMax;
		public int xMin => _xMin;
		public int xMax => _xMax;
		public int yMin => _yMin;
		public int yMax => _yMax;
		public int width { get; private set; }
		public int height { get; private set; }


		[OnDeserialized]
		private void OnDeserialized(StreamingContext context) => OnAfterDeserialize();

		public void OnBeforeSerialize() { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void OnAfterDeserialize()
		{
			width = xMax - xMin + 1;
			height = yMax - yMin + 1;
		}
		#endregion


		public Rect(int xMin, int yMin, int xMax, int yMax)
		{
			_xMin = xMin; _yMin = yMin; _xMax = xMax; _yMax = yMax;
			width = xMax - xMin + 1;
			height = yMax - yMin + 1;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(Vector2Int index) => xMin <= index.x && index.x <= xMax && yMin <= index.y && index.y <= yMax;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(int x, int y) => xMin <= x && x <= xMax && yMin <= y && y <= yMax;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains((int x, int y) index) => Contains(index.x, index.y);


		public override string ToString() => $"(xMin= {xMin}, yMin= {yMin}, xMax= {xMax}, yMax= {yMax})";


		public override bool Equals(object obj)
		{
			if (!(obj is Rect)) return false;

			var rect = (Rect)obj;
			return rect.xMin == xMin && rect.yMin == yMin && rect.xMax == xMax && rect.yMax == yMax;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => (xMin, yMin, xMax, yMax).GetHashCode();


		public static bool operator ==(Rect left, Rect right) =>
			left.xMin == right.xMin && left.yMin == right.yMin
			&& left.xMax == right.xMax && left.yMax == right.yMax;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(Rect left, Rect right) => !(left == right);
	}



	public readonly struct ReadOnlyArray<T> : IEnumerable<T>
	{
		private readonly T[] array;

		public ReadOnlyArray(T[] array) => this.array = array;

		public T this[int index] => array[index];

		public int Length => array.Length;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerator<T> GetEnumerator() => (array as IEnumerable<T>).GetEnumerator();


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator IEnumerable.GetEnumerator() => array.GetEnumerator();


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object obj) => array.Equals(obj);


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => array.GetHashCode();


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(ReadOnlyArray<T> left, ReadOnlyArray<T> right) => left.Equals(right);


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(ReadOnlyArray<T> left, ReadOnlyArray<T> right) => !(left == right);
	}



	[Serializable]
	public sealed class ObjectPool<T> : IEnumerable<T> where T : Component
	{
		[SerializeField] private T prefab;
		[SerializeField] private Transform usingAnchor, freeAnchor;
		[SerializeField] private List<T> free = new List<T>();
		private readonly List<T> @using = new List<T>();


		private ObjectPool() { }


		public ObjectPool(T prefab, Transform freeAnchor = null, Transform usingAnchor = null)
		{
			this.prefab = prefab;
			this.freeAnchor = freeAnchor;
			this.usingAnchor = usingAnchor;
		}


		/// <param name="active">Active gameObject ngay lập tức ?</param>
		public T Get(Vector3 position = default, bool active = true)
		{
			T item;
			if (free.Count != 0)
			{
				item = free[0];
				free.RemoveAt(0);
			}
			else item = UnityEngine.Object.Instantiate(prefab);

			item.transform.parent = usingAnchor;
			@using.Add(item);
			item.transform.position = position;
			item.gameObject.SetActive(active);
			return item;
		}


		public void Recycle(T item)
		{
			item.gameObject.SetActive(false);
			item.transform.parent = freeAnchor;
			@using.Remove(item);
			free.Add(item);
		}


		public void Recycle()
		{
			for (int i = 0; i < @using.Count; ++i)
			{
				var item = @using[i];
				item.gameObject.SetActive(false);
				item.transform.parent = freeAnchor;
				free.Add(item);
			}
			@using.Clear();
		}


		public void DestroyGameObject(T item)
		{
			@using.Remove(item);
			UnityEngine.Object.Destroy(item.gameObject);
		}


		public void DestroyGameObject()
		{
			foreach (var item in @using) UnityEngine.Object.Destroy(item.gameObject);
			@using.Clear();
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator IEnumerable.GetEnumerator() => @using.GetEnumerator();


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerator<T> GetEnumerator() => (@using as IEnumerable<T>).GetEnumerator();
	}
}