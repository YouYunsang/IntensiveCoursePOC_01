using System;
using System.Collections.Generic;
using PocBattle.Data;

namespace PocBattle.Core
{
    /// <summary>
    /// One stable player-owned block instance. Runtime instance ids allow duplicate block definitions to be discarded independently.
    /// </summary>
    public sealed class DeckBlockInstance
    {
        /// <summary>Stable id unique within one run.</summary>
        private readonly int _instanceId;

        /// <summary>Immutable block definition shared by every copy of this block type.</summary>
        private readonly BlockDefinitionSO _definition;

        /// <summary>Gets stable runtime deck instance id.</summary>
        public int InstanceId => _instanceId;

        /// <summary>Gets immutable block definition.</summary>
        public BlockDefinitionSO Definition => _definition;

        /// <summary>Creates one runtime deck block instance.</summary>
        public DeckBlockInstance(int instanceId, BlockDefinitionSO definition)
        {
            _instanceId = instanceId;
            _definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
        }
    }

    /// <summary>
    /// Mutable runtime deck for one run. Starting ScriptableObject data is copied once and never modified at runtime.
    /// </summary>
    public sealed class PlayerDeckModel
    {
        /// <summary>All currently owned physical block instances.</summary>
        private readonly List<DeckBlockInstance> _blocks;

        /// <summary>Maximum number of player-owned blocks.</summary>
        private readonly int _capacity;

        /// <summary>Monotonically increasing id used for newly looted physical block copies.</summary>
        private int _nextInstanceId;

        /// <summary>Gets current physical block instances.</summary>
        public IReadOnlyList<DeckBlockInstance> Blocks => _blocks;

        /// <summary>Gets current number of owned blocks.</summary>
        public int Count => _blocks.Count;

        /// <summary>Gets maximum number of owned blocks.</summary>
        public int Capacity => _capacity;

        /// <summary>Gets whether a new block can be added without discarding.</summary>
        public bool HasFreeSlot => _blocks.Count < _capacity;

        /// <summary>
        /// Copies every physical starting block from immutable deck data into runtime instances.
        /// </summary>
        public PlayerDeckModel(DeckDefinitionSO startingDeck, int capacity)
        {
            if (startingDeck == null)
            {
                throw new ArgumentNullException(nameof(startingDeck));
            }

            _capacity = Math.Max(1, capacity);
            _blocks = new List<DeckBlockInstance>(_capacity);
            _nextInstanceId = 0;

            IReadOnlyList<DeckBlockEntry> entries = startingDeck.Entries;
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                DeckBlockEntry entry = entries[entryIndex];
                if (entry == null || entry.Block == null)
                {
                    continue;
                }

                if (entry.Block.IsTrap)
                {
                    throw new InvalidOperationException($"Player starting deck cannot contain trap block '{entry.Block.DisplayName}'.");
                }

                for (int copyIndex = 0; copyIndex < entry.Count; copyIndex++)
                {
                    AddBlockInternal(entry.Block);
                }
            }

            if (_blocks.Count > _capacity)
            {
                throw new InvalidOperationException($"Starting deck contains {_blocks.Count} blocks, exceeding player deck capacity {_capacity}.");
            }
        }

        /// <summary>
        /// Adds one physical reward block when capacity is available.
        /// </summary>
        public bool TryAddBlock(BlockDefinitionSO definition)
        {
            if (definition == null || definition.IsTrap || !HasFreeSlot)
            {
                return false;
            }

            AddBlockInternal(definition);
            return true;
        }

        /// <summary>
        /// Removes one exact physical block by stable runtime id.
        /// </summary>
        public bool RemoveBlock(int instanceId)
        {
            for (int blockIndex = 0; blockIndex < _blocks.Count; blockIndex++)
            {
                if (_blocks[blockIndex].InstanceId != instanceId)
                {
                    continue;
                }

                _blocks.RemoveAt(blockIndex);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns whether this runtime deck still contains the requested physical block instance.
        /// </summary>
        public bool Contains(int instanceId)
        {
            for (int blockIndex = 0; blockIndex < _blocks.Count; blockIndex++)
            {
                if (_blocks[blockIndex].InstanceId == instanceId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds one physical block without capacity checks. Used only while constructing a validated runtime deck.
        /// </summary>
        private void AddBlockInternal(BlockDefinitionSO definition)
        {
            _blocks.Add(new DeckBlockInstance(_nextInstanceId++, definition));
        }
    }
}
