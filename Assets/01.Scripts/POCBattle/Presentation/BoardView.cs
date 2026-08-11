using System.Collections.Generic;
using PocBattle.Core;
using PocBattle.Data;
using PocBattle.Runtime;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// World-space 3D board view that builds static cells and reuses block views across turns.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        /// <summary>Sentinel id used when no editable block is selected.</summary>
        private const int NO_SELECTED_BLOCK_ID = -1;

        [SerializeField, Tooltip("Board gameplay dimensions and centered coordinate mapping.")]
        private BattleBoardSettingsSO _boardSettings;

        [SerializeField, Tooltip("Board/block presentation dimensions and tween timing.")]
        private BattlePresentationSettingsSO _presentationSettings;

        [SerializeField, Tooltip("Shared event hub used instead of direct BattleController references.")]
        private BattleEventChannelSO _eventChannel;

        [SerializeField, Tooltip("Generic 3D cell prefab containing BoardCellView and a collider.")]
        private BoardCellView _cellPrefab;

        [SerializeField, Tooltip("Generic 3D block prefab containing BlockView and a collider.")]
        private BlockView _blockPrefab;

        /// <summary>Stable block id to reusable block view lookup.</summary>
        private Dictionary<int, BlockView> _blockViews;

        /// <summary>Reusable set of block ids present in the latest layout snapshot.</summary>
        private HashSet<int> _activeLayoutIds;

        /// <summary>Currently highlighted block id.</summary>
        private int _selectedBlockId;

        /// <summary>
        /// Builds static cells before BattleController.Start publishes its first runtime block layout.
        /// </summary>
        private void Awake()
        {
            _blockViews = new Dictionary<int, BlockView>();
            _activeLayoutIds = new HashSet<int>();
            _selectedBlockId = NO_SELECTED_BLOCK_ID;
            BuildStaticGrid();
        }

        /// <summary>
        /// Subscribes to block layout and selection presentation events.
        /// </summary>
        private void OnEnable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.BlockLayoutChanged += HandleBlockLayoutChanged;
            _eventChannel.BlockSelectionChanged += HandleBlockSelectionChanged;
        }

        /// <summary>
        /// Removes board presentation event subscriptions.
        /// </summary>
        private void OnDisable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.BlockLayoutChanged -= HandleBlockLayoutChanged;
            _eventChannel.BlockSelectionChanged -= HandleBlockSelectionChanged;
        }

        /// <summary>
        /// Instantiates one visible world-space cell per configured grid coordinate and marks exactly four corners as walls.
        /// </summary>
        private void BuildStaticGrid()
        {
            if (_boardSettings == null || _presentationSettings == null || _cellPrefab == null)
            {
                return;
            }

            for (int row = 0; row < _boardSettings.Rows; row++)
            {
                for (int column = 0; column < _boardSettings.Columns; column++)
                {
                    Vector2Int coordinate = new Vector2Int(column, row);
                    bool isCornerWall = IsCornerWall(coordinate);
                    float height = isCornerWall ? _presentationSettings.WallHeight : _presentationSettings.CellHeight;
                    float centerY = _presentationSettings.CellCenterY + (height - _presentationSettings.CellHeight) * 0.5f;
                    Vector3 position = BoardCoordinateUtility.GridToWorld(_boardSettings, coordinate, centerY);
                    Vector3 scale = new Vector3(_boardSettings.CellSize, height, _boardSettings.CellSize);
                    Color color = isCornerWall ? _presentationSettings.WallCellColor : _presentationSettings.NormalCellColor;
                    BoardCellView cellView = Instantiate(_cellPrefab, transform);
                    cellView.name = isCornerWall ? $"Wall_{column}_{row}" : $"Cell_{column}_{row}";
                    cellView.Configure(position, scale, color);
                }
            }
        }

        /// <summary>
        /// Synchronizes every runtime block to a reusable world view, tweening only already-existing views when requested.
        /// </summary>
        private void HandleBlockLayoutChanged(IReadOnlyList<BlockSnapshot> snapshots, bool animate)
        {
            _activeLayoutIds.Clear();

            for (int snapshotIndex = 0; snapshotIndex < snapshots.Count; snapshotIndex++)
            {
                BlockSnapshot snapshot = snapshots[snapshotIndex];
                _activeLayoutIds.Add(snapshot.BlockId);
                bool createdNewView = !_blockViews.TryGetValue(snapshot.BlockId, out BlockView blockView);

                if (createdNewView)
                {
                    blockView = Instantiate(_blockPrefab, transform);
                    blockView.name = $"Block_{snapshot.BlockId}_{snapshot.Definition.DisplayName}";
                    _blockViews.Add(snapshot.BlockId, blockView);
                }

                blockView.gameObject.SetActive(true);
                Vector3 baseScale = new Vector3(
                    _boardSettings.CellSize * _presentationSettings.BlockFootprintRatio,
                    _presentationSettings.BlockHeight,
                    _boardSettings.CellSize * _presentationSettings.BlockFootprintRatio);
                blockView.Configure(snapshot.Definition, baseScale);
                Vector3 position = BoardCoordinateUtility.GridToWorld(
                    _boardSettings,
                    snapshot.Coordinate,
                    _presentationSettings.BlockCenterY);

                if (animate && !createdNewView)
                {
                    blockView.MoveTo(position, _presentationSettings.BlockMoveDuration);
                }
                else
                {
                    blockView.SnapTo(position);
                }
            }

            foreach (KeyValuePair<int, BlockView> blockPair in _blockViews)
            {
                if (!_activeLayoutIds.Contains(blockPair.Key))
                {
                    blockPair.Value.gameObject.SetActive(false);
                }
            }

            ApplySelectionHighlight();
        }

        /// <summary>
        /// Stores selection id and updates only visual scale highlights.
        /// </summary>
        private void HandleBlockSelectionChanged(int blockId)
        {
            _selectedBlockId = blockId;
            ApplySelectionHighlight();
        }

        /// <summary>
        /// Applies selected/unselected scale to all active reusable block views.
        /// </summary>
        private void ApplySelectionHighlight()
        {
            foreach (KeyValuePair<int, BlockView> blockPair in _blockViews)
            {
                if (blockPair.Value.gameObject.activeSelf)
                {
                    blockPair.Value.SetSelected(
                        blockPair.Key == _selectedBlockId,
                        _presentationSettings.SelectedBlockScale,
                        _presentationSettings.BlockMoveDuration);
                }
            }
        }

        /// <summary>
        /// Returns true only for the four corner coordinates, never for an entire outer edge.
        /// </summary>
        private bool IsCornerWall(Vector2Int coordinate)
        {
            bool onHorizontalEdge = coordinate.x == 0 || coordinate.x == _boardSettings.Columns - 1;
            bool onVerticalEdge = coordinate.y == 0 || coordinate.y == _boardSettings.Rows - 1;
            return onHorizontalEdge && onVerticalEdge;
        }
    }
}
