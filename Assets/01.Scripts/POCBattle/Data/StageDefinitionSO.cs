using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Defines one weighted deck-item reward candidate for a stage loot pool.
    /// The serialized field name remains _block so existing stage assets retain their block reward references.
    /// </summary>
    [Serializable]
    public sealed class WeightedBlockRewardEntry
    {
        [SerializeField, Tooltip("Player-collectible deck item that can drop after clearing this stage.")]
        private DeckItemDefinitionSO _block;

        [SerializeField, Tooltip("Relative random selection weight. Higher values are selected more often.")]
        private int _weight = 1;

        /// <summary>Gets generalized reward item definition.</summary>
        public DeckItemDefinitionSO Item => _block;

        /// <summary>Compatibility accessor for older block-only editor tooling.</summary>
        public BlockDefinitionSO Block => _block as BlockDefinitionSO;

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

        [SerializeField, Tooltip("Weighted player deck-item candidates used for the post-battle loot drop.")]
        private WeightedBlockRewardEntry[] _lootPool = Array.Empty<WeightedBlockRewardEntry>();

        /// <summary>Gets random encounter candidates.</summary>
        public IReadOnlyList<EncounterDefinitionSO> EncounterPool => _encounterPool;

        /// <summary>Gets weighted loot candidates.</summary>
        public IReadOnlyList<WeightedBlockRewardEntry> LootPool => _lootPool;
    }
}
