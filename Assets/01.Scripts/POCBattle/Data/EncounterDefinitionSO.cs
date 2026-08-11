using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Defines enemies and encounter-owned trap blocks for one battle.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Encounters/Encounter Definition", fileName = "EncounterDefinition")]
    public sealed class EncounterDefinitionSO : ScriptableObject
    {
        [SerializeField, Tooltip("Enemies spawned in front-to-back target order.")]
        private EnemyDefinitionSO[] _enemies = Array.Empty<EnemyDefinitionSO>();

        [SerializeField, Tooltip("Trap block added independently from the player's deck.")]
        private BlockDefinitionSO _trapDefinition;

        [SerializeField, Tooltip("Number of trap copies placed every player turn.")]
        private int _trapCount = 2;

        /// <summary>Gets enemies in target priority order.</summary>
        public IReadOnlyList<EnemyDefinitionSO> Enemies => _enemies;

        /// <summary>Gets encounter trap definition.</summary>
        public BlockDefinitionSO TrapDefinition => _trapDefinition;

        /// <summary>Gets number of encounter trap blocks.</summary>
        public int TrapCount => _trapCount;

        /// <summary>
        /// Prevents a negative trap count.
        /// </summary>
        private void OnValidate()
        {
            _trapCount = Mathf.Max(0, _trapCount);
        }
    }
}
