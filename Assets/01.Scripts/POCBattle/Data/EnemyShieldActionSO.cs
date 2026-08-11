using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Enemy shield action definition. The concrete ScriptableObject is kept in a matching file
    /// so Unity can serialize a valid MonoScript reference.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Enemies/Actions/Shield", fileName = "EnemyShieldAction")]
    public sealed class EnemyShieldActionSO : EnemyActionDefinitionSO
    {
        [SerializeField, Tooltip("Shield granted to this enemy when the action resolves.")]
        private int _shield = 3;

        /// <summary>Gets configured shield amount.</summary>
        public int Shield => _shield;

        /// <summary>
        /// Builds a shield intent using the same value as execution.
        /// </summary>
        public override EnemyIntentPart BuildIntent()
        {
            return new EnemyIntentPart(EnemyIntentType.Shield, _shield);
        }

        /// <summary>
        /// Grants configured shield to the acting enemy.
        /// </summary>
        public override void Execute(int enemyIndex, IEnemyActionReceiver receiver)
        {
            receiver.GrantEnemyShield(enemyIndex, _shield);
        }

        /// <summary>
        /// Prevents negative shield values.
        /// </summary>
        private void OnValidate()
        {
            _shield = Mathf.Max(0, _shield);
        }
    }
}
