using System.Collections.Generic;
using System.Text;
using PocBattle.Core;
using PocBattle.Data;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Converts pure runtime model state into immutable event snapshots for views/presenters.
    /// </summary>
    public sealed class BattlePresentationPublisher
    {
        /// <summary>Runtime battle models queried to build snapshots.</summary>
        private readonly BattleRuntimeContext _context;

        /// <summary>Presentation settings used only for final-damage preview calculation.</summary>
        private readonly BattlePresentationSettingsSO _presentationSettings;

        /// <summary>Shared event hub used to publish snapshots across GameObjects.</summary>
        private readonly BattleEventChannelSO _eventChannel;

        /// <summary>Reusable block snapshot list to avoid per-edit garbage.</summary>
        private readonly List<BlockSnapshot> _blockSnapshots;

        /// <summary>Reusable enemy setup list created once per battle initialization.</summary>
        private readonly List<EnemySetupSnapshot> _enemySetupSnapshots;

        /// <summary>Reusable intent text builder for multi-action pattern steps.</summary>
        private readonly StringBuilder _intentBuilder;

        /// <summary>
        /// Creates one presentation publisher for a battle runtime.
        /// </summary>
        public BattlePresentationPublisher(
            BattleRuntimeContext context,
            BattlePresentationSettingsSO presentationSettings,
            BattleEventChannelSO eventChannel)
        {
            _context = context;
            _presentationSettings = presentationSettings;
            _eventChannel = eventChannel;
            _blockSnapshots = new List<BlockSnapshot>(context.PlacementService.Blocks.Count);
            _enemySetupSnapshots = new List<EnemySetupSnapshot>(context.EnemyParty.Enemies.Count);
            _intentBuilder = new StringBuilder();
        }

        /// <summary>
        /// Publishes current player HP and shield.
        /// </summary>
        public void PublishPlayerStatus()
        {
            PlayerCombatModel player = _context.Player;
            _eventChannel.RaisePlayerStatusChanged(
                new PlayerStatusSnapshot(player.CurrentHealth, player.MaxHealth, player.Shield));
        }

        /// <summary>
        /// Publishes remaining and maximum per-turn move/edit resources.
        /// </summary>
        public void PublishTurnResources()
        {
            PlayerCombatModel player = _context.Player;
            _eventChannel.RaiseTurnResourcesChanged(
                new TurnResourceSnapshot(
                    _context.RemainingMoveCount,
                    player.MaxMoveCount,
                    _context.RemainingEditCount,
                    player.MaxEditCount));
        }

        /// <summary>
        /// Publishes collected attack/shield/critical values and rounded damage preview.
        /// </summary>
        public void PublishTurnEffects()
        {
            PlayerTurnEffectContext effects = _context.TurnEffects;
            int finalDamage = effects.CalculateFinalDamage(_presentationSettings.CriticalMultiplier);
            _eventChannel.RaiseTurnEffectsChanged(
                new TurnEffectSnapshot(effects.Attack, effects.PendingShield, effects.CriticalStacks, finalDamage));
        }

        /// <summary>
        /// Publishes every persistent runtime block coordinate, optionally requesting tweens.
        /// </summary>
        public void PublishBlockLayout(bool animate)
        {
            _blockSnapshots.Clear();
            IReadOnlyList<BlockRuntime> blocks = _context.PlacementService.Blocks;
            for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
            {
                BlockRuntime block = blocks[blockIndex];
                _blockSnapshots.Add(new BlockSnapshot(block.Id, block.Definition, block.Coordinate));
            }

            _eventChannel.RaiseBlockLayoutChanged(_blockSnapshots, animate);
        }

        /// <summary>
        /// Publishes player logical coordinate for immediate visual synchronization.
        /// </summary>
        public void PublishPlayerPositionSync()
        {
            _eventChannel.RaisePlayerPositionSyncRequested(_context.Board.PlayerPosition);
        }

        /// <summary>
        /// Publishes immutable setup data so the enemy view can create one child per runtime enemy.
        /// </summary>
        public void PublishEnemyPartySetup()
        {
            _enemySetupSnapshots.Clear();
            IReadOnlyList<EnemyCombatModel> enemies = _context.EnemyParty.Enemies;
            for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
            {
                EnemyCombatModel enemy = enemies[enemyIndex];
                _enemySetupSnapshots.Add(
                    new EnemySetupSnapshot(
                        enemy.PartyIndex,
                        enemy.Definition.DisplayName,
                        enemy.Definition.DisplayColor,
                        enemy.Definition.MaxHealth));
            }

            _eventChannel.RaiseEnemyPartySetupRequested(_enemySetupSnapshots);
        }

        /// <summary>
        /// Publishes status and next-turn intent for every enemy.
        /// </summary>
        public void PublishAllEnemyPresentation()
        {
            IReadOnlyList<EnemyCombatModel> enemies = _context.EnemyParty.Enemies;
            for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
            {
                PublishEnemyStatus(enemies[enemyIndex]);
                PublishEnemyIntent(enemies[enemyIndex]);
            }
        }

        /// <summary>
        /// Publishes one enemy HP/shield/life state.
        /// </summary>
        public void PublishEnemyStatus(EnemyCombatModel enemy)
        {
            _eventChannel.RaiseEnemyStatusChanged(
                new EnemyStatusSnapshot(
                    enemy.PartyIndex,
                    enemy.CurrentHealth,
                    enemy.Definition.MaxHealth,
                    enemy.Shield,
                    enemy.IsAlive));
        }

        /// <summary>
        /// Builds and publishes one next-turn intent from the exact action step that will execute.
        /// </summary>
        public void PublishEnemyIntent(EnemyCombatModel enemy)
        {
            _eventChannel.RaiseEnemyIntentChanged(
                new EnemyIntentSnapshot(enemy.PartyIndex, BuildIntentText(enemy)));
        }

        /// <summary>
        /// Builds a compact intent summary from all actions in the enemy's current pattern step.
        /// </summary>
        private string BuildIntentText(EnemyCombatModel enemy)
        {
            if (!enemy.IsAlive)
            {
                return "DEFEATED";
            }

            EnemyPatternStep step = enemy.GetCurrentPatternStep();
            if (step == null || step.Actions.Count == 0)
            {
                return "WAIT";
            }

            _intentBuilder.Clear();
            for (int actionIndex = 0; actionIndex < step.Actions.Count; actionIndex++)
            {
                EnemyActionDefinitionSO action = step.Actions[actionIndex];
                if (action == null)
                {
                    continue;
                }

                if (_intentBuilder.Length > 0)
                {
                    _intentBuilder.Append(" / ");
                }

                EnemyIntentPart intent = action.BuildIntent();
                _intentBuilder.Append(intent.Type == EnemyIntentType.Attack ? "ATK " : "SHD ");
                _intentBuilder.Append(intent.Value);
            }

            return _intentBuilder.Length > 0 ? _intentBuilder.ToString() : "WAIT";
        }
    }
}
