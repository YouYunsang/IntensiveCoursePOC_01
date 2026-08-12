using System.Globalization;
using System.Text;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Converts composable block effect data into concise world-space POC labels without parsing asset names.
    /// </summary>
    public static class BlockEffectTextFormatter
    {
        /// <summary>Reusable separator between multiple effects on one block.</summary>
        private const string LINE_SEPARATOR = "\n";

        /// <summary>
        /// Builds one presentation label from the block's actual effect values so text and gameplay cannot silently diverge.
        /// </summary>
        public static string Format(BlockDefinitionSO definition, float criticalMultiplier)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(32);
            bool hasWrittenEffect = false;
            for (int effectIndex = 0; effectIndex < definition.Effects.Count; effectIndex++)
            {
                BlockEffectDefinitionSO effect = definition.Effects[effectIndex];
                if (!TryFormatEffect(effect, criticalMultiplier, out string line))
                {
                    continue;
                }

                if (hasWrittenEffect)
                {
                    builder.Append(LINE_SEPARATOR);
                }

                builder.Append(line);
                hasWrittenEffect = true;
            }

            return builder.Length > 0 ? builder.ToString() : definition.DisplayName;
        }

        /// <summary>
        /// Formats one supported effect from its actual configured value and returns false for unknown/null effect types.
        /// </summary>
        private static bool TryFormatEffect(BlockEffectDefinitionSO effect, float criticalMultiplier, out string line)
        {
            switch (effect)
            {
                case AttackBlockEffectSO attack:
                    line = $"Attack +{attack.Amount}";
                    return true;

                case ShieldBlockEffectSO shield:
                    line = $"Shield +{shield.Amount}";
                    return true;

                case CriticalBlockEffectSO critical:
                    float multiplier = Mathf.Pow(criticalMultiplier, critical.StackCount);
                    line = $"Critical x{multiplier.ToString("0.##", CultureInfo.InvariantCulture)}";
                    return true;

                case ImmediateDamageBlockEffectSO damage:
                    line = $"HP -{damage.Damage}";
                    return true;

                default:
                    line = string.Empty;
                    return false;
            }
        }
    }
}
