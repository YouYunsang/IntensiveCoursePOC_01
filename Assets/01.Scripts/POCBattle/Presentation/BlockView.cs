using DG.Tweening;
using PocBattle.Data;
using TMPro;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Explicit block presentation interaction states used instead of selection/drag booleans.
    /// </summary>
    internal enum BlockViewInteractionState
    {
        Resting = 0,
        Dragging = 1
    }

    /// <summary>
    /// Reusable 3D view for any deck or trap block definition, including drag/drop, hit bounce, and effect text.
    /// </summary>
    public sealed class BlockView : MonoBehaviour
    {
        /// <summary>URP base color shader property id.</summary>
        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");

        /// <summary>Built-in/standard color shader property id fallback.</summary>
        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");

        /// <summary>Portion of the total slime hit duration spent squashing.</summary>
        private const float HIT_SQUASH_DURATION_RATIO = 0.3f;

        /// <summary>Portion of the total slime hit duration spent stretching.</summary>
        private const float HIT_STRETCH_DURATION_RATIO = 0.35f;

        /// <summary>Portion of the total slime hit duration spent settling back to rest.</summary>
        private const float HIT_SETTLE_DURATION_RATIO = 0.35f;

        [SerializeField, Tooltip("Renderer cached on this generic block prefab.")]
        private Renderer _renderer;

        [SerializeField, Tooltip("World-space TMP child that displays the block's actual gameplay effect values.")]
        private TMP_Text _effectText;

        /// <summary>Reusable material property block.</summary>
        private MaterialPropertyBlock _propertyBlock;

        /// <summary>Configured resting scale restored after drag and hit feedback.</summary>
        private Vector3 _baseScale;

        /// <summary>Current explicit block interaction state.</summary>
        private BlockViewInteractionState _interactionState;

        /// <summary>World Y target used while following the mouse during drag.</summary>
        private float _dragWorldY;

        /// <summary>Active position tween.</summary>
        private Tween _moveTween;

        /// <summary>Active drag/settle scale tween.</summary>
        private Tween _scaleTween;

        /// <summary>Active slime hit squash/stretch sequence.</summary>
        private Sequence _hitSequence;

        /// <summary>Gets whether this block is currently held by the edit pointer.</summary>
        public bool IsDragging => _interactionState == BlockViewInteractionState.Dragging;

        /// <summary>
        /// Creates reusable presentation state and performs a same-prefab fallback cache for manually edited prefabs.
        /// </summary>
        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _baseScale = transform.localScale;
            _interactionState = BlockViewInteractionState.Resting;

            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            if (_effectText == null)
            {
                _effectText = GetComponentInChildren<TMP_Text>(true);
            }
        }

        /// <summary>
        /// Kills active tweens when a reused block view is disabled.
        /// </summary>
        private void OnDisable()
        {
            KillMoveTween();
            KillScaleTween();
            KillHitTween();
            _interactionState = BlockViewInteractionState.Resting;
        }

        /// <summary>
        /// Caches local prefab references for inspector-friendly setup.
        /// </summary>
        private void Reset()
        {
            _renderer = GetComponent<Renderer>();
            _effectText = GetComponentInChildren<TMP_Text>(true);
        }

        /// <summary>
        /// Applies definition color, dimensions, and effect-derived text without interrupting an active held pose.
        /// </summary>
        public void Configure(BlockDefinitionSO definition, Vector3 baseScale, float criticalMultiplier)
        {
            _baseScale = baseScale;
            ApplyColor(definition != null ? definition.DisplayColor : Color.white);
            ApplyEffectText(BlockEffectTextFormatter.Format(definition, criticalMultiplier));

            if (_interactionState == BlockViewInteractionState.Resting && _hitSequence == null)
            {
                transform.localScale = _baseScale;
            }
        }

        /// <summary>
        /// Snaps the view to a grid position and restores its resting pose.
        /// </summary>
        public void SnapTo(Vector3 position)
        {
            _interactionState = BlockViewInteractionState.Resting;
            KillMoveTween();
            KillScaleTween();
            KillHitTween();
            transform.position = position;
            transform.localScale = _baseScale;
        }

        /// <summary>
        /// Tweens the view to a newly randomized/non-drag grid position.
        /// </summary>
        public void MoveTo(Vector3 position, float duration)
        {
            _interactionState = BlockViewInteractionState.Resting;
            KillMoveTween();
            KillScaleTween();
            KillHitTween();
            transform.localScale = _baseScale;
            _moveTween = transform
                .DOMove(position, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => _moveTween = null);
        }

        /// <summary>
        /// Lifts a selected block from the floor and enlarges it slightly while the mouse remains held.
        /// </summary>
        public void BeginDrag(float liftHeight, float dragScale, float duration)
        {
            KillMoveTween();
            KillScaleTween();
            KillHitTween();
            _interactionState = BlockViewInteractionState.Dragging;
            _dragWorldY = transform.position.y + liftHeight;

            _moveTween = transform
                .DOMoveY(_dragWorldY, duration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => _moveTween = null);

            _scaleTween = transform
                .DOScale(_baseScale * dragScale, duration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => _scaleTween = null);
        }

        /// <summary>
        /// Follows mouse XZ movement while preserving the current lift tween's Y until the configured drag height is reached.
        /// </summary>
        public void FollowDrag(Vector3 pointerWorldPosition)
        {
            if (_interactionState != BlockViewInteractionState.Dragging)
            {
                return;
            }

            Vector3 currentPosition = transform.position;
            float worldY = _moveTween != null && _moveTween.IsActive() ? currentPosition.y : _dragWorldY;
            transform.position = new Vector3(pointerWorldPosition.x, worldY, pointerWorldPosition.z);
        }

        /// <summary>
        /// Drops a held block onto the authoritative logical target and restores its resting scale.
        /// </summary>
        public void DropTo(Vector3 position, float duration)
        {
            _interactionState = BlockViewInteractionState.Resting;
            KillMoveTween();
            KillScaleTween();
            KillHitTween();

            _moveTween = transform
                .DOMove(position, duration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => _moveTween = null);

            _scaleTween = transform
                .DOScale(_baseScale, duration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => _scaleTween = null);
        }

        /// <summary>
        /// Plays a slime-like squash, stretch, and settle bounce when the player collides with this block.
        /// </summary>
        public void PlayHitBounce(Vector3 squashScale, Vector3 stretchScale, float duration)
        {
            if (_interactionState == BlockViewInteractionState.Dragging)
            {
                return;
            }

            KillScaleTween();
            KillHitTween();

            Vector3 squashTarget = Vector3.Scale(_baseScale, squashScale);
            Vector3 stretchTarget = Vector3.Scale(_baseScale, stretchScale);
            _hitSequence = DOTween.Sequence();
            _hitSequence.Append(transform.DOScale(squashTarget, duration * HIT_SQUASH_DURATION_RATIO).SetEase(Ease.OutQuad));
            _hitSequence.Append(transform.DOScale(stretchTarget, duration * HIT_STRETCH_DURATION_RATIO).SetEase(Ease.OutBack));
            _hitSequence.Append(transform.DOScale(_baseScale, duration * HIT_SETTLE_DURATION_RATIO).SetEase(Ease.OutElastic));
            _hitSequence.OnComplete(() => _hitSequence = null);
        }

        /// <summary>
        /// Kills only the active position tween.
        /// </summary>
        private void KillMoveTween()
        {
            if (_moveTween != null && _moveTween.IsActive())
            {
                _moveTween.Kill();
            }

            _moveTween = null;
        }

        /// <summary>
        /// Kills only the active drag/settle scale tween.
        /// </summary>
        private void KillScaleTween()
        {
            if (_scaleTween != null && _scaleTween.IsActive())
            {
                _scaleTween.Kill();
            }

            _scaleTween = null;
        }

        /// <summary>
        /// Kills only the active collision bounce sequence and restores no pose by itself.
        /// </summary>
        private void KillHitTween()
        {
            if (_hitSequence != null && _hitSequence.IsActive())
            {
                _hitSequence.Kill();
            }

            _hitSequence = null;
        }

        /// <summary>
        /// Writes display color through MaterialPropertyBlock to avoid material cloning.
        /// </summary>
        private void ApplyColor(Color color)
        {
            if (_renderer == null)
            {
                return;
            }

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BASE_COLOR_ID, color);
            _propertyBlock.SetColor(COLOR_ID, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// Updates the cached world-space TMP label only when a definition/layout is configured, never every frame.
        /// </summary>
        private void ApplyEffectText(string effectText)
        {
            if (_effectText != null && _effectText.text != effectText)
            {
                _effectText.text = effectText;
            }
        }
    }
}
