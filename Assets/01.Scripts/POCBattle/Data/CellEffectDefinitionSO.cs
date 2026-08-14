using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Base definition for a non-block effect that occupies a board cell and is randomly re-placed every player turn.
    /// </summary>
    public abstract class CellEffectDefinitionSO : DeckItemDefinitionSO
    {
        [SerializeField, Tooltip("Human-readable name used by loot and deck-management presentation.")]
        private string _displayName = "Cell Effect";

        [SerializeField, Tooltip("POC tint used by the board sprite and loot glow.")]
        private Color _displayColor = Color.white;

        [SerializeField, Tooltip("World-space sprite rendered above the affected board cell.")]
        private Sprite _displaySprite;

        [SerializeField, Tooltip("Per-effect world-space offset added after the shared board surface offset. Use Y to lift sprites above nearby blocks.")]
        private Vector3 _boardVisualOffset = Vector3.zero;

        /// <summary>Gets the cell effect display name.</summary>
        public override string DisplayName => _displayName;

        /// <summary>Gets the cell effect POC tint.</summary>
        public override Color DisplayColor => _displayColor;

        /// <summary>Cell effects created for the player deck are collectible.</summary>
        public override bool IsPlayerCollectible => true;

        /// <summary>Gets the sprite displayed on an affected board cell.</summary>
        public Sprite DisplaySprite => _displaySprite;

        /// <summary>Gets the per-effect board visual offset.</summary>
        public Vector3 BoardVisualOffset => _boardVisualOffset;

        /// <summary>Gets whether this effect requires a random cardinal direction when placed.</summary>
        public virtual bool RequiresRandomDirection => false;
    }

    /// <summary>
    /// Implemented by cell effects that redirect a continuing slide after the player enters their cell.
    /// </summary>
    public interface ICellMovementDirectionEffect
    {
        /// <summary>Returns the next cardinal movement direction after this cell effect triggers.</summary>
        Vector2Int ResolveNextDirection(Vector2Int incomingDirection, Vector2Int assignedDirection);
    }
}
