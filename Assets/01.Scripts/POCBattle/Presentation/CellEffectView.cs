using DG.Tweening;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>Explicit world cell-effect view states used instead of drag booleans.</summary>
    internal enum CellEffectViewInteractionState
    {
        Resting = 0,
        Dragging = 1
    }

    /// <summary>
    /// Generic collider-free world-space sprite view for one cell effect. It owns direction, edit drag, and short activation feedback.
    /// </summary>
    public sealed class CellEffectView : MonoBehaviour
    {
        private const float HALF_DURATION_FACTOR = 0.5f;

        [SerializeField, Tooltip("SpriteRenderer used to show the cell effect icon above the board cell.")]
        private SpriteRenderer _spriteRenderer;

        private Vector3 _baseScale;
        private Color _activeColor;
        private float _dragWorldY;
        private CellEffectViewInteractionState _interactionState;
        private Tween _moveTween;
        private Tween _scaleTween;
        private Tween _triggerTween;

        /// <summary>Gets whether this view is currently held by placement editing.</summary>
        public bool IsDragging => _interactionState == CellEffectViewInteractionState.Dragging;

        /// <summary>Caches same-prefab SpriteRenderer fallback.</summary>
        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            _interactionState = CellEffectViewInteractionState.Resting;
        }

        /// <summary>Kills owned presentation tweens when this pooled view is disabled.</summary>
        private void OnDisable()
        {
            KillMoveTween();
            KillScaleTween();
            KillTriggerTween();
            _interactionState = CellEffectViewInteractionState.Resting;
        }

        /// <summary>Caches local prefab reference for inspector-friendly setup.</summary>
        private void Reset()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>Applies immutable visual data, turn direction, and configured board-space size without moving the view.</summary>
        public void Configure(CellEffectDefinitionSO definition, Vector2Int direction, float worldSize)
        {
            _baseScale = Vector3.one * worldSize;
            transform.rotation = GetDirectionRotation(direction);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = definition != null ? definition.DisplaySprite : null;
                _activeColor = definition != null ? definition.DisplayColor : Color.white;
                _activeColor.a = 1f;
                _spriteRenderer.color = _activeColor;
            }

            if (_interactionState == CellEffectViewInteractionState.Resting && _triggerTween == null)
            {
                transform.localScale = _baseScale;
            }
        }

        /// <summary>Snaps to an authoritative cell position and restores resting presentation.</summary>
        public void SnapTo(Vector3 position)
        {
            _interactionState = CellEffectViewInteractionState.Resting;
            KillMoveTween();
            KillScaleTween();
            KillTriggerTween();
            transform.position = position;
            transform.localScale = _baseScale;
            SetMoveActive();
        }

        /// <summary>Moves a non-held effect to an authoritative edit target.</summary>
        public void MoveTo(Vector3 position, float duration)
        {
            _interactionState = CellEffectViewInteractionState.Resting;
            KillMoveTween();
            KillScaleTween();
            KillTriggerTween();
            transform.localScale = _baseScale;
            _moveTween = transform.DOMove(position, duration).SetEase(Ease.OutQuad).OnComplete(() => _moveTween = null);
        }

        /// <summary>Lifts an accepted cell effect from the board while the pointer remains held.</summary>
        public void BeginDrag(float liftHeight, float dragScale, float duration)
        {
            KillMoveTween();
            KillScaleTween();
            KillTriggerTween();
            _interactionState = CellEffectViewInteractionState.Dragging;
            _dragWorldY = transform.position.y + liftHeight;
            _moveTween = transform.DOMoveY(_dragWorldY, duration).SetEase(Ease.OutBack).OnComplete(() => _moveTween = null);
            _scaleTween = transform.DOScale(_baseScale * dragScale, duration).SetEase(Ease.OutBack).OnComplete(() => _scaleTween = null);
        }

        /// <summary>Follows mouse XZ while preserving the lifted Y pose.</summary>
        public void FollowDrag(Vector3 pointerWorldPosition)
        {
            if (_interactionState != CellEffectViewInteractionState.Dragging)
            {
                return;
            }

            float worldY = _moveTween != null && _moveTween.IsActive() ? transform.position.y : _dragWorldY;
            transform.position = new Vector3(pointerWorldPosition.x, worldY, pointerWorldPosition.z);
        }

        /// <summary>Drops a held effect to the authoritative destination and restores base scale.</summary>
        public void DropTo(Vector3 position, float duration)
        {
            _interactionState = CellEffectViewInteractionState.Resting;
            KillMoveTween();
            KillScaleTween();
            KillTriggerTween();
            _moveTween = transform.DOMove(position, duration).SetEase(Ease.OutBack).OnComplete(() => _moveTween = null);
            _scaleTween = transform.DOScale(_baseScale, duration).SetEase(Ease.OutBack).OnComplete(() => _scaleTween = null);
        }

        /// <summary>Restores the effect as available at the beginning of a new manual movement input.</summary>
        public void SetMoveActive()
        {
            KillTriggerTween();
            if (_interactionState == CellEffectViewInteractionState.Resting)
            {
                transform.localScale = _baseScale;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _activeColor;
            }
        }

        /// <summary>Plays a short pulse then dims this effect for the rest of the current manual movement input.</summary>
        public void PlayTriggered(float pulseScale, float duration, float inactiveAlpha)
        {
            if (_interactionState == CellEffectViewInteractionState.Dragging)
            {
                return;
            }

            KillTriggerTween();
            transform.localScale = _baseScale;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(_baseScale * pulseScale, duration * HALF_DURATION_FACTOR).SetEase(Ease.OutBack));
            sequence.Append(transform.DOScale(_baseScale, duration * HALF_DURATION_FACTOR).SetEase(Ease.InOutQuad));
            sequence.OnComplete(() =>
            {
                _triggerTween = null;
                if (_spriteRenderer == null)
                {
                    return;
                }

                Color inactiveColor = _activeColor;
                inactiveColor.a = inactiveAlpha;
                _spriteRenderer.color = inactiveColor;
            });
            _triggerTween = sequence;
        }

        /// <summary>Rotates a source sprite painted left so local -X points along the logical board direction.</summary>
        private static Quaternion GetDirectionRotation(Vector2Int direction)
        {
            Vector3 arrowWorldDirection = new Vector3(direction.x, 0f, direction.y);
            if (arrowWorldDirection.sqrMagnitude < 0.5f)
            {
                arrowWorldDirection = Vector3.left;
            }

            arrowWorldDirection.Normalize();
            Vector3 localRightWorld = -arrowWorldDirection;
            Vector3 localUpWorld = Vector3.Cross(Vector3.up, localRightWorld).normalized;
            return Quaternion.LookRotation(Vector3.up, localUpWorld);
        }

        /// <summary>Kills only the active position tween.</summary>
        private void KillMoveTween()
        {
            if (_moveTween != null && _moveTween.IsActive())
            {
                _moveTween.Kill();
            }
            _moveTween = null;
        }

        /// <summary>Kills only the active drag/settle scale tween.</summary>
        private void KillScaleTween()
        {
            if (_scaleTween != null && _scaleTween.IsActive())
            {
                _scaleTween.Kill();
            }
            _scaleTween = null;
        }

        /// <summary>Kills only the trigger pulse tween.</summary>
        private void KillTriggerTween()
        {
            if (_triggerTween != null && _triggerTween.IsActive())
            {
                _triggerTween.Kill();
            }
            _triggerTween = null;
        }
    }
}
