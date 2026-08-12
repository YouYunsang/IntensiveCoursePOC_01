using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Stores player combat values that are copied into mutable runtime progression state at run start.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Settings/Player Base Stats", fileName = "PlayerBaseStats")]
    public sealed class PlayerBaseStatsSO : ScriptableObject
    {
        [SerializeField, Tooltip("Maximum player health at run start.")]
        private int _maxHealth = 20;

        [SerializeField, Tooltip("Base number of slide moves available each player turn.")]
        private int _baseMoveCount = 3;

        [SerializeField, Tooltip("Base number of block placement edits available each player turn.")]
        private int _baseEditCount = 2;

        [SerializeField, Tooltip("Maximum number of player-owned blocks that may exist in the runtime deck.")]
        private int _deckCapacity = 10;

        /// <summary>Gets maximum player health.</summary>
        public int MaxHealth => _maxHealth;

        /// <summary>Gets base moves per player turn.</summary>
        public int BaseMoveCount => _baseMoveCount;

        /// <summary>Gets base edits per player turn.</summary>
        public int BaseEditCount => _baseEditCount;

        /// <summary>Gets maximum number of player-owned deck blocks.</summary>
        public int DeckCapacity => _deckCapacity;

        /// <summary>
        /// Prevents invalid negative runtime defaults.
        /// </summary>
        private void OnValidate()
        {
            _maxHealth = Mathf.Max(1, _maxHealth);
            _baseMoveCount = Mathf.Max(1, _baseMoveCount);
            _baseEditCount = Mathf.Max(0, _baseEditCount);
            _deckCapacity = Mathf.Max(1, _deckCapacity);
        }
    }
}
