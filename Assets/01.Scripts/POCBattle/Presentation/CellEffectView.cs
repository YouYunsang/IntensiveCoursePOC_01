using DG.Tweening;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Generic world-space sprite view for one cell effect. It owns only orientation and short activation presentation.
    /// </summary>
    public sealed class CellEffectView : MonoBehaviour
    {
        /// <summary>Factor used to split the trigger pulse into grow/settle halves.</summary>
        private const float HALF_DURATION_FACTOR = 0.5f;

        [SerializeField, Tooltip("SpriteRenderer used to show the cell effect icon above the board cell.")]
        private SpriteRenderer _spriteRenderer;

        /// <summary>Configured base world scale restored after activation pulse.</summary>
        private Vector3 _baseScale;

        /// <summary>Current definition tint with active alpha.</summary>
        private Color _activeColor;

        /// <summary>Active trigger pulse owned by this view.</summary>
        private Tween _triggerTween;

        /// <summary>Caches same-prefab SpriteRenderer fallback.</summary>
        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>Kills active presentation tween when the pooled view is disabled.</summary>
        private void OnDisable()
        {
            KillTriggerTween();
        }

        /// <summary>Caches local prefab reference for inspector-friendly setup.</summary>
        private void Reset()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>Applies immutable effect data, turn direction, position, and configured board-space size.</summary>
        public void Configure(CellEffectDefinitionSO definition, Vector2Int direction, Vector3 worldPosition, float worldSize)
        {
            KillTriggerTween();
            transform.position = worldPosition;
            _baseScale = Vector3.one * worldSize;
            transform.localScale = _baseScale;
            transform.rotation = GetDirectionRotation(direction);

            if (_spriteRenderer == null)
            {
                return;
            }

            _spriteRenderer.sprite = definition != null ? definition.DisplaySprite : null;
            _activeColor = definition != null ? definition.DisplayColor : Color.white;
            _activeColor.a = 1f;
            _spriteRenderer.color = _activeColor;
        }

        /// <summary>Restores the effect as available at the beginning of a new manual movement input.</summary>
        public void SetMoveActive()
        {
            KillTriggerTween();
            transform.localScale = _baseScale;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _activeColor;
            }
        }

        /// <summary>Plays a short pulse then dims this effect for the rest of the current manual movement input.</summary>
        public void PlayTriggered(float pulseScale, float duration, float inactiveAlpha)
        {
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

        /// <summary>
        /// Rotates a source sprite whose painted arrow points left so local -X points along the logical world-space direction,
        /// while the sprite plane itself lies flat on the XZ board with its normal facing upward.
        /// </summary>
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

        /// <summary>Kills only the trigger pulse tween owned by this view.</summary>
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
