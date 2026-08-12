using System;
using DG.Tweening;
using PocBattle.Runtime;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Displays one world-space enemy sprite. Combat data remains outside the view; this class owns only presentation tweens.
    /// </summary>
    public sealed class EnemyView : MonoBehaviour
    {
        [SerializeField, Tooltip("SpriteRenderer used for the enemy placeholder or final sprite.")]
        private SpriteRenderer _spriteRenderer;

        /// <summary>Stable runtime party index represented by this view.</summary>
        private int _enemyIndex;

        /// <summary>Gets represented party index.</summary>
        public int EnemyIndex => _enemyIndex;

        /// <summary>Caches same-GameObject SpriteRenderer as a runtime safeguard.</summary>
        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>Kills presentation tweens when this reusable view is disabled.</summary>
        private void OnDisable()
        {
            transform.DOKill();
            _spriteRenderer?.DOKill();
        }

        /// <summary>
        /// Restores reusable view state and applies stable identity/tint for the current stage enemy.
        /// </summary>
        public void Configure(EnemySetupSnapshot setupSnapshot)
        {
            _enemyIndex = setupSnapshot.EnemyIndex;
            transform.DOKill();
            _spriteRenderer?.DOKill();

            if (_spriteRenderer != null)
            {
                Color color = setupSnapshot.DisplayColor;
                color.a = 1f;
                _spriteRenderer.color = color;
            }
        }

        /// <summary>Plays short hit punch feedback without mutating enemy combat data.</summary>
        public void PlayHit(float duration, float strength, int vibrato, float elasticity)
        {
            transform.DOKill();
            transform.DOPunchScale(Vector3.one * strength, duration, vibrato, elasticity);
        }

        /// <summary>
        /// Shrinks and fades a defeated enemy, then disables the reusable view so loot can occupy its former position cleanly.
        /// </summary>
        public void PlayDeath(float duration, Action completed)
        {
            transform.DOKill();
            _spriteRenderer?.DOKill();

            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
            if (_spriteRenderer != null)
            {
                sequence.Join(_spriteRenderer.DOFade(0f, duration).SetEase(Ease.InQuad));
            }

            sequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
                completed?.Invoke();
            });
        }

        /// <summary>Caches same-GameObject SpriteRenderer for inspector-friendly prefab authoring.</summary>
        private void Reset()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
