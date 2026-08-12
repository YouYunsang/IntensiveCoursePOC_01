using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Common immutable definition for every player-owned deck item, including board blocks and cell effects.
    /// </summary>
    public abstract class DeckItemDefinitionSO : ScriptableObject
    {
        /// <summary>Gets a presentation-ready item name.</summary>
        public abstract string DisplayName { get; }

        /// <summary>Gets the primary POC presentation color used by loot glow and debug views.</summary>
        public abstract Color DisplayColor { get; }

        /// <summary>Gets whether this definition is allowed in the player's mutable run deck.</summary>
        public abstract bool IsPlayerCollectible { get; }
    }
}
