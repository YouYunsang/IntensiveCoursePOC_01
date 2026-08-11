using PocBattle.Core;
using PocBattle.Data;
using PocBattle.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Camera-local edit pointer reader that processes mouse input in Update and emits only logical board coordinates.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BattlePointerInput : MonoBehaviour
    {
        [SerializeField, Tooltip("Shared event hub for coordinate clicks and phase gating.")]
        private BattleEventChannelSO _eventChannel;

        [SerializeField, Tooltip("Board mapping data used to convert raycast hit points into logical coordinates.")]
        private BattleBoardSettingsSO _boardSettings;

        [SerializeField, Tooltip("Pointer ray distance and other presentation tuning.")]
        private BattlePresentationSettingsSO _presentationSettings;

        [SerializeField, Tooltip("Physics layers considered by board edit raycasts.")]
        private LayerMask _raycastMask = ~0;

        [SerializeField, Tooltip("Same-GameObject Camera cached in Awake/Reset.")]
        private Camera _camera;

        /// <summary>Current phase used to allow mouse board clicks only during placement editing.</summary>
        private BattlePhase _currentPhase;

        /// <summary>
        /// Subscribes to phase changes through the shared event channel.
        /// </summary>
        private void OnEnable()
        {
            if (_eventChannel != null)
            {
                _eventChannel.PhaseChanged += HandlePhaseChanged;
            }
        }

        /// <summary>
        /// Removes phase event subscription.
        /// </summary>
        private void OnDisable()
        {
            if (_eventChannel != null)
            {
                _eventChannel.PhaseChanged -= HandlePhaseChanged;
            }
        }

        /// <summary>
        /// Reads one-frame left mouse clicks, raycasts world geometry, and emits a logical board coordinate.
        /// </summary>
        private void Update()
        {
            if (_currentPhase != BattlePhase.PlacementEdit || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(pointerPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, _presentationSettings.PointerRayDistance, _raycastMask))
            {
                return;
            }

            if (BoardCoordinateUtility.TryWorldToGrid(_boardSettings, hit.point, out Vector2Int coordinate))
            {
                _eventChannel.RaiseBoardCellClicked(coordinate);
            }
        }

        /// <summary>
        /// Caches same-GameObject Camera for inspector-friendly setup.
        /// </summary>
        private void Reset()
        {
            _camera = GetComponent<Camera>();
        }

        /// <summary>
        /// Stores current phase for Update input gating.
        /// </summary>
        private void HandlePhaseChanged(BattlePhase phase)
        {
            _currentPhase = phase;
        }
    }
}
