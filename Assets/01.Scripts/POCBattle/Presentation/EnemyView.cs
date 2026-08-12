using DG.Tweening;
using PocBattle.Runtime;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Displays only one world-space enemy sprite. POC status and intent information is rendered by BattleOnGuiPresenter.
    /// </summary>
    public sealed class EnemyView : MonoBehaviour
    {
        [SerializeField, Tooltip("SpriteRenderer used for the enemy placeholder or final sprite.")]
        private SpriteRenderer _spriteRenderer;

        /// <summary>Stable runtime party index represented by this view.</summary>
        private int _enemyIndex;

        /// <summary>Gets represented party index.</summary>
        public int EnemyIndex => _enemyIndex;

        /// <summary>
        /// Caches the same-GameObject SpriteRenderer when a manually created prefab omitted the Reset assignment.
        /// </summary>
        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// Kills hit feedback tweens when this view is reused or disabled.
        /// </summary>
        private void OnDisable()
        {
            transform.DOKill();
        }

        /// <summary>
        /// Configures stable identity and world-sprite tint only.
        /// </summary>
        public void Configure(EnemySetupSnapshot setupSnapshot)
        {
            _enemyIndex = setupSnapshot.EnemyIndex;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = setupSnapshot.DisplayColor;
            }
        }

        /// <summary>
        /// Plays DOTween hit feedback without changing enemy combat state.
        /// </summary>
        public void PlayHit(float duration, float strength, int vibrato, float elasticity)
        {
            transform.DOKill();
            transform.DOPunchScale(Vector3.one * strength, duration, vibrato, elasticity);
        }

        /// <summary>
        /// Caches the same-GameObject SpriteRenderer for inspector-friendly prefab authoring.
        /// </summary>
        private void Reset()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
