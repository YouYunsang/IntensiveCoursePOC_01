using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Defines the ordered stage sequence and deterministic test seed policy for one complete run.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Run/Run Definition", fileName = "RunDefinition")]
    public sealed class RunDefinitionSO : ScriptableObject
    {
        [SerializeField, Tooltip("Ordered stage definitions. Clearing every stage completes the run.")]
        private StageDefinitionSO[] _stages = Array.Empty<StageDefinitionSO>();

        [SerializeField, Tooltip("Use a stable run seed so encounter and loot selection can be reproduced while testing.")]
        private bool _useFixedRandomSeed;

        [SerializeField, Tooltip("Base run seed used when fixed random seed mode is enabled.")]
        private int _fixedRandomSeed = 24680;

        /// <summary>Gets ordered stage definitions.</summary>
        public IReadOnlyList<StageDefinitionSO> Stages => _stages;

        /// <summary>Gets whether the run uses a deterministic test seed.</summary>
        public bool UseFixedRandomSeed => _useFixedRandomSeed;

        /// <summary>Gets deterministic base run seed.</summary>
        public int FixedRandomSeed => _fixedRandomSeed;
    }
}
