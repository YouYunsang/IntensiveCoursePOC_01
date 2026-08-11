using DG.Tweening;
using PocBattle.Runtime;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Displays one 2D sprite enemy plus world-space text for HP, shield, and next intent.
    /// </summary>
    public sealed class EnemyView : MonoBehaviour
    {
        [SerializeField, Tooltip("SpriteRenderer used for the enemy placeholder or final sprite.")]
        private SpriteRenderer _spriteRenderer;

        [SerializeField, Tooltip("World-space TextMesh displaying enemy HP and shield.")]
        private TextMesh _statusText;

        [SerializeField, Tooltip("World-space TextMesh displaying the next deterministic intent.")]
        private TextMesh _intentText;

        /// <summary>Stable runtime party index represented by this view.</summary>
        private int _enemyIndex;

        /// <summary>Configured display name retained for status text.</summary>
        private string _displayName;

        /// <summary>Gets represented party index.</summary>
        public int EnemyIndex => _enemyIndex;

        /// <summary>
        /// Kills hit feedback tweens when this view is reused or disabled.
        /// </summary>
        private void OnDisable()
        {
            transform.DOKill();
        }

        /// <summary>
        /// Configures stable identity, POC tint, and initial labels.
        /// </summary>
        public void Configure(EnemySetupSnapshot setupSnapshot)
        {
            _enemyIndex = setupSnapshot.EnemyIndex;
            _displayName = setupSnapshot.DisplayName;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = setupSnapshot.DisplayColor;
            }

            if (_statusText != null)
            {
                _statusText.text = $"{_displayName}\nHP {setupSnapshot.MaxHealth}/{setupSnapshot.MaxHealth}";
            }

            if (_intentText != null)
            {
                _intentText.text = "INTENT: ...";
            }
        }

        /// <summary>
        /// Updates HP/shield presentation from an immutable runtime snapshot.
        /// </summary>
        public void SetStatus(EnemyStatusSnapshot snapshot)
        {
            if (_statusText == null)
            {
                return;
            }

            _statusText.text = snapshot.IsAlive
                ? $"{_displayName}\nHP {snapshot.CurrentHealth}/{snapshot.MaxHealth}  SH {snapshot.Shield}"
                : $"{_displayName}\nDEFEATED";
        }

        /// <summary>
        /// Updates next-turn intent presentation generated from the actual enemy pattern action asset.
        /// </summary>
        public void SetIntent(EnemyIntentSnapshot snapshot)
        {
            if (_intentText != null)
            {
                _intentText.text = $"INTENT: {snapshot.IntentText}";
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
    }
}
