using UnityEngine;


namespace BoardGames.Gomoku
{
	[RequireComponent(typeof(SpriteRenderer))]
	public sealed class Piece : MonoBehaviour
	{
		[field: SerializeField] public Symbol symbol { get; private set; }
	}
}