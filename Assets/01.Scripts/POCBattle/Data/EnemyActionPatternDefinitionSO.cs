using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// One sequential enemy pattern step that can contain one or more actions.
    /// </summary>
    [Serializable]
    public sealed class EnemyPatternStep
    {
        [SerializeField, Tooltip("Actions executed together when this pattern step resolves.")]
        private EnemyActionDefinitionSO[] _actions = Array.Empty<EnemyActionDefinitionSO>();

        /// <summary>Gets actions in this step.</summary>
        public IReadOnlyList<EnemyActionDefinitionSO> Actions => _actions;
    }

    /// <summary>
    /// Defines a deterministic sequential enemy pattern.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Enemies/Action Pattern", fileName = "EnemyActionPattern")]
    public sealed class EnemyActionPatternDefinitionSO : ScriptableObject
    {
        [SerializeField, Tooltip("Ordered enemy action steps.")]
        private EnemyPatternStep[] _steps = Array.Empty<EnemyPatternStep>();

        [SerializeField, Tooltip("When enabled, the pattern wraps back to step zero after the final step.")]
        private bool _loop = true;

        /// <summary>Gets number of pattern steps.</summary>
        public int StepCount => _steps.Length;

        /// <summary>Gets whether the pattern loops.</summary>
        public bool Loop => _loop;

        /// <summary>
        /// Returns the step corresponding to a runtime pattern index.
        /// </summary>
        public EnemyPatternStep GetStep(int patternIndex)
        {
            if (_steps.Length == 0)
            {
                return null;
            }

            int resolvedIndex = ResolveStepIndex(patternIndex);
            return _steps[resolvedIndex];
        }

        /// <summary>
        /// Resolves an arbitrary runtime index into a valid serialized step index.
        /// </summary>
        public int ResolveStepIndex(int patternIndex)
        {
            if (_steps.Length == 0)
            {
                return 0;
            }

            if (_loop)
            {
                int wrappedIndex = patternIndex % _steps.Length;
                return wrappedIndex < 0 ? wrappedIndex + _steps.Length : wrappedIndex;
            }

            return Mathf.Clamp(patternIndex, 0, _steps.Length - 1);
        }
    }
}
