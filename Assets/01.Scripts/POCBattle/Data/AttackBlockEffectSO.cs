using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>Adds attack to the current turn context.</summary>
    [CreateAssetMenu(menuName = "POC Battle/Block Effects/Attack", fileName = "AttackEffect")]
    public sealed class AttackBlockEffectSO : BlockEffectDefinitionSO
    {
        [SerializeField, Tooltip("Attack power added when this block is collected.")]
        private int _amount = 2;

        /// <summary>Gets configured attack amount.</summary>
        public int Amount => _amount;

        /// <summary>Adds attack to the runtime receiver.</summary>
        public override void Apply(IBlockEffectReceiver receiver)
        {
            receiver.AddAttack(_amount);
        }

        /// <summary>Prevents negative attack values in the POC data.</summary>
        private void OnValidate()
        {
            _amount = Mathf.Max(0, _amount);
        }
    }
}
