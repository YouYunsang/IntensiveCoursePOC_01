using System.Collections.Generic;
using PocBattle.Data;

namespace PocBattle.Core
{
    /// <summary>
    /// Executes composable block effect assets against the current turn effect receiver.
    /// </summary>
    public sealed class BlockEffectResolver
    {
        /// <summary>Current-turn receiver for attack, shield, critical, and immediate trap effects.</summary>
        private readonly PlayerTurnEffectContext _turnEffects;

        /// <summary>
        /// Creates a resolver for one player turn effect context.
        /// </summary>
        public BlockEffectResolver(PlayerTurnEffectContext turnEffects)
        {
            _turnEffects = turnEffects;
        }

        /// <summary>
        /// Executes every effect on a collided block every time it is reached.
        /// </summary>
        public void Apply(BlockRuntime block)
        {
            if (block == null || block.Definition == null)
            {
                return;
            }

            IReadOnlyList<BlockEffectDefinitionSO> effects = block.Definition.Effects;
            for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
            {
                BlockEffectDefinitionSO effect = effects[effectIndex];
                if (effect != null)
                {
                    effect.Apply(_turnEffects);
                }
            }
        }
    }
}
