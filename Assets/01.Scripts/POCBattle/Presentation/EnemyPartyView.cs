using System.Collections.Generic;
using PocBattle.Data;
using PocBattle.Runtime;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Creates/reuses world enemy sprite views and publishes their anchors without exposing GameObject references to run logic.
    /// </summary>
    public sealed class EnemyPartyView : MonoBehaviour
    {
        [SerializeField, Tooltip("Shared battle/run event hub.")]
        private BattleEventChannelSO _eventChannel;

        [SerializeField, Tooltip("World-space enemy placement, hit, and death feedback settings.")]
        private BattlePresentationSettingsSO _presentationSettings;

        [SerializeField, Tooltip("Generic 2D enemy sprite prefab reused for all POC enemy definitions.")]
        private EnemyView _enemyViewPrefab;

        /// <summary>Stable party index to reusable enemy view lookup.</summary>
        private Dictionary<int, EnemyView> _enemyViews;

        /// <summary>Reusable set of enemy indices present in the latest encounter setup.</summary>
        private HashSet<int> _activeEnemyIndices;

        /// <summary>Allocates reusable lookup containers once.</summary>
        private void Awake()
        {
            _enemyViews = new Dictionary<int, EnemyView>();
            _activeEnemyIndices = new HashSet<int>();
        }

        /// <summary>Subscribes only to world-view setup, hit, and death presentation events.</summary>
        private void OnEnable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.EnemyPartySetupRequested += HandleEnemyPartySetupRequested;
            _eventChannel.EnemyHitVisualRequested += HandleEnemyHitVisualRequested;
            _eventChannel.EnemyDeathVisualRequested += HandleEnemyDeathVisualRequested;
        }

        /// <summary>Removes all enemy world-view event subscriptions.</summary>
        private void OnDisable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.EnemyPartySetupRequested -= HandleEnemyPartySetupRequested;
            _eventChannel.EnemyHitVisualRequested -= HandleEnemyHitVisualRequested;
            _eventChannel.EnemyDeathVisualRequested -= HandleEnemyDeathVisualRequested;
        }

        /// <summary>
        /// Creates/reuses one sprite view per encounter enemy and publishes each world anchor after positioning.
        /// </summary>
        private void HandleEnemyPartySetupRequested(IReadOnlyList<EnemySetupSnapshot> snapshots)
        {
            if (snapshots == null)
            {
                return;
            }

            if (_enemyViewPrefab == null || _presentationSettings == null)
            {
                Debug.LogError("EnemyPartyView cannot build enemy views because prefab or presentation settings are missing.", this);
                return;
            }

            _activeEnemyIndices.Clear();

            for (int snapshotIndex = 0; snapshotIndex < snapshots.Count; snapshotIndex++)
            {
                EnemySetupSnapshot snapshot = snapshots[snapshotIndex];
                _activeEnemyIndices.Add(snapshot.EnemyIndex);

                if (!_enemyViews.TryGetValue(snapshot.EnemyIndex, out EnemyView enemyView))
                {
                    enemyView = Instantiate(_enemyViewPrefab, transform);
                    _enemyViews.Add(snapshot.EnemyIndex, enemyView);
                }

                enemyView.gameObject.SetActive(true);
                enemyView.name = $"Enemy_{snapshot.EnemyIndex}_{snapshot.DisplayName}";
                Vector3 worldPosition = _presentationSettings.EnemyStartPosition
                                        + Vector3.right * (_presentationSettings.EnemySpacing * snapshot.EnemyIndex);
                enemyView.transform.position = worldPosition;
                enemyView.transform.rotation = Quaternion.Euler(_presentationSettings.EnemyEulerAngles);
                enemyView.transform.localScale = Vector3.one * _presentationSettings.EnemyScale;
                enemyView.Configure(snapshot);
                _eventChannel.RaiseEnemyWorldAnchorChanged(snapshot.EnemyIndex, worldPosition);
            }

            foreach (KeyValuePair<int, EnemyView> enemyPair in _enemyViews)
            {
                if (!_activeEnemyIndices.Contains(enemyPair.Key))
                {
                    enemyPair.Value.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>Plays hit feedback on the enemy selected by stable party index.</summary>
        private void HandleEnemyHitVisualRequested(int enemyIndex)
        {
            if (_presentationSettings == null)
            {
                return;
            }

            if (_enemyViews.TryGetValue(enemyIndex, out EnemyView enemyView) && enemyView.gameObject.activeSelf)
            {
                enemyView.PlayHit(
                    _presentationSettings.EnemyHitDuration,
                    _presentationSettings.EnemyHitStrength,
                    _presentationSettings.HitPunchVibrato,
                    _presentationSettings.HitPunchElasticity);
            }
        }

        /// <summary>
        /// Plays defeated enemy presentation. Run logic already cached this enemy's anchor through setup events.
        /// </summary>
        private void HandleEnemyDeathVisualRequested(int enemyIndex)
        {
            if (_presentationSettings == null)
            {
                return;
            }

            if (_enemyViews.TryGetValue(enemyIndex, out EnemyView enemyView) && enemyView.gameObject.activeSelf)
            {
                enemyView.PlayDeath(_presentationSettings.EnemyDeathDuration, null);
            }
        }
    }
}
