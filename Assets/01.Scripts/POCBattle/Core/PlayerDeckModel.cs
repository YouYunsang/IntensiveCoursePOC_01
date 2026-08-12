using System;
using System.Collections.Generic;
using PocBattle.Data;

namespace PocBattle.Core
{
    /// <summary>
    /// One stable player-owned deck item instance. Runtime instance ids allow duplicate definitions to be discarded independently.
    /// </summary>
    public sealed class DeckItemInstance
    {
        /// <summary>Stable id unique within one run.</summary>
        private readonly int _instanceId;

        /// <summary>Immutable item definition shared by every copy of this item type.</summary>
        private readonly DeckItemDefinitionSO _definition;

        /// <summary>Gets stable runtime deck instance id.</summary>
        public int InstanceId => _instanceId;

        /// <summary>Gets immutable deck item definition.</summary>
        public DeckItemDefinitionSO Definition => _definition;

        /// <summary>Creates one runtime deck item instance.</summary>
        public DeckItemInstance(int instanceId, DeckItemDefinitionSO definition)
        {
            _instanceId = instanceId;
            _definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
        }
    }

    /// <summary>
    /// Mutable runtime deck for one run. Blocks and cell effects share the same capacity, loot, and discard rules.
    /// </summary>
    public sealed class PlayerDeckModel
    {
        /// <summary>All currently owned physical deck item instances.</summary>
        private readonly List<DeckItemInstance> _items;

        /// <summary>Maximum number of player-owned deck items.</summary>
        private readonly int _capacity;

        /// <summary>Monotonically increasing id used for newly looted physical item copies.</summary>
        private int _nextInstanceId;

        /// <summary>Gets current physical deck item instances.</summary>
        public IReadOnlyList<DeckItemInstance> Items => _items;

        /// <summary>Gets current number of owned deck items.</summary>
        public int Count => _items.Count;

        /// <summary>Gets maximum number of owned deck items.</summary>
        public int Capacity => _capacity;

        /// <summary>Gets whether a new item can be added without discarding.</summary>
        public bool HasFreeSlot => _items.Count < _capacity;

        /// <summary>Gets how many currently owned items are physical blocks.</summary>
        public int BlockCount
        {
            get
            {
                int count = 0;
                for (int itemIndex = 0; itemIndex < _items.Count; itemIndex++)
                {
                    if (_items[itemIndex].Definition is BlockDefinitionSO)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Gets how many currently owned items are cell effects.</summary>
        public int CellEffectCount => Count - BlockCount;

        /// <summary>
        /// Copies every physical starting item from immutable deck data into runtime instances.
        /// </summary>
        public PlayerDeckModel(DeckDefinitionSO startingDeck, int capacity)
        {
            if (startingDeck == null)
            {
                throw new ArgumentNullException(nameof(startingDeck));
            }

            _capacity = Math.Max(1, capacity);
            _items = new List<DeckItemInstance>(_capacity);
            _nextInstanceId = 0;

            IReadOnlyList<DeckBlockEntry> entries = startingDeck.Entries;
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                DeckBlockEntry entry = entries[entryIndex];
                if (entry == null || entry.Item == null)
                {
                    continue;
                }

                if (!entry.Item.IsPlayerCollectible)
                {
                    throw new InvalidOperationException($"Player starting deck cannot contain non-collectible item '{entry.Item.DisplayName}'.");
                }

                for (int copyIndex = 0; copyIndex < entry.Count; copyIndex++)
                {
                    AddItemInternal(entry.Item);
                }
            }

            if (_items.Count > _capacity)
            {
                throw new InvalidOperationException($"Starting deck contains {_items.Count} items, exceeding player deck capacity {_capacity}.");
            }
        }

        /// <summary>Adds one physical reward item when capacity is available.</summary>
        public bool TryAddItem(DeckItemDefinitionSO definition)
        {
            if (definition == null || !definition.IsPlayerCollectible || !HasFreeSlot)
            {
                return false;
            }

            AddItemInternal(definition);
            return true;
        }

        /// <summary>Compatibility wrapper for previous block-only run code.</summary>
        public bool TryAddBlock(BlockDefinitionSO definition)
        {
            return TryAddItem(definition);
        }

        /// <summary>Removes one exact physical item by stable runtime id.</summary>
        public bool RemoveItem(int instanceId)
        {
            for (int itemIndex = 0; itemIndex < _items.Count; itemIndex++)
            {
                if (_items[itemIndex].InstanceId != instanceId)
                {
                    continue;
                }

                _items.RemoveAt(itemIndex);
                return true;
            }

            return false;
        }

        /// <summary>Compatibility wrapper retained for existing discard request naming.</summary>
        public bool RemoveBlock(int instanceId)
        {
            return RemoveItem(instanceId);
        }

        /// <summary>Returns whether this runtime deck still contains the requested physical item instance.</summary>
        public bool Contains(int instanceId)
        {
            for (int itemIndex = 0; itemIndex < _items.Count; itemIndex++)
            {
                if (_items[itemIndex].InstanceId == instanceId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Adds one physical item without capacity checks. Used only while constructing a validated runtime deck.</summary>
        private void AddItemInternal(DeckItemDefinitionSO definition)
        {
            _items.Add(new DeckItemInstance(_nextInstanceId++, definition));
        }
    }
}
