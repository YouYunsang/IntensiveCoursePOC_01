using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Immediately restores player HP when the player collides with a heal block.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Block Effects/Heal", fileName = "HealEffect")]
    public sealed class HealBlockEffectSO : BlockEffectDefinitionSO
    {
        [SerializeField, Tooltip("HP restored immediately when this block is collected.")]
        private int _amount = 3;

        /// <summary>Gets configured heal amount.</summary>
        public int Amount => _amount;

        /// <summary>Applies immediate healing through the runtime effect receiver.</summary>
        public override void Apply(IBlockEffectReceiver receiver)
        {
            receiver.ApplyImmediateHeal(_amount);
        }

        /// <summary>Prevents negative healing values.</summary>
        private void OnValidate()
        {
            _amount = Mathf.Max(0, _amount);
        }
    }
}
