using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Stores board dimensions and gameplay rules that define valid board composition.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Settings/Board Settings", fileName = "BattleBoardSettings")]
    public sealed class BattleBoardSettingsSO : ScriptableObject
    {
        [SerializeField, Tooltip("Number of grid columns.")]
        private int _columns = 8;

        [SerializeField, Tooltip("Number of grid rows.")]
        private int _rows = 5;

        [SerializeField, Tooltip("Size of one cell on the XZ plane.")]
        private float _cellSize = 1f;

        [SerializeField, Tooltip("Gap between neighboring cells.")]
        private float _cellGap = 0.08f;

        [SerializeField, Tooltip("World-space center of the complete board.")]
        private Vector3 _boardCenter = Vector3.zero;

        [SerializeField, Tooltip("Player starting grid coordinate.")]
        private Vector2Int _playerStartPosition = new Vector2Int(3, 2);

        [SerializeField, Tooltip("Maximum total number of player deck items allowed for this board configuration, including blocks and cell effects.")]
        private int _maxDeckBlockCount = 20;

        [SerializeField, Tooltip("When enabled, two editable deck blocks may swap positions during the edit phase.")]
        private bool _allowBlockSwap = true;

        [SerializeField, Tooltip("Use a stable placement seed so board layouts can be reproduced while testing.")]
        private bool _useFixedRandomSeed;

        [SerializeField, Tooltip("Placement seed used when fixed random seed mode is enabled.")]
        private int _fixedRandomSeed = 12345;

        /// <summary>Gets the number of columns.</summary>
        public int Columns => _columns;

        /// <summary>Gets the number of rows.</summary>
        public int Rows => _rows;

        /// <summary>Gets one cell size.</summary>
        public float CellSize => _cellSize;

        /// <summary>Gets the distance between neighboring cell centers.</summary>
        public float CellStep => _cellSize + _cellGap;

        /// <summary>Gets the world-space board center.</summary>
        public Vector3 BoardCenter => _boardCenter;

        /// <summary>Gets the starting player coordinate.</summary>
        public Vector2Int PlayerStartPosition => _playerStartPosition;

        /// <summary>Gets the configured maximum total player deck item count for this board.</summary>
        public int MaxDeckItemCount => _maxDeckBlockCount;

        /// <summary>Compatibility alias retained for previous POC scripts.</summary>
        public int MaxDeckBlockCount => _maxDeckBlockCount;

        /// <summary>Gets whether block-to-block swapping is enabled.</summary>
        public bool AllowBlockSwap => _allowBlockSwap;

        /// <summary>Gets whether deterministic placement seeding is enabled.</summary>
        public bool UseFixedRandomSeed => _useFixedRandomSeed;

        /// <summary>Gets deterministic placement seed.</summary>
        public int FixedRandomSeed => _fixedRandomSeed;

        /// <summary>
        /// Clamps settings to values that can produce a valid board.
        /// </summary>
        private void OnValidate()
        {
            _columns = Mathf.Max(2, _columns);
            _rows = Mathf.Max(2, _rows);
            _cellSize = Mathf.Max(0.1f, _cellSize);
            _cellGap = Mathf.Max(0f, _cellGap);
            _maxDeckBlockCount = Mathf.Max(0, _maxDeckBlockCount);
            _playerStartPosition.x = Mathf.Clamp(_playerStartPosition.x, 0, _columns - 1);
            _playerStartPosition.y = Mathf.Clamp(_playerStartPosition.y, 0, _rows - 1);
        }
    }
}
