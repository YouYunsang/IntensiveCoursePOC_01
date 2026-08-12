using PocBattle.Core;
using PocBattle.Data;
using PocBattle.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Explicit edit pointer states used instead of drag boolean combinations.
    /// </summary>
    internal enum EditPointerInteractionState
    {
        Idle = 0,
        HoldingPointer = 1
    }

    /// <summary>
    /// Camera-local edit pointer reader. Press begins a logical drag request, hold publishes world pointer movement,
    /// and release requests a drop/cancel without directly referencing board or block GameObjects.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BattlePointerInput : MonoBehaviour
    {
        [SerializeField, Tooltip("Shared event hub for drag requests, pointer presentation, and phase gating.")]
        private BattleEventChannelSO _eventChannel;

        [SerializeField, Tooltip("Board mapping data used to convert world pointer positions into logical coordinates.")]
        private BattleBoardSettingsSO _boardSettings;

        [SerializeField, Tooltip("Pointer ray distance and presentation tuning.")]
        private BattlePresentationSettingsSO _presentationSettings;

        [SerializeField, Tooltip("Physics layers considered only when identifying the block/cell pressed at drag start.")]
        private LayerMask _raycastMask = ~0;

        [SerializeField, Tooltip("Same-GameObject Camera cached in Awake/Reset.")]
        private Camera _camera;

        /// <summary>Current phase used to allow edit pointer input only during placement editing.</summary>
        private BattlePhase _currentPhase;

        /// <summary>Current battle result used only for shared IMGUI hit-region checks.</summary>
        private BattleResult _battleResult;

        /// <summary>Latest active enemy count used to block world input behind enemy OnGUI panels.</summary>
        private int _activeEnemyCount;

        /// <summary>Current press/hold interaction state.</summary>
        private EditPointerInteractionState _interactionState;

        /// <summary>
        /// Caches the same-GameObject Camera only; cross-GameObject references remain inspector/event based.
        /// </summary>
        private void Awake()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            _interactionState = EditPointerInteractionState.Idle;
        }

        /// <summary>
        /// Subscribes to phase/result changes through the shared event channel.
        /// </summary>
        private void OnEnable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.PhaseChanged += HandlePhaseChanged;
            _eventChannel.BattleResultChanged += HandleBattleResultChanged;
            _eventChannel.EnemyPartySetupRequested += HandleEnemyPartySetupRequested;
        }

        /// <summary>
        /// Removes phase/result event subscriptions and clears local pointer state.
        /// </summary>
        private void OnDisable()
        {
            if (_eventChannel != null)
            {
                _eventChannel.PhaseChanged -= HandlePhaseChanged;
                _eventChannel.BattleResultChanged -= HandleBattleResultChanged;
                _eventChannel.EnemyPartySetupRequested -= HandleEnemyPartySetupRequested;
            }

            _interactionState = EditPointerInteractionState.Idle;
        }

        /// <summary>
        /// Reads left-button press/hold/release in Update, as required by the project input rules.
        /// </summary>
        private void Update()
        {
            if (_currentPhase != BattlePhase.PlacementEdit || Mouse.current == null)
            {
                return;
            }

            if (_eventChannel == null || _boardSettings == null || _presentationSettings == null || _camera == null)
            {
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryBeginPointerHold();
            }

            if (_interactionState == EditPointerInteractionState.HoldingPointer && Mouse.current.leftButton.isPressed)
            {
                PublishHeldPointerWorldPosition();
            }

            if (_interactionState == EditPointerInteractionState.HoldingPointer && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                CompletePointerHold();
            }
        }

        /// <summary>
        /// Starts a hold only when the initial press is over board world geometry and not over visible OnGUI.
        /// Battle logic decides whether the pressed cell actually contains an editable block.
        /// </summary>
        private void TryBeginPointerHold()
        {
            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            if (IsPointerOverGui(pointerPosition))
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(pointerPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, _presentationSettings.PointerRayDistance, _raycastMask))
            {
                return;
            }

            if (!BoardCoordinateUtility.TryWorldToGrid(_boardSettings, hit.point, out Vector2Int coordinate))
            {
                return;
            }

            _interactionState = EditPointerInteractionState.HoldingPointer;
            _eventChannel.RaisePlacementDragBeginRequested(coordinate);
            PublishHeldPointerWorldPosition();
        }

        /// <summary>
        /// Publishes the pointer's XZ position on a stable board plane so the held block does not raycast against itself.
        /// </summary>
        private void PublishHeldPointerWorldPosition()
        {
            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            if (TryGetBoardPlaneWorldPosition(pointerPosition, out Vector3 worldPosition))
            {
                _eventChannel.RaiseBlockDragPointerMoved(worldPosition);
            }
        }

        /// <summary>
        /// Requests a logical drop for a valid board coordinate or cancellation for GUI/outside-board release.
        /// </summary>
        private void CompletePointerHold()
        {
            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            _interactionState = EditPointerInteractionState.Idle;

            if (IsPointerOverGui(pointerPosition)
                || !TryGetBoardPlaneWorldPosition(pointerPosition, out Vector3 worldPosition)
                || !BoardCoordinateUtility.TryWorldToGrid(_boardSettings, worldPosition, out Vector2Int coordinate))
            {
                _eventChannel.RaisePlacementDragCancelRequested();
                return;
            }

            _eventChannel.RaisePlacementDragDropRequested(coordinate);
        }

        /// <summary>
        /// Intersects the camera pointer ray with the board's horizontal presentation plane.
        /// </summary>
        private bool TryGetBoardPlaneWorldPosition(Vector2 pointerPosition, out Vector3 worldPosition)
        {
            Ray ray = _camera.ScreenPointToRay(pointerPosition);
            Plane boardPlane = new Plane(Vector3.up, new Vector3(0f, _presentationSettings.CellCenterY, 0f));
            if (!boardPlane.Raycast(ray, out float enter))
            {
                worldPosition = default;
                return false;
            }

            worldPosition = ray.GetPoint(enter);
            return true;
        }

        /// <summary>
        /// Returns true when the screen-space mouse position overlaps any currently interactive POC OnGUI region.
        /// </summary>
        private bool IsPointerOverGui(Vector2 pointerPosition)
        {
            Vector2 guiPointerPosition = new Vector2(pointerPosition.x, Screen.height - pointerPosition.y);
            return BattleOnGuiLayoutUtility.IsPointerOverVisibleGui(
                guiPointerPosition,
                _currentPhase,
                _battleResult,
                _activeEnemyCount);
        }

        /// <summary>
        /// Caches same-GameObject Camera for inspector-friendly setup.
        /// </summary>
        private void Reset()
        {
            _camera = GetComponent<Camera>();
        }

        /// <summary>
        /// Stores current phase and clears a local hold whenever edit phase is no longer active.
        /// PlacementEditState.Exit owns the authoritative visual cancellation.
        /// </summary>
        private void HandlePhaseChanged(BattlePhase phase)
        {
            _currentPhase = phase;
            if (phase != BattlePhase.PlacementEdit)
            {
                _interactionState = EditPointerInteractionState.Idle;
            }
        }

        /// <summary>
        /// Stores terminal result for shared IMGUI interactive-region checks.
        /// </summary>
        private void HandleBattleResultChanged(BattleResult result)
        {
            _battleResult = result;
        }

        /// <summary>
        /// Stores current encounter enemy count so pointer input can block clicks behind every visible enemy OnGUI panel.
        /// </summary>
        private void HandleEnemyPartySetupRequested(System.Collections.Generic.IReadOnlyList<EnemySetupSnapshot> snapshots)
        {
            _activeEnemyCount = snapshots != null ? snapshots.Count : 0;
        }
    }
}
