using DG.Tweening;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Renders one 3D floor/wall cell using a shared material plus MaterialPropertyBlock color.
    /// It also owns stage-field idle pulse and short activation flash presentation.
    /// </summary>
    public sealed class BoardCellView : MonoBehaviour
    {
        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");

        [SerializeField, Tooltip("Renderer cached on this cell prefab.")]
        private Renderer _renderer;

        private MaterialPropertyBlock _propertyBlock;
        private Color _normalColor;
        private Color _fieldHighlightColor;
        private float _fieldPulseStrength;
        private float _fieldPulseDuration;
        private Tween _fieldPulseTween;
        private Tween _fieldFlashTween;

        /// <summary>Allocates one reusable property block for this cell view.</summary>
        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>Kills field presentation tweens when this view is disabled.</summary>
        private void OnDisable()
        {
            KillFieldPulseTween();
            KillFieldFlashTween();
        }

        /// <summary>Caches the local renderer when the component is added/reset in the editor.</summary>
        private void Reset()
        {
            _renderer = GetComponent<Renderer>();
        }

        /// <summary>Applies position, scale, and normal color without cloning materials.</summary>
        public void Configure(Vector3 position, Vector3 scale, Color color)
        {
            transform.position = position;
            transform.localScale = scale;
            _normalColor = color;
            _fieldHighlightColor = color;
            ApplyColor(color);
        }

        /// <summary>Clears any stage-field highlight and restores the original cell color.</summary>
        public void ClearFieldHighlight()
        {
            KillFieldPulseTween();
            KillFieldFlashTween();
            _fieldHighlightColor = _normalColor;
            ApplyColor(_normalColor);
        }

        /// <summary>Starts a low-cost looping color pulse for a special row/column cell.</summary>
        public void SetFieldHighlight(Color highlightColor, float pulseStrength, float pulseDuration)
        {
            KillFieldPulseTween();
            KillFieldFlashTween();
            _fieldHighlightColor = highlightColor;
            _fieldPulseStrength = Mathf.Clamp01(pulseStrength);
            _fieldPulseDuration = Mathf.Max(0.05f, pulseDuration);
            StartFieldPulse();
        }

        /// <summary>Flashes a triggered field cell, then resumes its configured idle pulse.</summary>
        public void PlayFieldActivationFlash(Color flashColor, float duration)
        {
            KillFieldPulseTween();
            KillFieldFlashTween();

            Color currentColor = _fieldHighlightColor;
            float halfDuration = Mathf.Max(0.01f, duration * 0.5f);
            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(() => currentColor, value =>
            {
                currentColor = value;
                ApplyColor(value);
            }, flashColor, halfDuration));
            sequence.Append(DOTween.To(() => currentColor, value =>
            {
                currentColor = value;
                ApplyColor(value);
            }, _fieldHighlightColor, halfDuration));
            sequence.OnComplete(() =>
            {
                _fieldFlashTween = null;
                StartFieldPulse();
            });
            _fieldFlashTween = sequence;
        }

        /// <summary>Starts the ambient color pulse from a dimmed highlight to the configured highlight.</summary>
        private void StartFieldPulse()
        {
            KillFieldPulseTween();
            Color dimColor = Color.Lerp(_fieldHighlightColor, _normalColor, _fieldPulseStrength);
            Color currentColor = dimColor;
            ApplyColor(currentColor);
            _fieldPulseTween = DOTween.To(() => currentColor, value =>
            {
                currentColor = value;
                ApplyColor(value);
            }, _fieldHighlightColor, _fieldPulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        /// <summary>Writes compatible color properties into the renderer property block.</summary>
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

        /// <summary>Kills only the idle field pulse tween.</summary>
        private void KillFieldPulseTween()
        {
            if (_fieldPulseTween != null && _fieldPulseTween.IsActive())
            {
                _fieldPulseTween.Kill();
            }
            _fieldPulseTween = null;
        }

        /// <summary>Kills only the field activation flash tween.</summary>
        private void KillFieldFlashTween()
        {
            if (_fieldFlashTween != null && _fieldFlashTween.IsActive())
            {
                _fieldFlashTween.Kill();
            }
            _fieldFlashTween = null;
        }
    }
}
