using System;
using System.Collections.Generic;
using PocBattle.Core;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Explicit battle phase state machine that owns only one current state at a time.
    /// </summary>
    public sealed class BattleStateMachine
    {
        /// <summary>Registered state instance by explicit battle phase.</summary>
        private readonly Dictionary<BattlePhase, BattleStateBase> _states;

        /// <summary>Callback used to publish high-level phase changes.</summary>
        private readonly Action<BattlePhase> _phaseChanged;

        /// <summary>Currently active state object.</summary>
        private BattleStateBase _currentState;

        /// <summary>Currently active phase enum.</summary>
        private BattlePhase _currentPhase;

        /// <summary>Gets current state for input forwarding.</summary>
        public BattleStateBase CurrentState => _currentState;

        /// <summary>Gets current explicit battle phase.</summary>
        public BattlePhase CurrentPhase => _currentPhase;

        /// <summary>
        /// Creates an empty state machine with a phase-change callback.
        /// </summary>
        public BattleStateMachine(Action<BattlePhase> phaseChanged)
        {
            _states = new Dictionary<BattlePhase, BattleStateBase>();
            _phaseChanged = phaseChanged;
            _currentPhase = BattlePhase.None;
        }

        /// <summary>
        /// Registers one reusable state object for a battle phase.
        /// </summary>
        public void Register(BattlePhase phase, BattleStateBase state)
        {
            _states[phase] = state;
        }

        /// <summary>
        /// Exits the previous state, publishes phase change, and enters the next state.
        /// </summary>
        public void ChangeState(BattlePhase phase)
        {
            if (!_states.TryGetValue(phase, out BattleStateBase nextState))
            {
                throw new InvalidOperationException($"No battle state registered for phase {phase}.");
            }

            _currentState?.Exit();
            _currentState = nextState;
            _currentPhase = phase;
            _phaseChanged?.Invoke(phase);
            _currentState.Enter();
        }
    }
}
