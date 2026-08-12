using PocBattle.Core;
using PocBattle.Data;
using PocBattle.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Representative player component that reads keyboard input in Update and coordinates the same-GameObject PlayerMovement.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        /// <summary>Minimum cell count used only to protect animation duration from zero.</summary>
        private const int MIN_TRAVELED_CELL_COUNT = 1;

        [SerializeField, Tooltip("Shared event hub used for all cross-GameObject communication.")]
        private BattleEventChannelSO _eventChannel;

        [SerializeField, Tooltip("Board coordinate settings used to map logical coordinates to world positions.")]
        private BattleBoardSettingsSO _boardSettings;

        [SerializeField, Tooltip("Player movement and feedback presentation timing.")]
        private BattlePresentationSettingsSO _presentationSettings;

        [SerializeField, Tooltip("Same-GameObject movement component cached for transform animation only.")]
        private PlayerMovement _movement;

        /// <summary>Current high-level battle phase received through the event channel.</summary>
        private BattlePhase _currentPhase;

        /// <summary>Current top-level run phase used to hard-disable battle input during loot/fade/result states.</summary>
        private RunPhase _currentRunPhase;

        /// <summary>
        /// Subscribes to player-specific presentation events and phase state.
        /// </summary>
        private void OnEnable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.PhaseChanged += HandlePhaseChanged;
            _eventChannel.RunPhaseChanged += HandleRunPhaseChanged;
            _eventChannel.PlayerPositionSyncRequested += HandlePlayerPositionSyncRequested;
            _eventChannel.PlayerMoveVisualRequested += HandlePlayerMoveVisualRequested;
            _eventChannel.PlayerHitVisualRequested += HandlePlayerHitVisualRequested;
        }

        /// <summary>
        /// Removes player event subscriptions.
        /// </summary>
        private void OnDisable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.PhaseChanged -= HandlePhaseChanged;
            _eventChannel.RunPhaseChanged -= HandleRunPhaseChanged;
            _eventChannel.PlayerPositionSyncRequested -= HandlePlayerPositionSyncRequested;
            _eventChannel.PlayerMoveVisualRequested -= HandlePlayerMoveVisualRequested;
            _eventChannel.PlayerHitVisualRequested -= HandlePlayerHitVisualRequested;
        }

        /// <summary>
        /// Reads WASD/arrow key presses only during movement phase and raises a single cardinal request per frame.
        /// </summary>
        private void Update()
        {
            if ((_currentRunPhase != RunPhase.None && _currentRunPhase != RunPhase.Battle)
                || _currentPhase != BattlePhase.Movement
                || _movement == null
                || _movement.IsAnimating)
            {
                return;
            }

            if (TryReadDirection(out Vector2Int direction))
            {
                _eventChannel.RaiseMoveInputRequested(direction);
            }
        }

        /// <summary>
        /// Caches the same-GameObject PlayerMovement in the editor for inspector-friendly prefab wiring.
        /// </summary>
        private void Reset()
        {
            _movement = GetComponent<PlayerMovement>();
        }

        /// <summary>
        /// Stores the currently active battle phase for Update input gating.
        /// </summary>
        private void HandlePhaseChanged(BattlePhase phase)
        {
            _currentPhase = phase;
        }

        /// <summary>Stores the current run phase so movement input cannot leak into looting or stage transitions.</summary>
        private void HandleRunPhaseChanged(RunPhase phase)
        {
            _currentRunPhase = phase;
        }

        /// <summary>
        /// Snaps player view to persistent logical grid position, used on battle start/restart and turn setup.
        /// </summary>
        private void HandlePlayerPositionSyncRequested(Vector2Int coordinate)
        {
            Vector3 worldPosition = BoardCoordinateUtility.GridToWorld(
                _boardSettings,
                coordinate,
                _presentationSettings.PlayerCenterY);
            _movement.SnapTo(worldPosition);
        }

        /// <summary>
        /// Converts a logical move command into either a slide tween or blocked-direction punch feedback.
        /// </summary>
        private void HandlePlayerMoveVisualRequested(PlayerMoveVisualCommand command)
        {
            if (command.IsValid)
            {
                Vector3 destination = BoardCoordinateUtility.GridToWorld(
                    _boardSettings,
                    command.Destination,
                    _presentationSettings.PlayerCenterY);
                int traveledCells = Mathf.Abs(command.Destination.x - command.StartPosition.x)
                                    + Mathf.Abs(command.Destination.y - command.StartPosition.y);
                float duration = Mathf.Max(MIN_TRAVELED_CELL_COUNT, traveledCells) * _presentationSettings.MoveSecondsPerCell;
                _movement.PlaySlide(destination, duration, _eventChannel.RaisePlayerMoveVisualCompleted);
                return;
            }

            Vector3 worldDirection = new Vector3(command.Direction.x, 0f, command.Direction.y).normalized;
            Vector3 offset = worldDirection * _presentationSettings.InvalidMoveDistance;
            _movement.PlayInvalidMove(offset, _presentationSettings.InvalidMoveDuration, _eventChannel.RaisePlayerMoveVisualCompleted);
        }

        /// <summary>
        /// Plays player hit feedback when trap or enemy damage reduces HP.
        /// </summary>
        private void HandlePlayerHitVisualRequested()
        {
            _movement.PlayHit(
                _presentationSettings.EnemyHitDuration,
                _presentationSettings.PlayerHitStrength,
                _presentationSettings.HitPunchVibrato,
                _presentationSettings.HitPunchElasticity);
        }

        /// <summary>
        /// Reads one-frame WASD or arrow presses as exactly one cardinal grid direction.
        /// </summary>
        private static bool TryReadDirection(out Vector2Int direction)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                direction = Vector2Int.zero;
                return false;
            }

            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            {
                direction = Vector2Int.up;
                return true;
            }

            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                direction = Vector2Int.down;
                return true;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                direction = Vector2Int.left;
                return true;
            }

            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                direction = Vector2Int.right;
                return true;
            }

            direction = Vector2Int.zero;
            return false;
        }
    }
}
