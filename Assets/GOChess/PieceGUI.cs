using UnityEngine;


namespace BoardGames.GOChess
{
	[RequireComponent(typeof(SpriteRenderer))]
	public sealed class PieceGUI : MonoBehaviour
	{
		[field: SerializeField] public Color color { get; private set; }
	}
}