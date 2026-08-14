using System.Collections.Generic;
using DG.Tweening;
using PocBattle.Core;
using PocBattle.Data;
using PocBattle.Runtime;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>Dedicated collider-free board cell-effect layer with reusable views and edit-drag presentation.</summary>
    public sealed class CellEffectLayerView : MonoBehaviour
    {
        private const int NO_DRAGGED_EFFECT_ID = -1;

        [SerializeField, Tooltip("Board dimensions used to map logical effect coordinates to world positions.")]
        private BattleBoardSettingsSO _boardSettings;

        [SerializeField, Tooltip("Cell-effect size, height, edit, and trigger feedback tuning.")]
        private BattlePresentationSettingsSO _presentationSettings;

        [SerializeField, Tooltip("Shared event hub used instead of direct battle-state references.")]
        private BattleEventChannelSO _eventChannel;

        [SerializeField, Tooltip("Collider-free generic cell-effect sprite prefab.")]
        private CellEffectView _cellEffectPrefab;

        private Dictionary<int, CellEffectView> _effectViews;
        private HashSet<int> _activeLayoutIds;
        private int _draggedEffectId;
        private Tween _layoutCompletionTween;

        /// <summary>Allocates reusable lookup collections before the first battle layout.</summary>
        private void Awake()
        {
            _effectViews = new Dictionary<int, CellEffectView>();
            _activeLayoutIds = new HashSet<int>();
            _draggedEffectId = NO_DRAGGED_EFFECT_ID;
        }

        /// <summary>Subscribes only to immutable cell-effect presentation events.</summary>
        private void OnEnable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.CellEffectLayoutChanged += HandleCellEffectLayoutChanged;
            _eventChannel.CellEffectDragVisualStarted += HandleCellEffectDragVisualStarted;
            _eventChannel.CellEffectDragPointerMoved += HandleCellEffectDragPointerMoved;
            _eventChannel.CellEffectMoveUsageResetRequested += HandleMoveUsageResetRequested;
            _eventChannel.CellEffectTriggeredVisualRequested += HandleTriggeredVisualRequested;
        }

        /// <summary>Removes all cell-effect presentation subscriptions.</summary>
        private void OnDisable()
        {
            if (_eventChannel != null)
            {
                _eventChannel.CellEffectLayoutChanged -= HandleCellEffectLayoutChanged;
                _eventChannel.CellEffectDragVisualStarted -= HandleCellEffectDragVisualStarted;
                _eventChannel.CellEffectDragPointerMoved -= HandleCellEffectDragPointerMoved;
                _eventChannel.CellEffectMoveUsageResetRequested -= HandleMoveUsageResetRequested;
                _eventChannel.CellEffectTriggeredVisualRequested -= HandleTriggeredVisualRequested;
            }

            _draggedEffectId = NO_DRAGGED_EFFECT_ID;
            KillLayoutCompletionTween();
        }

        /// <summary>Synchronizes the complete cell-effect layer, using shared + per-definition visual offsets.</summary>
        private void HandleCellEffectLayoutChanged(IReadOnlyList<CellEffectSnapshot> snapshots, bool animate)
        {
            _activeLayoutIds.Clear();
            float surfaceY = _presentationSettings.CellCenterY
                             + _presentationSettings.CellHeight * 0.5f
                             + _presentationSettings.CellEffectSurfaceOffset;
            float worldSize = _boardSettings.CellSize * _presentationSettings.CellEffectFootprintRatio;

            for (int snapshotIndex = 0; snapshotIndex < snapshots.Count; snapshotIndex++)
            {
                CellEffectSnapshot snapshot = snapshots[snapshotIndex];
                _activeLayoutIds.Add(snapshot.EffectId);
                bool createdNewView = !_effectViews.TryGetValue(snapshot.EffectId, out CellEffectView effectView);
                if (createdNewView)
                {
                    effectView = Instantiate(_cellEffectPrefab, transform);
                    _effectViews.Add(snapshot.EffectId, effectView);
                }

                effectView.name = $"CellEffect_{snapshot.EffectId}_{snapshot.Definition.DisplayName}";
                effectView.gameObject.SetActive(true);
                effectView.Configure(snapshot.Definition, snapshot.Direction, worldSize);
                Vector3 worldPosition = BoardCoordinateUtility.GridToWorld(_boardSettings, snapshot.Coordinate, surfaceY)
                                        + snapshot.Definition.BoardVisualOffset;

                if (effectView.IsDragging)
                {
                    effectView.DropTo(worldPosition, _presentationSettings.CellEffectDropDuration);
                }
                else if (animate && !createdNewView)
                {
                    effectView.MoveTo(worldPosition, _presentationSettings.CellEffectDropDuration);
                }
                else
                {
                    effectView.SnapTo(worldPosition);
                }
            }

            foreach (KeyValuePair<int, CellEffectView> pair in _effectViews)
            {
                if (!_activeLayoutIds.Contains(pair.Key))
                {
                    pair.Value.gameObject.SetActive(false);
                }
            }

            _draggedEffectId = NO_DRAGGED_EFFECT_ID;
            ScheduleLayoutCompletion(animate);
        }

        /// <summary>Begins the lift pose for the runtime cell effect accepted by edit logic.</summary>
        private void HandleCellEffectDragVisualStarted(int effectId)
        {
            if (!_effectViews.TryGetValue(effectId, out CellEffectView effectView) || !effectView.gameObject.activeSelf)
            {
                return;
            }

            _draggedEffectId = effectId;
            effectView.BeginDrag(
                _presentationSettings.CellEffectDragLiftHeight,
                _presentationSettings.CellEffectDragScale,
                _presentationSettings.CellEffectDragLiftDuration);
        }

        /// <summary>Moves only the accepted held effect with the pointer XZ position.</summary>
        private void HandleCellEffectDragPointerMoved(Vector3 worldPosition)
        {
            if (_draggedEffectId == NO_DRAGGED_EFFECT_ID
                || !_effectViews.TryGetValue(_draggedEffectId, out CellEffectView effectView))
            {
                return;
            }

            effectView.FollowDrag(worldPosition);
        }

        /// <summary>Reactivates all current effect sprites for a fresh manual movement input.</summary>
        private void HandleMoveUsageResetRequested()
        {
            foreach (KeyValuePair<int, CellEffectView> pair in _effectViews)
            {
                if (pair.Value.gameObject.activeSelf)
                {
                    pair.Value.SetMoveActive();
                }
            }
        }

        /// <summary>Dims exactly the effect that triggered during the current manual move.</summary>
        private void HandleTriggeredVisualRequested(int effectId)
        {
            if (!_effectViews.TryGetValue(effectId, out CellEffectView effectView) || !effectView.gameObject.activeSelf)
            {
                return;
            }

            effectView.PlayTriggered(
                _presentationSettings.CellEffectTriggerPulseScale,
                _presentationSettings.CellEffectTriggerPulseDuration,
                _presentationSettings.CellEffectInactiveAlpha);
        }

        /// <summary>Publishes one completion after cell-effect drop/move presentation settles.</summary>
        private void ScheduleLayoutCompletion(bool animate)
        {
            KillLayoutCompletionTween();
            if (!animate)
            {
                return;
            }

            float delay = _presentationSettings.CellEffectDropDuration;
            if (delay <= 0f)
            {
                _eventChannel.RaiseCellEffectLayoutVisualCompleted();
                return;
            }

            _layoutCompletionTween = DOVirtual.DelayedCall(delay, () =>
            {
                _layoutCompletionTween = null;
                _eventChannel.RaiseCellEffectLayoutVisualCompleted();
            });
        }

        /// <summary>Kills a superseded cell-effect layout completion callback.</summary>
        private void KillLayoutCompletionTween()
        {
            if (_layoutCompletionTween != null && _layoutCompletionTween.IsActive())
            {
                _layoutCompletionTween.Kill();
            }
            _layoutCompletionTween = null;
        }
    }
}
