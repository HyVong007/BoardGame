using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;


namespace BoardGames.Utils
{
	public sealed class Popup : MonoBehaviour
	{
		[field: SerializeField] public Text title { get; private set; }
		[field: SerializeField] public Image icon { get; private set; }
		[SerializeField] private Button blocker, buttonX, buttonOK;
		[SerializeField] private RectTransform scrollViewContent;


		private static Popup instance;
		private void Awake()
		{
			if (instance && instance != this) Destroy(instance.gameObject);
			instance = this;
			blocker.click += Cancel;
			buttonX.click += Cancel;
			buttonOK.click += OK;
			if (transform.parent) transform.SetParent(null, false);
			transform.SetAsLastSibling();
		}


		private void OnDisable()
		{
			if (isOK) ok(); else cancel?.Invoke();
		}


		public event Action cancel, ok;
		private void Cancel(Vector2 _) => Destroy(gameObject);


		private bool isOK;
		private void OK(Vector2 _)
		{
			isOK = true;
			Destroy(gameObject);
		}


		private bool _isBusy;
		public bool isBusy
		{
			get => _isBusy;

			set
			{
				if (value == _isBusy) return;
				_isBusy = value;
				buttonOK.interactable = buttonX.interactable = !value;
				if (value) blocker.click -= Cancel; else blocker.click += Cancel;
			}
		}


		[SerializeField] private RectTransform window;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ScaleWindow(in Vector3 scale) => window.localScale = scale;


		/// <summary>
		/// Thêm <paramref name="content"/> vào cuối danh sách.<br/>
		/// <paramref name="content"/> là: Top-Stretch, pivot (0, 1)
		/// </summary>
		public void AddContent(RectTransform content, float spacing = 0)
		{
#if UNITY_EDITOR
			var viewport = (scrollViewContent.parent as RectTransform).rect;
			if (content.rect.width > viewport.width)
				print($"Popup: content width nên <= \"ScrollViewPort\" width để không bị scale. Viewport= {viewport}");
#endif
			var sd = scrollViewContent.sizeDelta;
			var d = content.sizeDelta;
			content.sizeDelta = new Vector2(-sd.x, d.y);
			scrollViewContent.sizeDelta = new Vector2(sd.x, sd.y + d.y + spacing);
			content.SetParent(scrollViewContent, false);
			content.localPosition = new Vector3(0, -sd.y - spacing);
		}


		/// <summary>
		/// Thêm <paramref name="content"/> vào cuối danh sách.<br/>
		/// <paramref name="content"/> là: Top-Stretch, pivot (0, 1)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddContent(Transform content, float spacing = 0) =>
			AddContent(content as RectTransform, spacing);
	}
}