using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Renders one 3D floor/wall cell using a shared material plus MaterialPropertyBlock color.
    /// </summary>
    public sealed class BoardCellView : MonoBehaviour
    {
        /// <summary>URP base color shader property id.</summary>
        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");

        /// <summary>Built-in/standard color shader property id fallback.</summary>
        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");

        [SerializeField, Tooltip("Renderer cached on this cell prefab.")]
        private Renderer _renderer;

        /// <summary>Reusable property block avoids per-cell material instances.</summary>
        private MaterialPropertyBlock _propertyBlock;

        /// <summary>
        /// Allocates one reusable property block for this cell view.
        /// </summary>
        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Caches the local renderer when the component is added/reset in the editor.
        /// </summary>
        private void Reset()
        {
            _renderer = GetComponent<Renderer>();
        }

        /// <summary>
        /// Applies position, scale, and color without cloning materials.
        /// </summary>
        public void Configure(Vector3 position, Vector3 scale, Color color)
        {
            transform.position = position;
            transform.localScale = scale;
            ApplyColor(color);
        }

        /// <summary>
        /// Writes compatible color properties into the renderer property block.
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
