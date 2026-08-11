using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Enemy attack action definition. The concrete ScriptableObject is kept in a matching file
    /// so Unity can serialize a valid MonoScript reference.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Enemies/Actions/Attack", fileName = "EnemyAttackAction")]
    public sealed class EnemyAttackActionSO : EnemyActionDefinitionSO
    {
        [SerializeField, Tooltip("Damage dealt to the player when this action resolves.")]
        private int _damage = 4;

        /// <summary>Gets configured attack damage.</summary>
        public int Damage => _damage;

        /// <summary>
        /// Builds an attack intent using the same damage value as execution.
        /// </summary>
        public override EnemyIntentPart BuildIntent()
        {
            return new EnemyIntentPart(EnemyIntentType.Attack, _damage);
        }

        /// <summary>
        /// Deals configured damage to the player.
        /// </summary>
        public override void Execute(int enemyIndex, IEnemyActionReceiver receiver)
        {
            receiver.DamagePlayer(enemyIndex, _damage);
        }

        /// <summary>
        /// Prevents negative damage values.
        /// </summary>
        private void OnValidate()
        {
            _damage = Mathf.Max(0, _damage);
        }
    }
}
