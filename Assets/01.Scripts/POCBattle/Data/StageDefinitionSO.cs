using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Defines one weighted block reward candidate for a stage loot pool.
    /// </summary>
    [Serializable]
    public sealed class WeightedBlockRewardEntry
    {
        [SerializeField, Tooltip("Player-collectible block that can drop after clearing this stage.")]
        private BlockDefinitionSO _block;

        [SerializeField, Tooltip("Relative random selection weight. Higher values are selected more often.")]
        private int _weight = 1;

        /// <summary>Gets reward block definition.</summary>
        public BlockDefinitionSO Block => _block;

        /// <summary>Gets positive random selection weight.</summary>
        public int Weight => Mathf.Max(0, _weight);
    }

    /// <summary>
    /// Defines the encounter pool and weighted loot pool used by one run stage.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Run/Stage Definition", fileName = "StageDefinition")]
    public sealed class StageDefinitionSO : ScriptableObject
    {
        [SerializeField, Tooltip("Encounter candidates. One encounter is selected randomly when this stage begins.")]
        private EncounterDefinitionSO[] _encounterPool = Array.Empty<EncounterDefinitionSO>();

        [SerializeField, Tooltip("Weighted player-block candidates used for the post-battle loot drop.")]
        private WeightedBlockRewardEntry[] _lootPool = Array.Empty<WeightedBlockRewardEntry>();

        /// <summary>Gets random encounter candidates.</summary>
        public IReadOnlyList<EncounterDefinitionSO> EncounterPool => _encounterPool;

        /// <summary>Gets weighted loot candidates.</summary>
        public IReadOnlyList<WeightedBlockRewardEntry> LootPool => _lootPool;
    }
}
