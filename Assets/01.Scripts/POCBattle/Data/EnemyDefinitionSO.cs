using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Defines one enemy archetype and its deterministic action pattern.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Enemies/Enemy Definition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinitionSO : ScriptableObject
    {
        [SerializeField, Tooltip("Human-readable enemy name.")]
        private string _displayName = "Enemy";

        [SerializeField, Tooltip("Maximum enemy health.")]
        private int _maxHealth = 20;

        [SerializeField, Tooltip("POC sprite tint used by the enemy view.")]
        private Color _displayColor = Color.white;

        [SerializeField, Tooltip("Deterministic sequential action pattern for this enemy.")]
        private EnemyActionPatternDefinitionSO _pattern;

        /// <summary>Gets enemy display name.</summary>
        public string DisplayName => _displayName;

        /// <summary>Gets maximum health.</summary>
        public int MaxHealth => _maxHealth;

        /// <summary>Gets POC sprite tint.</summary>
        public Color DisplayColor => _displayColor;

        /// <summary>Gets deterministic action pattern.</summary>
        public EnemyActionPatternDefinitionSO Pattern => _pattern;

        /// <summary>
        /// Keeps health at a valid positive value.
        /// </summary>
        private void OnValidate()
        {
            _maxHealth = Mathf.Max(1, _maxHealth);
        }
    }
}
