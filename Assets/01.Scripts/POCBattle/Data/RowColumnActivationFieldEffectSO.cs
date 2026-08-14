using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Marks one random row and one random column for the whole stage. Directly colliding with a block on either line
    /// activates every unique block on the triggered line(s) exactly once.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Stage Field Effects/Row Column Activation", fileName = "FieldEffect_RowColumnActivation")]
    public sealed class RowColumnActivationFieldEffectSO : StageFieldEffectDefinitionSO
    {
        [Header("Idle Highlight")]
        [SerializeField, Tooltip("Color used by non-wall cells in the selected special row.")]
        private Color _rowHighlightColor = new Color(0.25f, 0.75f, 1f, 1f);

        [SerializeField, Tooltip("Color used by non-wall cells in the selected special column.")]
        private Color _columnHighlightColor = new Color(0.75f, 0.35f, 1f, 1f);

        [SerializeField, Tooltip("Color used by the row/column intersection cell.")]
        private Color _intersectionHighlightColor = new Color(1f, 0.55f, 0.2f, 1f);

        [SerializeField, Range(0f, 1f), Tooltip("How strongly the idle pulse drifts back toward the cell's normal color. Smaller values keep the field brighter.")]
        private float _idlePulseStrength = 0.18f;

        [SerializeField, Tooltip("Seconds used for one half-cycle of the special row/column idle color pulse.")]
        private float _idlePulseDuration = 0.8f;

        [Header("Activation Flash")]
        [SerializeField, Tooltip("Flash color shown on the triggered special row/column before returning to its idle pulse.")]
        private Color _activationFlashColor = Color.white;

        [SerializeField, Tooltip("Total duration of the short row/column activation flash.")]
        private float _activationFlashDuration = 0.24f;

        /// <summary>Gets special-row idle color.</summary>
        public Color RowHighlightColor => _rowHighlightColor;

        /// <summary>Gets special-column idle color.</summary>
        public Color ColumnHighlightColor => _columnHighlightColor;

        /// <summary>Gets row/column intersection idle color.</summary>
        public Color IntersectionHighlightColor => _intersectionHighlightColor;

        /// <summary>Gets idle pulse blend strength.</summary>
        public float IdlePulseStrength => _idlePulseStrength;

        /// <summary>Gets idle pulse half-cycle duration.</summary>
        public float IdlePulseDuration => _idlePulseDuration;

        /// <summary>Gets activation flash color.</summary>
        public Color ActivationFlashColor => _activationFlashColor;

        /// <summary>Gets total activation flash duration.</summary>
        public float ActivationFlashDuration => _activationFlashDuration;

        /// <summary>Keeps field-specific presentation values in stable ranges.</summary>
        private void OnValidate()
        {
            _idlePulseStrength = Mathf.Clamp01(_idlePulseStrength);
            _idlePulseDuration = Mathf.Max(0.05f, _idlePulseDuration);
            _activationFlashDuration = Mathf.Max(0.02f, _activationFlashDuration);
        }
    }
}
