using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif


namespace BoardGames.Utils
{
	/// <summary>
	/// Bắt event từ <see cref="GraphicRaycaster"/>, không làm gì liên quan UI<br/>
	/// Obj ở dưới trong cây Hiearchy sẽ đè target lên obj ở trên (không phân biệt parent-child)<br/>
	/// Position Z không ảnh hưởng đến ray cast
	/// </summary>
	[RequireComponent(typeof(CanvasRenderer))]
	public sealed class GraphicRaycastTarget : Graphic
	{
		public override void SetMaterialDirty() { }
		public override void SetVerticesDirty() { }
		protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();



#if UNITY_EDITOR
		/// <summary>
		/// Ẩn hết field, chỉ hiện tùy chọn raycast
		/// </summary>
		[CanEditMultipleObjects, CustomEditor(typeof(GraphicRaycastTarget), false)]
		private sealed class Editor : GraphicEditor
		{
			public override void OnInspectorGUI()
			{
				serializedObject.Update();
				EditorGUILayout.PropertyField(m_Script, new GUILayoutOption[0]);
				RaycastControlsGUI();
				serializedObject.ApplyModifiedProperties();
			}
		}
#endif
	}
}