using System;
using System.Collections.Generic;
using PocBattle.Data;

namespace PocBattle.Core
{
    /// <summary>
    /// Creates and owns stage-local field-effect runtimes. Random row/column choices are made once at battle-stage setup,
    /// never again at player-turn setup.
    /// </summary>
    public sealed class StageFieldEffectService
    {
        private readonly BoardModel _board;
        private readonly List<StageFieldEffectRuntime> _effects;
        private readonly System.Random _random;

        /// <summary>Gets every active field effect for this stage.</summary>
        public IReadOnlyList<StageFieldEffectRuntime> Effects => _effects;

        /// <summary>Creates field-effect runtimes from immutable stage definitions.</summary>
        public StageFieldEffectService(
            BoardModel board,
            IReadOnlyList<StageFieldEffectDefinitionSO> definitions,
            int randomSeed)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _effects = new List<StageFieldEffectRuntime>(definitions != null ? definitions.Count : 0);
            _random = new System.Random(randomSeed);
            BuildRuntimeEffects(definitions);
        }

        /// <summary>Creates supported concrete runtime effects while ignoring null entries explicitly.</summary>
        private void BuildRuntimeEffects(IReadOnlyList<StageFieldEffectDefinitionSO> definitions)
        {
            if (definitions == null)
            {
                return;
            }

            for (int definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
            {
                StageFieldEffectDefinitionSO definition = definitions[definitionIndex];
                if (definition == null)
                {
                    continue;
                }

                if (definition is RowColumnActivationFieldEffectSO rowColumnDefinition)
                {
                    int specialRow = _random.Next(0, _board.Rows);
                    int specialColumn = _random.Next(0, _board.Columns);
                    _effects.Add(
                        new RowColumnActivationFieldEffectRuntime(
                            _effects.Count,
                            rowColumnDefinition,
                            specialRow,
                            specialColumn));
                    continue;
                }

                throw new InvalidOperationException(
                    $"Unsupported stage field effect type '{definition.GetType().Name}'. Add an explicit runtime implementation before using it.");
            }
        }
    }
}
