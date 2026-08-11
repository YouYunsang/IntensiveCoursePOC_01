using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Defines one deck or trap block and the effects it executes on collision.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Blocks/Block Definition", fileName = "BlockDefinition")]
    public sealed class BlockDefinitionSO : ScriptableObject
    {
        [SerializeField, Tooltip("Human-readable name shown by debug and POC presentation.")]
        private string _displayName = "Block";

        [SerializeField, Tooltip("POC display color for this block.")]
        private Color _displayColor = Color.white;

        [SerializeField, Tooltip("Trap blocks are not editable and are added by encounters instead of the player deck.")]
        private bool _isTrap;

        [SerializeField, Tooltip("Effects executed every time the player collides with this block.")]
        private BlockEffectDefinitionSO[] _effects = Array.Empty<BlockEffectDefinitionSO>();

        /// <summary>Gets the block display name.</summary>
        public string DisplayName => _displayName;

        /// <summary>Gets the POC block display color.</summary>
        public Color DisplayColor => _displayColor;

        /// <summary>Gets whether this definition represents an uneditable trap.</summary>
        public bool IsTrap => _isTrap;

        /// <summary>Gets this block's composable effects.</summary>
        public IReadOnlyList<BlockEffectDefinitionSO> Effects => _effects;
    }
}
