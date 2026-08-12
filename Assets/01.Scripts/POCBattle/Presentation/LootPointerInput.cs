using PocBattle.Data;
using PocBattle.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Camera-local POC loot pointer reader. It tests a screen-space radius around the published loot anchor and emits only an event request.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class LootPointerInput : MonoBehaviour
    {
        [SerializeField, Tooltip("Shared event hub for run phase, loot snapshots, and click requests.")]
        private BattleEventChannelSO _eventChannel;

        [SerializeField, Tooltip("Loot height/click radius settings.")]
        private BattlePresentationSettingsSO _presentationSettings;

        [SerializeField, Tooltip("Same-GameObject camera cached in Awake/Reset.")]
        private Camera _camera;

        /// <summary>Current run phase.</summary>
        private RunPhase _runPhase;

        /// <summary>Current explicit loot interaction phase.</summary>
        private LootInteractionPhase _lootInteractionPhase;

        /// <summary>Current defeated-enemy world anchor for the visible loot.</summary>
        private Vector3 _lootAnchor;

        /// <summary>Caches same-GameObject Camera.</summary>
        private void Awake()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }
        }

        /// <summary>Subscribes to run/loot gating snapshots.</summary>
        private void OnEnable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.RunPhaseChanged += HandleRunPhaseChanged;
            _eventChannel.LootInteractionPhaseChanged += HandleLootInteractionPhaseChanged;
            _eventChannel.LootDropChanged += HandleLootDropChanged;
        }

        /// <summary>Removes run/loot gating subscriptions.</summary>
        private void OnDisable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.RunPhaseChanged -= HandleRunPhaseChanged;
            _eventChannel.LootInteractionPhaseChanged -= HandleLootInteractionPhaseChanged;
            _eventChannel.LootDropChanged -= HandleLootDropChanged;
        }

        /// <summary>Reads a one-frame left click only while the floating world loot is the active interaction target.</summary>
        private void Update()
        {
            if (_runPhase != RunPhase.Looting
                || _lootInteractionPhase != LootInteractionPhase.WaitingForWorldClick
                || Mouse.current == null
                || !Mouse.current.leftButton.wasPressedThisFrame
                || _camera == null
                || _presentationSettings == null)
            {
                return;
            }

            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            Vector2 guiPosition = new Vector2(pointerPosition.x, Screen.height - pointerPosition.y);
            if (RunOnGuiLayoutUtility.IsPointerOverPersistentGui(guiPosition))
            {
                return;
            }

            Vector3 clickAnchor = _lootAnchor + Vector3.up * _presentationSettings.LootHeightOffset;
            Vector3 screenPosition = _camera.WorldToScreenPoint(clickAnchor);
            if (screenPosition.z <= 0f)
            {
                return;
            }

            Vector2 delta = pointerPosition - new Vector2(screenPosition.x, screenPosition.y);
            float radius = _presentationSettings.LootClickRadiusPixels;
            if (delta.sqrMagnitude <= radius * radius)
            {
                _eventChannel?.RaiseLootWorldClickedRequested();
            }
        }

        /// <summary>Caches same-GameObject Camera for inspector-friendly setup.</summary>
        private void Reset()
        {
            _camera = GetComponent<Camera>();
        }

        /// <summary>Caches top-level run phase.</summary>
        private void HandleRunPhaseChanged(RunPhase phase)
        {
            _runPhase = phase;
        }

        /// <summary>Caches explicit looting interaction phase.</summary>
        private void HandleLootInteractionPhaseChanged(LootInteractionPhase phase)
        {
            _lootInteractionPhase = phase;
        }

        /// <summary>Caches visible loot anchor position.</summary>
        private void HandleLootDropChanged(LootDropSnapshot snapshot)
        {
            if (snapshot.IsVisible)
            {
                _lootAnchor = snapshot.WorldPosition;
            }
        }
    }
}
