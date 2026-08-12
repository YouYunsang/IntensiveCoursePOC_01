using DG.Tweening;
using PocBattle.Data;
using PocBattle.Runtime;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Owns one reusable world-space loot visual for either a block or a cell-effect deck item.
    /// </summary>
    public sealed class LootDropPresenter : MonoBehaviour
    {
        /// <summary>Shader property used by URP Lit and common color shaders without cloning materials.</summary>
        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");

        private const float CELL_EFFECT_PLATE_RADIUS_RATIO = 0.35f;
        private const float CELL_EFFECT_PLATE_HEIGHT_RATIO = 0.18f;
        private const float LOOT_SCALE_PULSE_MULTIPLIER = 1.06f;
        private const float LOOT_GLOW_LOW_INTENSITY_RATIO = 0.55f;

        /// <summary>Explicit active loot visual kind instead of separate visibility flags.</summary>
        private enum LootVisualKind
        {
            None = 0,
            Block = 1,
            CellEffect = 2
        }

        [SerializeField, Tooltip("Shared event hub used for loot appearance without direct RunController references.")]
        private BattleEventChannelSO _eventChannel;

        [SerializeField, Tooltip("Board size used to derive loot dimensions.")]
        private BattleBoardSettingsSO _boardSettings;

        [SerializeField, Tooltip("Loot float/glow and item visual tuning.")]
        private BattlePresentationSettingsSO _presentationSettings;

        [SerializeField, Tooltip("Existing generic Block prefab reused for block rewards.")]
        private BlockView _blockPrefab;

        [SerializeField, Tooltip("Collider-free cell-effect sprite prefab reused for cell-effect rewards.")]
        private CellEffectView _cellEffectPrefab;

        /// <summary>Reusable block reward view.</summary>
        private BlockView _lootBlockView;

        /// <summary>Reusable root containing the floating cell-effect icon and a small glowing plate.</summary>
        private Transform _cellEffectLootRoot;

        /// <summary>Reusable cell-effect reward view.</summary>
        private CellEffectView _lootCellEffectView;

        /// <summary>Reusable cylinder plate renderer for cell-effect loot.</summary>
        private Renderer _cellEffectPlateRenderer;

        /// <summary>Reusable point light shared by both loot visual kinds.</summary>
        private Light _glowLight;

        /// <summary>Reusable property block for the cell-effect loot plate.</summary>
        private MaterialPropertyBlock _platePropertyBlock;

        /// <summary>Current active loot view kind.</summary>
        private LootVisualKind _visualKind;

        /// <summary>Root transform currently animated by float/scale loops.</summary>
        private Transform _activeVisualTransform;

        /// <summary>Configured resting scale of the active visual root.</summary>
        private Vector3 _activeBaseScale;

        /// <summary>Looping loot float tween.</summary>
        private Tween _floatTween;

        /// <summary>Looping glow intensity tween.</summary>
        private Tween _glowTween;

        /// <summary>Subtle looping scale pulse.</summary>
        private Tween _pulseTween;

        /// <summary>Creates reusable material-property state.</summary>
        private void Awake()
        {
            _platePropertyBlock = new MaterialPropertyBlock();
            _visualKind = LootVisualKind.None;
        }

        /// <summary>Subscribes to loot visibility snapshots.</summary>
        private void OnEnable()
        {
            if (_eventChannel != null)
            {
                _eventChannel.LootDropChanged += HandleLootDropChanged;
            }
        }

        /// <summary>Removes subscription and stops presentation loops.</summary>
        private void OnDisable()
        {
            if (_eventChannel != null)
            {
                _eventChannel.LootDropChanged -= HandleLootDropChanged;
            }

            HideLoot();
        }

        /// <summary>Creates/reuses or hides the floating world reward from immutable generalized deck-item data.</summary>
        private void HandleLootDropChanged(LootDropSnapshot snapshot)
        {
            if (!snapshot.IsVisible || snapshot.Definition == null)
            {
                HideLoot();
                return;
            }

            KillTweens();
            Vector3 basePosition = snapshot.WorldPosition + Vector3.up * _presentationSettings.LootHeightOffset;

            if (snapshot.Definition is BlockDefinitionSO blockDefinition)
            {
                ShowBlockLoot(blockDefinition, basePosition);
            }
            else if (snapshot.Definition is CellEffectDefinitionSO cellEffectDefinition)
            {
                ShowCellEffectLoot(cellEffectDefinition, basePosition);
            }
            else
            {
                HideLoot();
                return;
            }

            ConfigureGlow(snapshot.Definition.DisplayColor, basePosition);
            StartLoops(basePosition);
        }

        /// <summary>Configures the reusable generic block reward.</summary>
        private void ShowBlockLoot(BlockDefinitionSO definition, Vector3 basePosition)
        {
            EnsureBlockView();
            HideCellEffectView();

            Vector3 baseScale = new Vector3(
                _boardSettings.CellSize * _presentationSettings.BlockFootprintRatio,
                _presentationSettings.BlockHeight,
                _boardSettings.CellSize * _presentationSettings.BlockFootprintRatio)
                * _presentationSettings.LootBlockScaleMultiplier;

            _lootBlockView.gameObject.SetActive(true);
            _lootBlockView.Configure(definition, baseScale, _presentationSettings.CriticalMultiplier);
            _lootBlockView.SnapTo(basePosition);
            _activeVisualTransform = _lootBlockView.transform;
            _activeBaseScale = baseScale;
            _visualKind = LootVisualKind.Block;
        }

        /// <summary>Configures the reusable arrow-on-plate reward for a cell-effect deck item.</summary>
        private void ShowCellEffectLoot(CellEffectDefinitionSO definition, Vector3 basePosition)
        {
            EnsureCellEffectView();
            HideBlockView();

            _cellEffectLootRoot.gameObject.SetActive(true);
            _cellEffectLootRoot.position = basePosition;
            _cellEffectLootRoot.rotation = Quaternion.identity;
            _cellEffectLootRoot.localScale = Vector3.one;

            float iconSize = _boardSettings.CellSize
                             * _presentationSettings.CellEffectFootprintRatio
                             * _presentationSettings.LootCellEffectScaleMultiplier;
            _lootCellEffectView.gameObject.SetActive(true);
            _lootCellEffectView.Configure(definition, Vector2Int.left, basePosition + Vector3.up * _presentationSettings.CellEffectSurfaceOffset, iconSize);
            _lootCellEffectView.transform.SetParent(_cellEffectLootRoot, true);

            Transform plateTransform = _cellEffectPlateRenderer.transform;
            plateTransform.localPosition = Vector3.zero;
            plateTransform.localRotation = Quaternion.identity;
            plateTransform.localScale = new Vector3(
                _boardSettings.CellSize * CELL_EFFECT_PLATE_RADIUS_RATIO,
                _presentationSettings.CellHeight * CELL_EFFECT_PLATE_HEIGHT_RATIO,
                _boardSettings.CellSize * CELL_EFFECT_PLATE_RADIUS_RATIO);
            ApplyPlateColor(definition.DisplayColor);

            _activeVisualTransform = _cellEffectLootRoot;
            _activeBaseScale = Vector3.one;
            _visualKind = LootVisualKind.CellEffect;
        }

        /// <summary>Creates the block view lazily and reuses it across stages.</summary>
        private void EnsureBlockView()
        {
            if (_lootBlockView != null)
            {
                return;
            }

            _lootBlockView = Instantiate(_blockPrefab, transform);
            _lootBlockView.name = "LootBlockView";
        }

        /// <summary>Creates the cell-effect root, icon, and simple plate only once.</summary>
        private void EnsureCellEffectView()
        {
            if (_cellEffectLootRoot != null)
            {
                return;
            }

            GameObject rootObject = new GameObject("LootCellEffectRoot");
            rootObject.transform.SetParent(transform, false);
            _cellEffectLootRoot = rootObject.transform;

            _lootCellEffectView = Instantiate(_cellEffectPrefab, _cellEffectLootRoot);
            _lootCellEffectView.name = "LootCellEffectIcon";

            GameObject plateObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plateObject.name = "LootCellEffectPlate";
            plateObject.transform.SetParent(_cellEffectLootRoot, false);
            Collider plateCollider = plateObject.GetComponent<Collider>();
            if (plateCollider != null)
            {
                Destroy(plateCollider);
            }

            _cellEffectPlateRenderer = plateObject.GetComponent<Renderer>();
        }

        /// <summary>Creates/reuses the shared point light and applies current reward color.</summary>
        private void ConfigureGlow(Color color, Vector3 basePosition)
        {
            if (_glowLight == null)
            {
                GameObject lightObject = new GameObject("LootGlow");
                lightObject.transform.SetParent(transform, false);
                _glowLight = lightObject.AddComponent<Light>();
                _glowLight.type = LightType.Point;
                _glowLight.shadows = LightShadows.None;
            }

            _glowLight.gameObject.SetActive(true);
            _glowLight.transform.position = basePosition;
            _glowLight.color = color;
            _glowLight.range = _presentationSettings.LootGlowRange;
            _glowLight.intensity = _presentationSettings.LootGlowIntensity;
        }

        /// <summary>Starts shared floating, scale-pulse, and glow-pulse loops on the active visual root.</summary>
        private void StartLoops(Vector3 basePosition)
        {
            _floatTween = _activeVisualTransform
                .DOMoveY(basePosition.y + _presentationSettings.LootFloatHeight, _presentationSettings.LootFloatDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            _pulseTween = _activeVisualTransform
                .DOScale(_activeBaseScale * LOOT_SCALE_PULSE_MULTIPLIER, _presentationSettings.LootGlowPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            float lowIntensity = _presentationSettings.LootGlowIntensity * LOOT_GLOW_LOW_INTENSITY_RATIO;
            _glowTween = DOTween.To(
                    () => _glowLight.intensity,
                    value => _glowLight.intensity = value,
                    lowIntensity,
                    _presentationSettings.LootGlowPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>Stops loops and hides every reusable loot presentation.</summary>
        private void HideLoot()
        {
            KillTweens();
            HideBlockView();
            HideCellEffectView();
            if (_glowLight != null)
            {
                _glowLight.gameObject.SetActive(false);
            }

            _activeVisualTransform = null;
            _visualKind = LootVisualKind.None;
        }

        /// <summary>Hides the reusable block reward if it exists.</summary>
        private void HideBlockView()
        {
            if (_lootBlockView != null)
            {
                _lootBlockView.gameObject.SetActive(false);
            }
        }

        /// <summary>Hides the reusable cell-effect reward group if it exists.</summary>
        private void HideCellEffectView()
        {
            if (_cellEffectLootRoot != null)
            {
                _cellEffectLootRoot.gameObject.SetActive(false);
            }
        }

        /// <summary>Writes plate color through MaterialPropertyBlock without cloning a material.</summary>
        private void ApplyPlateColor(Color color)
        {
            if (_cellEffectPlateRenderer == null)
            {
                return;
            }

            _cellEffectPlateRenderer.GetPropertyBlock(_platePropertyBlock);
            _platePropertyBlock.SetColor(BASE_COLOR_ID, color);
            _platePropertyBlock.SetColor(COLOR_ID, color);
            _cellEffectPlateRenderer.SetPropertyBlock(_platePropertyBlock);
        }

        /// <summary>Kills only loot-owned looping tweens.</summary>
        private void KillTweens()
        {
            if (_floatTween != null && _floatTween.IsActive())
            {
                _floatTween.Kill();
            }
            _floatTween = null;

            if (_glowTween != null && _glowTween.IsActive())
            {
                _glowTween.Kill();
            }
            _glowTween = null;

            if (_pulseTween != null && _pulseTween.IsActive())
            {
                _pulseTween.Kill();
            }
            _pulseTween = null;

            if (_activeVisualTransform != null)
            {
                _activeVisualTransform.localScale = _activeBaseScale;
            }
        }
    }
}
