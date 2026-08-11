using System;
using DG.Tweening;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Handles player transform animation only; it never reads input or resolves game rules.
    /// </summary>
    public sealed class PlayerMovement : MonoBehaviour
    {
        /// <summary>Factor used to split invalid-move feedback into out/back halves.</summary>
        private const float HALF_DURATION_FACTOR = 0.5f;

        /// <summary>Currently active valid/invalid movement tween.</summary>
        private Tween _movementTween;

        /// <summary>Currently active hit scale tween, independent from movement.</summary>
        private Tween _hitTween;

        /// <summary>Gets whether a movement feedback tween is currently active.</summary>
        public bool IsAnimating => _movementTween != null && _movementTween.IsActive() && _movementTween.IsPlaying();

        /// <summary>
        /// Kills outstanding movement and hit tweens when this view is disabled.
        /// </summary>
        private void OnDisable()
        {
            KillMovementTween();
            KillHitTween();
        }

        /// <summary>
        /// Immediately synchronizes player transform to a logical position.
        /// </summary>
        public void SnapTo(Vector3 worldPosition)
        {
            KillMovementTween();
            transform.position = worldPosition;
        }

        /// <summary>
        /// Animates a valid ice slide to its already-resolved world destination.
        /// </summary>
        public void PlaySlide(Vector3 destination, float duration, Action completed)
        {
            KillMovementTween();
            _movementTween = transform
                .DOMove(destination, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _movementTween = null;
                    completed?.Invoke();
                });
        }

        /// <summary>
        /// Plays blocked-direction feedback without changing the logical or final transform position.
        /// </summary>
        public void PlayInvalidMove(Vector3 worldOffset, float duration, Action completed)
        {
            KillMovementTween();
            Vector3 startPosition = transform.position;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOMove(startPosition + worldOffset, duration * HALF_DURATION_FACTOR).SetEase(Ease.OutQuad));
            sequence.Append(transform.DOMove(startPosition, duration * HALF_DURATION_FACTOR).SetEase(Ease.InQuad));
            sequence.OnComplete(() =>
            {
                _movementTween = null;
                completed?.Invoke();
            });
            _movementTween = sequence;
        }

        /// <summary>
        /// Plays a short hit scale punch without interrupting movement position tweening.
        /// </summary>
        public void PlayHit(float duration, float strength, int vibrato, float elasticity)
        {
            KillHitTween();
            _hitTween = transform
                .DOPunchScale(Vector3.one * strength, duration, vibrato, elasticity)
                .OnComplete(() => _hitTween = null);
        }

        /// <summary>
        /// Kills only the active valid/invalid movement tween.
        /// </summary>
        private void KillMovementTween()
        {
            if (_movementTween != null && _movementTween.IsActive())
            {
                _movementTween.Kill();
            }

            _movementTween = null;
        }

        /// <summary>
        /// Kills only the active hit scale tween.
        /// </summary>
        private void KillHitTween()
        {
            if (_hitTween != null && _hitTween.IsActive())
            {
                _hitTween.Kill();
            }

            _hitTween = null;
        }
    }
}
