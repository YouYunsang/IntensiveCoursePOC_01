using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Redirects the current slide to the random cardinal direction assigned when this deck item is placed for a turn.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Cell Effects/Direction Change", fileName = "CellEffect_DirectionChange")]
    public sealed class DirectionChangeCellEffectSO : CellEffectDefinitionSO, ICellMovementDirectionEffect
    {
        /// <summary>This effect always receives a random cardinal direction for each player-turn placement.</summary>
        public override bool RequiresRandomDirection => true;

        /// <summary>Uses the placement-assigned cardinal direction as the continuation direction.</summary>
        public Vector2Int ResolveNextDirection(Vector2Int incomingDirection, Vector2Int assignedDirection)
        {
            return assignedDirection;
        }
    }
}
