using System.Collections.Generic;
using PocBattle.Core;
using PocBattle.Data;
using PocBattle.Runtime;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Dedicated board cell-effect layer. It reuses sprite views and never participates in block edit raycasts.
    /// </summary>
    public sealed class CellEffectLayerView : MonoBehaviour
    {
        [SerializeField, Tooltip("Board dimensions used to map logical effect coordinates to world positions.")]
        private BattleBoardSettingsSO _boardSettings;

        [SerializeField, Tooltip("Cell-effect size, height, and trigger feedback tuning.")]
        private BattlePresentationSettingsSO _presentationSettings;

        [SerializeField, Tooltip("Shared event hub used instead of direct battle-state references.")]
        private BattleEventChannelSO _eventChannel;

        [SerializeField, Tooltip("Collider-free generic cell-effect sprite prefab.")]
        private CellEffectView _cellEffectPrefab;

        /// <summary>Stable runtime id to reusable effect view.</summary>
        private Dictionary<int, CellEffectView> _effectViews;

        /// <summary>Reusable latest-layout id set.</summary>
        private HashSet<int> _activeLayoutIds;

        /// <summary>Allocates reusable lookup collections before battle Start publishes the first layout.</summary>
        private void Awake()
        {
            _effectViews = new Dictionary<int, CellEffectView>();
            _activeLayoutIds = new HashSet<int>();
        }

        /// <summary>Subscribes only to immutable cell-effect presentation events.</summary>
        private void OnEnable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.CellEffectLayoutChanged += HandleCellEffectLayoutChanged;
            _eventChannel.CellEffectMoveUsageResetRequested += HandleMoveUsageResetRequested;
            _eventChannel.CellEffectTriggeredVisualRequested += HandleTriggeredVisualRequested;
        }

        /// <summary>Removes all cell-effect presentation subscriptions.</summary>
        private void OnDisable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.CellEffectLayoutChanged -= HandleCellEffectLayoutChanged;
            _eventChannel.CellEffectMoveUsageResetRequested -= HandleMoveUsageResetRequested;
            _eventChannel.CellEffectTriggeredVisualRequested -= HandleTriggeredVisualRequested;
        }

        /// <summary>Synchronizes the complete immutable-per-turn cell-effect layer.</summary>
        private void HandleCellEffectLayoutChanged(IReadOnlyList<CellEffectSnapshot> snapshots)
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
                if (!_effectViews.TryGetValue(snapshot.EffectId, out CellEffectView effectView))
                {
                    effectView = Instantiate(_cellEffectPrefab, transform);
                    _effectViews.Add(snapshot.EffectId, effectView);
                }

                effectView.name = $"CellEffect_{snapshot.EffectId}_{snapshot.Definition.DisplayName}";
                effectView.gameObject.SetActive(true);
                Vector3 worldPosition = BoardCoordinateUtility.GridToWorld(_boardSettings, snapshot.Coordinate, surfaceY);
                effectView.Configure(snapshot.Definition, snapshot.Direction, worldPosition, worldSize);
            }

            foreach (KeyValuePair<int, CellEffectView> pair in _effectViews)
            {
                if (!_activeLayoutIds.Contains(pair.Key))
                {
                    pair.Value.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>Reactivates all current effect sprites for a fresh single manual movement input.</summary>
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
    }
}
