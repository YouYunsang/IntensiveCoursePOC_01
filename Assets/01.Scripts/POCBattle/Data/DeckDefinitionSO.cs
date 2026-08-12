using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Stores one player deck item type and how many physical copies exist in the starting deck.
    /// The serialized field name remains _block so existing POC deck assets keep their references after this generalization.
    /// </summary>
    [Serializable]
    public sealed class DeckBlockEntry
    {
        [SerializeField, Tooltip("Deck item definition used by this entry. Blocks and cell effects are both supported.")]
        private DeckItemDefinitionSO _block;

        [SerializeField, Tooltip("Number of physical copies included in the deck.")]
        private int _count = 1;

        /// <summary>Gets the generalized deck item definition.</summary>
        public DeckItemDefinitionSO Item => _block;

        /// <summary>Compatibility accessor for older editor utilities that only understand block entries.</summary>
        public BlockDefinitionSO Block => _block as BlockDefinitionSO;

        /// <summary>Gets the number of physical copies.</summary>
        public int Count => _count;
    }

    /// <summary>
    /// Defines the immutable starting deck. Every physical item is copied into PlayerDeckModel when a run begins.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Decks/Deck Definition", fileName = "DeckDefinition")]
    public sealed class DeckDefinitionSO : ScriptableObject
    {
        [SerializeField, Tooltip("All player deck item types and physical copy counts.")]
        private DeckBlockEntry[] _entries = Array.Empty<DeckBlockEntry>();

        /// <summary>Gets configured deck entries.</summary>
        public IReadOnlyList<DeckBlockEntry> Entries => _entries;

        /// <summary>Calculates the number of all physical deck items, including cell effects.</summary>
        public int GetTotalItemCount()
        {
            int totalCount = 0;
            for (int index = 0; index < _entries.Length; index++)
            {
                DeckBlockEntry entry = _entries[index];
                if (entry != null && entry.Item != null)
                {
                    totalCount += Mathf.Max(0, entry.Count);
                }
            }

            return totalCount;
        }

        /// <summary>Compatibility helper retained for previous POC validators; now returns total deck item count.</summary>
        public int GetTotalBlockCount()
        {
            return GetTotalItemCount();
        }
    }
}
