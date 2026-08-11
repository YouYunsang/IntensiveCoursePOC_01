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
            _pointerRayDistance = Mathf.Max(1f, _pointerRayDistance);
        }
    }
}
