using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Stores one block type and how many physical copies exist in the player's deck.
    /// </summary>
    [Serializable]
    public sealed class DeckBlockEntry
    {
        [SerializeField, Tooltip("Block definition used by this deck entry.")]
        private BlockDefinitionSO _block;

        [SerializeField, Tooltip("Number of physical copies included in the deck.")]
        private int _count = 1;

        /// <summary>Gets the block definition.</summary>
        public BlockDefinitionSO Block => _block;

        /// <summary>Gets the number of physical copies.</summary>
        public int Count => _count;
    }

    /// <summary>
    /// Defines a deck whose complete block list is placed on the board every player turn.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Decks/Deck Definition", fileName = "DeckDefinition")]
    public sealed class DeckDefinitionSO : ScriptableObject
    {
        [SerializeField, Tooltip("All player deck block types and physical copy counts.")]
        private DeckBlockEntry[] _entries = Array.Empty<DeckBlockEntry>();

        /// <summary>Gets configured deck entries.</summary>
        public IReadOnlyList<DeckBlockEntry> Entries => _entries;

        /// <summary>
        /// Calculates the number of physical blocks in this deck.
        /// </summary>
        public int GetTotalBlockCount()
        {
            int totalCount = 0;

            for (int index = 0; index < _entries.Length; index++)
            {
                DeckBlockEntry entry = _entries[index];
                if (entry != null && entry.Block != null)
                {
                    totalCount += Mathf.Max(0, entry.Count);
                }
            }

            return totalCount;
        }
    }
}
