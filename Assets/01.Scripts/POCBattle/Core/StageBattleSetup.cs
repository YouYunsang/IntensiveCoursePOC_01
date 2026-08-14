using System;
using System.Collections.Generic;
using PocBattle.Data;

namespace PocBattle.Core
{
    /// <summary>
    /// Immutable value object passed one-way from RunController to BattleController for one stage battle.
    /// It prevents BattleController from needing a reverse reference to RunController or StageDefinitionSO ownership logic.
    /// </summary>
    public readonly struct StageBattleSetup
    {
        private readonly EncounterDefinitionSO _encounter;
        private readonly IReadOnlyList<StageFieldEffectDefinitionSO> _fieldEffects;
        private readonly int _randomSeed;

        /// <summary>Gets the selected stage encounter.</summary>
        public EncounterDefinitionSO Encounter => _encounter;

        /// <summary>Gets immutable stage field-effect definitions.</summary>
        public IReadOnlyList<StageFieldEffectDefinitionSO> FieldEffects => _fieldEffects;

        /// <summary>Gets the stage board/system seed.</summary>
        public int RandomSeed => _randomSeed;

        /// <summary>Creates one immutable stage setup package.</summary>
        public StageBattleSetup(
            EncounterDefinitionSO encounter,
            IReadOnlyList<StageFieldEffectDefinitionSO> fieldEffects,
            int randomSeed)
        {
            _encounter = encounter != null ? encounter : throw new ArgumentNullException(nameof(encounter));
            _fieldEffects = fieldEffects ?? Array.Empty<StageFieldEffectDefinitionSO>();
            _randomSeed = randomSeed;
        }
    }
}
