using PocBattle.Data;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Presentation-only formatter for every deck item type. Gameplay values remain sourced from immutable definitions.
    /// </summary>
    public static class DeckItemTextFormatter
    {
        /// <summary>Returns compact POC text without parsing asset names.</summary>
        public static string Format(DeckItemDefinitionSO definition, float criticalMultiplier)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            if (definition is BlockDefinitionSO blockDefinition)
            {
                return BlockEffectTextFormatter.Format(blockDefinition, criticalMultiplier);
            }

            if (definition is DirectionChangeCellEffectSO)
            {
                return "Direction Change";
            }

            return definition.DisplayName;
        }
    }
}
