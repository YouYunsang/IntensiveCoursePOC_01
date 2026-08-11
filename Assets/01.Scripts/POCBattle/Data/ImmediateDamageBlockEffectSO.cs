using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>Applies immediate trap damage on collision.</summary>
    [CreateAssetMenu(menuName = "POC Battle/Block Effects/Immediate Damage", fileName = "ImmediateDamageEffect")]
    public sealed class ImmediateDamageBlockEffectSO : BlockEffectDefinitionSO
    {
        [SerializeField, Tooltip("HP damage immediately dealt by the trap block.")]
        private int _damage = 3;

        /// <summary>Gets configured immediate damage.</summary>
        public int Damage => _damage;

        /// <summary>Immediately damages the runtime receiver.</summary>
        public override void Apply(IBlockEffectReceiver receiver)
        {
            receiver.ApplyImmediateDamage(_damage);
        }

        /// <summary>Prevents invalid negative trap damage.</summary>
        private void OnValidate()
        {
            _damage = Mathf.Max(0, _damage);
        }
    }
}
