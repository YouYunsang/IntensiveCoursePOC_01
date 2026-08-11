using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>Adds shield that is granted in the player battle phase.</summary>
    [CreateAssetMenu(menuName = "POC Battle/Block Effects/Shield", fileName = "ShieldEffect")]
    public sealed class ShieldBlockEffectSO : BlockEffectDefinitionSO
    {
        [SerializeField, Tooltip("Shield added during automatic player battle resolution.")]
        private int _amount = 3;

        /// <summary>Gets configured shield amount.</summary>
        public int Amount => _amount;

        /// <summary>Adds pending shield to the runtime receiver.</summary>
        public override void Apply(IBlockEffectReceiver receiver)
        {
            receiver.AddPendingShield(_amount);
        }

        /// <summary>Prevents negative shield values.</summary>
        private void OnValidate()
        {
            _amount = Mathf.Max(0, _amount);
        }
    }
}
