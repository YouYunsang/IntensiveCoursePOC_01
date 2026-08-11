using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>Adds critical multiplier stacks to the current turn.</summary>
    [CreateAssetMenu(menuName = "POC Battle/Block Effects/Critical", fileName = "CriticalEffect")]
    public sealed class CriticalBlockEffectSO : BlockEffectDefinitionSO
    {
        [SerializeField, Tooltip("Number of critical stacks granted on collision.")]
        private int _stackCount = 1;

        /// <summary>Gets configured stack count.</summary>
        public int StackCount => _stackCount;

        /// <summary>Adds critical stacks to the runtime receiver.</summary>
        public override void Apply(IBlockEffectReceiver receiver)
        {
            receiver.AddCriticalStacks(_stackCount);
        }

        /// <summary>Ensures at least one stack is granted.</summary>
        private void OnValidate()
        {
            _stackCount = Mathf.Max(1, _stackCount);
        }
    }
}
