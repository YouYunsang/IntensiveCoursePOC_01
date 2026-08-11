using DG.Tweening;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Reusable 3D view for any deck or trap block definition.
    /// </summary>
    public sealed class BlockView : MonoBehaviour
    {
        /// <summary>URP base color shader property id.</summary>
        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");

        /// <summary>Built-in/standard color shader property id fallback.</summary>
        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");

        [SerializeField, Tooltip("Renderer cached on this generic block prefab.")]
        private Renderer _renderer;

        /// <summary>Reusable material property block.</summary>
        private MaterialPropertyBlock _propertyBlock;

        /// <summary>Configured unselected scale for selection tween reset.</summary>
        private Vector3 _baseScale;

        /// <summary>Active position tween kept separate from selection scale tween.</summary>
        private Tween _moveTween;

        /// <summary>Active selection scale tween kept separate from position tween.</summary>
        private Tween _scaleTween;

        /// <summary>
        /// Creates one reusable material property block.
        /// </summary>
        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _baseScale = transform.localScale;
        }

        /// <summary>
        /// Kills active tweens when a pooled/reused block view is disabled.
        /// </summary>
        private void OnDisable()
        {
            KillMoveTween();
            KillScaleTween();
        }

        /// <summary>
        /// Caches the local renderer when prefab wiring is reset in the editor.
        /// </summary>
        private void Reset()
        {
            _renderer = GetComponent<Renderer>();
        }

        /// <summary>
        /// Applies definition color and base dimensions to this generic block view.
        /// </summary>
        public void Configure(BlockDefinitionSO definition, Vector3 baseScale)
        {
            KillScaleTween();
            _baseScale = baseScale;
            transform.localScale = _baseScale;
            ApplyColor(definition != null ? definition.DisplayColor : Color.white);
        }

        /// <summary>
        /// Snaps the view to a grid position without movement tweening.
        /// </summary>
        public void SnapTo(Vector3 position)
        {
            KillMoveTween();
            transform.position = position;
        }

        /// <summary>
        /// Tweens the view to a newly edited/re-randomized grid position without disturbing selection scaling.
        /// </summary>
        public void MoveTo(Vector3 position, float duration)
        {
            KillMoveTween();
            _moveTween = transform
                .DOMove(position, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => _moveTween = null);
        }

        /// <summary>
        /// Tweens selection scale without interrupting position movement.
        /// </summary>
        public void SetSelected(bool selected, float selectedScale, float duration)
        {
            KillScaleTween();
            Vector3 targetScale = selected ? _baseScale * selectedScale : _baseScale;
            _scaleTween = transform
                .DOScale(targetScale, duration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => _scaleTween = null);
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
        /// Kills only the active selection scale tween.
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
    }
}
