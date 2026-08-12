using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Stores presentation-only tuning values so MonoBehaviours do not contain gameplay magic numbers.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Settings/Presentation Settings", fileName = "BattlePresentationSettings")]
    public sealed class BattlePresentationSettingsSO : ScriptableObject
    {
        [Header("Board")]
        [SerializeField, Tooltip("Height of a normal floor cell cube.")]
        private float _cellHeight = 0.1f;

        [SerializeField, Tooltip("Height of the four corner wall cells.")]
        private float _wallHeight = 1f;

        [SerializeField, Tooltip("World Y position used for floor cell centers.")]
        private float _cellCenterY = 0f;

        [SerializeField, Tooltip("World Y position used for block centers.")]
        private float _blockCenterY = 0.45f;

        [SerializeField, Tooltip("Block width/depth as a ratio of cell size.")]
        private float _blockFootprintRatio = 0.82f;

        [SerializeField, Tooltip("Block height.")]
        private float _blockHeight = 0.7f;

        [SerializeField, Tooltip("Scale multiplier used to highlight a selected block.")]
        private float _selectedBlockScale = 1.12f;

        [SerializeField, Tooltip("Duration of block reposition tweens.")]
        private float _blockMoveDuration = 0.18f;

        [Header("Block Edit Drag")]
        [SerializeField, Tooltip("World-space height added while an editable block is held by the mouse.")]
        private float _blockDragLiftHeight = 0.6f;

        [SerializeField, Tooltip("Duration of the lift-from-floor tween when drag begins.")]
        private float _blockDragLiftDuration = 0.12f;

        [SerializeField, Tooltip("Uniform scale multiplier applied while an editable block is held.")]
        private float _blockDragScale = 1.08f;

        [SerializeField, Tooltip("Duration used to place or return a held block when the mouse button is released.")]
        private float _blockDropDuration = 0.16f;

        [Header("Block Hit Bounce")]
        [SerializeField, Tooltip("Relative XYZ scale used for the first slime-like squash after player collision.")]
        private Vector3 _blockHitSquashScale = new Vector3(1.18f, 0.7f, 1.18f);

        [SerializeField, Tooltip("Relative XYZ scale used for the rebound stretch after player collision.")]
        private Vector3 _blockHitStretchScale = new Vector3(0.9f, 1.25f, 0.9f);

        [SerializeField, Tooltip("Total duration of the block squash/stretch/settle collision sequence.")]
        private float _blockHitBounceDuration = 0.32f;

        [Header("Cell Effects")]
        [SerializeField, Tooltip("World-space offset above the top surface of a normal cell used by cell-effect sprites.")]
        private float _cellEffectSurfaceOffset = 0.03f;

        [SerializeField, Tooltip("Cell-effect sprite size as a ratio of board cell size.")]
        private float _cellEffectFootprintRatio = 0.55f;

        [SerializeField, Tooltip("Sprite alpha used after a cell effect has triggered once during the current manual move.")]
        private float _cellEffectInactiveAlpha = 0.22f;

        [SerializeField, Tooltip("Scale multiplier used by the short trigger pulse before a cell effect visually deactivates.")]
        private float _cellEffectTriggerPulseScale = 1.22f;

        [SerializeField, Tooltip("Duration of the cell-effect trigger pulse.")]
        private float _cellEffectTriggerPulseDuration = 0.18f;

        [SerializeField, Tooltip("Scale multiplier used when a cell-effect deck item is shown as floating world loot.")]
        private float _lootCellEffectScaleMultiplier = 1.25f;

        [Header("Board Colors")]
        [SerializeField, Tooltip("Normal floor color.")]
        private Color _normalCellColor = new Color(0.12f, 0.12f, 0.14f, 1f);

        [SerializeField, Tooltip("Corner wall color.")]
        private Color _wallCellColor = new Color(0.28f, 0.3f, 0.34f, 1f);

        [Header("Player")]
        [SerializeField, Tooltip("World Y position of the player pivot.")]
        private float _playerCenterY = 0.75f;

        [SerializeField, Tooltip("Seconds used per traveled cell for slide movement.")]
        private float _moveSecondsPerCell = 0.08f;

        [SerializeField, Tooltip("Distance used by the blocked-move feedback punch.")]
        private float _invalidMoveDistance = 0.2f;

        [SerializeField, Tooltip("Duration of the blocked-move feedback tween.")]
        private float _invalidMoveDuration = 0.16f;

        [Header("Combat")]
        [SerializeField, Tooltip("Critical multiplier applied once per collected critical stack.")]
        private float _criticalMultiplier = 1.5f;

        [SerializeField, Tooltip("Delay before leaving automatic player battle resolution.")]
        private float _playerResolveDelay = 0.45f;

        [SerializeField, Tooltip("Delay before leaving the enemy turn after its actions resolve.")]
        private float _enemyResolveDelay = 0.65f;

        [SerializeField, Tooltip("Duration shared by short player/enemy hit punch feedback.")]
        private float _enemyHitDuration = 0.2f;

        [SerializeField, Tooltip("Scale strength of enemy hit punch feedback.")]
        private float _enemyHitStrength = 0.16f;

        [SerializeField, Tooltip("Scale strength of player hit punch feedback.")]
        private float _playerHitStrength = 0.12f;

        [SerializeField, Tooltip("DOTween punch vibrato used by player/enemy hit feedback.")]
        private int _hitPunchVibrato = 6;

        [SerializeField, Tooltip("DOTween punch elasticity used by player/enemy hit feedback.")]
        private float _hitPunchElasticity = 0.5f;

        [Header("Enemy Presentation")]
        [SerializeField, Tooltip("World position of the first enemy view.")]
        private Vector3 _enemyStartPosition = new Vector3(-1.5f, 2.2f, 4.1f);

        [SerializeField, Tooltip("World-space spacing between enemy views.")]
        private float _enemySpacing = 3f;

        [SerializeField, Tooltip("Euler rotation used by sprite enemy views so they face the angled camera.")]
        private Vector3 _enemyEulerAngles = new Vector3(45f, 0f, 0f);

        [SerializeField, Tooltip("Uniform scale applied to enemy sprite roots.")]
        private float _enemyScale = 1.7f;

        [Header("Enemy Death")]
        [SerializeField, Tooltip("Duration of the defeated enemy shrink/fade before post-battle looting begins.")]
        private float _enemyDeathDuration = 0.3f;

        [Header("Loot")]
        [SerializeField, Tooltip("Uniform scale multiplier applied to the floating loot block relative to a normal board block.")]
        private float _lootBlockScaleMultiplier = 1.15f;

        [SerializeField, Tooltip("Vertical offset from the defeated enemy position to the floating loot block center.")]
        private float _lootHeightOffset = 0.4f;

        [SerializeField, Tooltip("Vertical distance traveled by the looping floating loot animation.")]
        private float _lootFloatHeight = 0.25f;

        [SerializeField, Tooltip("Seconds used for one direction of the looping floating loot animation.")]
        private float _lootFloatDuration = 0.8f;

        [SerializeField, Tooltip("Base point-light intensity used to make the dropped loot glow.")]
        private float _lootGlowIntensity = 2.5f;

        [SerializeField, Tooltip("Point-light range used by the dropped loot glow.")]
        private float _lootGlowRange = 3f;

        [SerializeField, Tooltip("Seconds used for one direction of the loot glow pulse.")]
        private float _lootGlowPulseDuration = 0.65f;

        [SerializeField, Tooltip("Screen-space radius around the floating loot anchor that accepts a left-click.")]
        private float _lootClickRadiusPixels = 90f;

        [Header("Stage Transition")]
        [SerializeField, Tooltip("Seconds used by both fade-out and fade-in between run stages.")]
        private float _stageFadeDuration = 0.45f;

        [Header("Pointer")]
        [SerializeField, Tooltip("Maximum world raycast distance used for edit-phase mouse selection.")]
        private float _pointerRayDistance = 100f;

        /// <summary>Gets normal cell height.</summary>
        public float CellHeight => _cellHeight;

        /// <summary>Gets wall cell height.</summary>
        public float WallHeight => _wallHeight;

        /// <summary>Gets floor center Y.</summary>
        public float CellCenterY => _cellCenterY;

        /// <summary>Gets block center Y.</summary>
        public float BlockCenterY => _blockCenterY;

        /// <summary>Gets block footprint ratio.</summary>
        public float BlockFootprintRatio => _blockFootprintRatio;

        /// <summary>Gets block height.</summary>
        public float BlockHeight => _blockHeight;

        /// <summary>Gets selected block scale.</summary>
        public float SelectedBlockScale => _selectedBlockScale;

        /// <summary>Gets block tween duration.</summary>
        public float BlockMoveDuration => _blockMoveDuration;

        /// <summary>Gets height added while a block is held.</summary>
        public float BlockDragLiftHeight => _blockDragLiftHeight;

        /// <summary>Gets drag lift tween duration.</summary>
        public float BlockDragLiftDuration => _blockDragLiftDuration;

        /// <summary>Gets held block scale multiplier.</summary>
        public float BlockDragScale => _blockDragScale;

        /// <summary>Gets held block drop/return duration.</summary>
        public float BlockDropDuration => _blockDropDuration;

        /// <summary>Gets relative hit squash scale.</summary>
        public Vector3 BlockHitSquashScale => _blockHitSquashScale;

        /// <summary>Gets relative hit stretch scale.</summary>
        public Vector3 BlockHitStretchScale => _blockHitStretchScale;

        /// <summary>Gets total slime-like block hit bounce duration.</summary>
        public float BlockHitBounceDuration => _blockHitBounceDuration;

        /// <summary>Gets cell-effect sprite surface offset above the normal cell top.</summary>
        public float CellEffectSurfaceOffset => _cellEffectSurfaceOffset;

        /// <summary>Gets cell-effect sprite footprint ratio.</summary>
        public float CellEffectFootprintRatio => _cellEffectFootprintRatio;

        /// <summary>Gets alpha used by a cell effect disabled for the current manual move.</summary>
        public float CellEffectInactiveAlpha => _cellEffectInactiveAlpha;

        /// <summary>Gets trigger pulse scale multiplier.</summary>
        public float CellEffectTriggerPulseScale => _cellEffectTriggerPulseScale;

        /// <summary>Gets trigger pulse duration.</summary>
        public float CellEffectTriggerPulseDuration => _cellEffectTriggerPulseDuration;

        /// <summary>Gets floating loot scale multiplier for cell-effect items.</summary>
        public float LootCellEffectScaleMultiplier => _lootCellEffectScaleMultiplier;

        /// <summary>Gets normal floor color.</summary>
        public Color NormalCellColor => _normalCellColor;

        /// <summary>Gets wall floor color.</summary>
        public Color WallCellColor => _wallCellColor;

        /// <summary>Gets player world Y.</summary>
        public float PlayerCenterY => _playerCenterY;

        /// <summary>Gets movement time per crossed cell.</summary>
        public float MoveSecondsPerCell => _moveSecondsPerCell;

        /// <summary>Gets invalid move punch distance.</summary>
        public float InvalidMoveDistance => _invalidMoveDistance;

        /// <summary>Gets invalid move tween duration.</summary>
        public float InvalidMoveDuration => _invalidMoveDuration;

        /// <summary>Gets per-stack critical damage multiplier.</summary>
        public float CriticalMultiplier => _criticalMultiplier;

        /// <summary>Gets player resolve delay.</summary>
        public float PlayerResolveDelay => _playerResolveDelay;

        /// <summary>Gets enemy resolve delay.</summary>
        public float EnemyResolveDelay => _enemyResolveDelay;

        /// <summary>Gets enemy hit punch duration.</summary>
        public float EnemyHitDuration => _enemyHitDuration;

        /// <summary>Gets enemy hit punch strength.</summary>
        public float EnemyHitStrength => _enemyHitStrength;

        /// <summary>Gets player hit punch strength.</summary>
        public float PlayerHitStrength => _playerHitStrength;

        /// <summary>Gets hit punch vibrato.</summary>
        public int HitPunchVibrato => _hitPunchVibrato;

        /// <summary>Gets hit punch elasticity.</summary>
        public float HitPunchElasticity => _hitPunchElasticity;

        /// <summary>Gets first enemy position.</summary>
        public Vector3 EnemyStartPosition => _enemyStartPosition;

        /// <summary>Gets enemy spacing.</summary>
        public float EnemySpacing => _enemySpacing;

        /// <summary>Gets enemy view euler rotation.</summary>
        public Vector3 EnemyEulerAngles => _enemyEulerAngles;

        /// <summary>Gets enemy scale.</summary>
        public float EnemyScale => _enemyScale;

        /// <summary>Gets defeated enemy death animation duration.</summary>
        public float EnemyDeathDuration => _enemyDeathDuration;

        /// <summary>Gets loot block scale multiplier.</summary>
        public float LootBlockScaleMultiplier => _lootBlockScaleMultiplier;

        /// <summary>Gets vertical offset from defeated enemy to loot center.</summary>
        public float LootHeightOffset => _lootHeightOffset;

        /// <summary>Gets loot float travel distance.</summary>
        public float LootFloatHeight => _lootFloatHeight;

        /// <summary>Gets loot float half-cycle duration.</summary>
        public float LootFloatDuration => _lootFloatDuration;

        /// <summary>Gets base loot glow intensity.</summary>
        public float LootGlowIntensity => _lootGlowIntensity;

        /// <summary>Gets loot glow range.</summary>
        public float LootGlowRange => _lootGlowRange;

        /// <summary>Gets loot glow pulse half-cycle duration.</summary>
        public float LootGlowPulseDuration => _lootGlowPulseDuration;

        /// <summary>Gets screen-space loot click radius.</summary>
        public float LootClickRadiusPixels => _lootClickRadiusPixels;

        /// <summary>Gets stage fade duration.</summary>
        public float StageFadeDuration => _stageFadeDuration;

        /// <summary>Gets edit pointer ray distance.</summary>
        public float PointerRayDistance => _pointerRayDistance;

        /// <summary>
        /// Keeps presentation values in a usable range.
        /// </summary>
        private void OnValidate()
        {
            _cellHeight = Mathf.Max(0.01f, _cellHeight);
            _wallHeight = Mathf.Max(_cellHeight, _wallHeight);
            _blockFootprintRatio = Mathf.Clamp(_blockFootprintRatio, 0.1f, 1f);
            _blockHeight = Mathf.Max(0.05f, _blockHeight);
            _selectedBlockScale = Mathf.Max(1f, _selectedBlockScale);
            _blockMoveDuration = Mathf.Max(0f, _blockMoveDuration);
            _blockDragLiftHeight = Mathf.Max(0f, _blockDragLiftHeight);
            _blockDragLiftDuration = Mathf.Max(0.01f, _blockDragLiftDuration);
            _blockDragScale = Mathf.Max(1f, _blockDragScale);
            _blockDropDuration = Mathf.Max(0.01f, _blockDropDuration);
            _blockHitSquashScale = ClampPositiveScale(_blockHitSquashScale);
            _blockHitStretchScale = ClampPositiveScale(_blockHitStretchScale);
            _blockHitBounceDuration = Mathf.Max(0.03f, _blockHitBounceDuration);
            _cellEffectSurfaceOffset = Mathf.Max(0f, _cellEffectSurfaceOffset);
            _cellEffectFootprintRatio = Mathf.Clamp(_cellEffectFootprintRatio, 0.05f, 1f);
            _cellEffectInactiveAlpha = Mathf.Clamp01(_cellEffectInactiveAlpha);
            _cellEffectTriggerPulseScale = Mathf.Max(1f, _cellEffectTriggerPulseScale);
            _cellEffectTriggerPulseDuration = Mathf.Max(0.01f, _cellEffectTriggerPulseDuration);
            _lootCellEffectScaleMultiplier = Mathf.Max(0.1f, _lootCellEffectScaleMultiplier);
            _moveSecondsPerCell = Mathf.Max(0.01f, _moveSecondsPerCell);
            _invalidMoveDistance = Mathf.Max(0.01f, _invalidMoveDistance);
            _invalidMoveDuration = Mathf.Max(0.01f, _invalidMoveDuration);
            _criticalMultiplier = Mathf.Max(1f, _criticalMultiplier);
            _playerResolveDelay = Mathf.Max(0f, _playerResolveDelay);
            _enemyResolveDelay = Mathf.Max(0f, _enemyResolveDelay);
            _enemyHitDuration = Mathf.Max(0.01f, _enemyHitDuration);
            _enemyHitStrength = Mathf.Max(0.01f, _enemyHitStrength);
            _playerHitStrength = Mathf.Max(0.01f, _playerHitStrength);
            _hitPunchVibrato = Mathf.Max(1, _hitPunchVibrato);
            _hitPunchElasticity = Mathf.Clamp01(_hitPunchElasticity);
            _enemySpacing = Mathf.Max(0.1f, _enemySpacing);
            _enemyScale = Mathf.Max(0.1f, _enemyScale);
            _enemyDeathDuration = Mathf.Max(0.01f, _enemyDeathDuration);
            _lootBlockScaleMultiplier = Mathf.Max(0.1f, _lootBlockScaleMultiplier);
            _lootHeightOffset = Mathf.Max(0f, _lootHeightOffset);
            _lootFloatHeight = Mathf.Max(0f, _lootFloatHeight);
            _lootFloatDuration = Mathf.Max(0.05f, _lootFloatDuration);
            _lootGlowIntensity = Mathf.Max(0f, _lootGlowIntensity);
            _lootGlowRange = Mathf.Max(0.1f, _lootGlowRange);
            _lootGlowPulseDuration = Mathf.Max(0.05f, _lootGlowPulseDuration);
            _lootClickRadiusPixels = Mathf.Max(1f, _lootClickRadiusPixels);
            _stageFadeDuration = Mathf.Max(0.01f, _stageFadeDuration);
            _pointerRayDistance = Mathf.Max(1f, _pointerRayDistance);
        }

        /// <summary>
        /// Prevents invalid zero/negative relative squash or stretch axes.
        /// </summary>
        private static Vector3 ClampPositiveScale(Vector3 scale)
        {
            return new Vector3(
                Mathf.Max(0.01f, scale.x),
                Mathf.Max(0.01f, scale.y),
                Mathf.Max(0.01f, scale.z));
        }
    }
}
