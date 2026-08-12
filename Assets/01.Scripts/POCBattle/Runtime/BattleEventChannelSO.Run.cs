using System;
using UnityEngine;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Run/stage extensions for the shared POC event hub. Kept in a partial file so battle-only events remain readable.
    /// </summary>
    public sealed partial class BattleEventChannelSO
    {
        /// <summary>Raised whenever the high-level run phase changes.</summary>
        public event Action<RunPhase> RunPhaseChanged;

        /// <summary>Raised whenever stage, gold, or runtime deck capacity information changes.</summary>
        public event Action<RunStatusSnapshot> RunStatusChanged;

        /// <summary>Raised whenever the looting modal interaction substate changes.</summary>
        public event Action<LootInteractionPhase> LootInteractionPhaseChanged;

        /// <summary>Raised whenever the floating post-battle loot should appear, update, or disappear.</summary>
        public event Action<LootDropSnapshot> LootDropChanged;

        /// <summary>Raised with current physical deck rows while selecting discard candidates.</summary>
        public event Action<DeckDiscardSelectionSnapshot> DeckDiscardSelectionChanged;

        /// <summary>Raised by LootPointerInput when the floating loot block is left-clicked.</summary>
        public event Action LootWorldClickedRequested;

        /// <summary>Raised by OnGUI when the player accepts or declines the offered block.</summary>
        public event Action<bool> LootAcquireDecisionRequested;

        /// <summary>Raised by OnGUI when a full deck should enter discard-selection or decline the reward.</summary>
        public event Action<bool> LootReplaceDecisionRequested;

        /// <summary>Raised by OnGUI to toggle one exact runtime deck block for discard.</summary>
        public event Action<int> DeckDiscardToggleRequested;

        /// <summary>Raised by OnGUI to confirm selected runtime deck discards and take the reward.</summary>
        public event Action DeckDiscardConfirmRequested;

        /// <summary>Raised by OnGUI to leave discard selection and return to the full-deck decision.</summary>
        public event Action DeckDiscardCancelRequested;

        /// <summary>Raised when the run flow requests fade-out or fade-in presentation.</summary>
        public event Action<StageFadeDirection> StageFadeRequested;

        /// <summary>Raised by the OnGUI fade presenter when the requested fade has completed.</summary>
        public event Action<StageFadeDirection> StageFadeVisualCompleted;

        /// <summary>Raised by defeat OnGUI. True restarts the full run; false exits the application.</summary>
        public event Action<bool> RunRestartDecisionRequested;

        /// <summary>Raised by run-complete OnGUI. True starts a fresh run; false exits the application.</summary>
        public event Action<bool> RunCompleteDecisionRequested;

        /// <summary>Raised exactly once when a living enemy becomes defeated, including its gold reward.</summary>
        public event Action<int, int> EnemyDefeated;

        /// <summary>Raised when a defeated enemy should play its world death animation.</summary>
        public event Action<int> EnemyDeathVisualRequested;

        /// <summary>Raised by EnemyPartyView whenever an enemy world anchor is positioned for the current stage.</summary>
        public event Action<int, Vector3> EnemyWorldAnchorChanged;

        /// <summary>Publishes a high-level run phase.</summary>
        public void RaiseRunPhaseChanged(RunPhase phase) => RunPhaseChanged?.Invoke(phase);

        /// <summary>Publishes persistent run HUD values.</summary>
        public void RaiseRunStatusChanged(RunStatusSnapshot snapshot) => RunStatusChanged?.Invoke(snapshot);

        /// <summary>Publishes the active looting interaction substate.</summary>
        public void RaiseLootInteractionPhaseChanged(LootInteractionPhase phase) => LootInteractionPhaseChanged?.Invoke(phase);

        /// <summary>Publishes world loot appearance or removal.</summary>
        public void RaiseLootDropChanged(LootDropSnapshot snapshot) => LootDropChanged?.Invoke(snapshot);

        /// <summary>Publishes current runtime deck discard rows.</summary>
        public void RaiseDeckDiscardSelectionChanged(DeckDiscardSelectionSnapshot snapshot) => DeckDiscardSelectionChanged?.Invoke(snapshot);

        /// <summary>Requests opening the loot reward decision for the visible world loot.</summary>
        public void RaiseLootWorldClickedRequested() => LootWorldClickedRequested?.Invoke();

        /// <summary>Requests accepting or declining the offered reward.</summary>
        public void RaiseLootAcquireDecisionRequested(bool acquire) => LootAcquireDecisionRequested?.Invoke(acquire);

        /// <summary>Requests full-deck replacement flow or reward decline.</summary>
        public void RaiseLootReplaceDecisionRequested(bool replace) => LootReplaceDecisionRequested?.Invoke(replace);

        /// <summary>Toggles one exact runtime deck block for discard.</summary>
        public void RaiseDeckDiscardToggleRequested(int instanceId) => DeckDiscardToggleRequested?.Invoke(instanceId);

        /// <summary>Confirms selected runtime deck discards.</summary>
        public void RaiseDeckDiscardConfirmRequested() => DeckDiscardConfirmRequested?.Invoke();

        /// <summary>Cancels discard selection and returns to the previous loot decision.</summary>
        public void RaiseDeckDiscardCancelRequested() => DeckDiscardCancelRequested?.Invoke();

        /// <summary>Requests a stage transition fade direction.</summary>
        public void RaiseStageFadeRequested(StageFadeDirection direction) => StageFadeRequested?.Invoke(direction);

        /// <summary>Publishes completion of a requested stage fade.</summary>
        public void RaiseStageFadeVisualCompleted(StageFadeDirection direction) => StageFadeVisualCompleted?.Invoke(direction);

        /// <summary>Publishes defeat restart/exit choice.</summary>
        public void RaiseRunRestartDecisionRequested(bool restart) => RunRestartDecisionRequested?.Invoke(restart);

        /// <summary>Publishes run-complete restart/exit choice.</summary>
        public void RaiseRunCompleteDecisionRequested(bool restart) => RunCompleteDecisionRequested?.Invoke(restart);

        /// <summary>Publishes one newly defeated enemy and its gold reward.</summary>
        public void RaiseEnemyDefeated(int enemyIndex, int goldReward) => EnemyDefeated?.Invoke(enemyIndex, goldReward);

        /// <summary>Requests death presentation for one stable enemy party index.</summary>
        public void RaiseEnemyDeathVisualRequested(int enemyIndex) => EnemyDeathVisualRequested?.Invoke(enemyIndex);

        /// <summary>Publishes the current world anchor of one enemy view.</summary>
        public void RaiseEnemyWorldAnchorChanged(int enemyIndex, Vector3 worldPosition) => EnemyWorldAnchorChanged?.Invoke(enemyIndex, worldPosition);
    }
}
