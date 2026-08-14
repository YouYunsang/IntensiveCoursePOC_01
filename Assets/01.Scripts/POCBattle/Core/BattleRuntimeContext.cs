using PocBattle.Data;

namespace PocBattle.Core
{
    /// <summary>
    /// Composition object that owns pure runtime models and services for one stage battle.
    /// Persistent player/deck progression is injected from PlayerRunModel instead of rebuilt per stage.
    /// </summary>
    public sealed class BattleRuntimeContext
    {
        /// <summary>Seed offset used to decorrelate cell-effect placement from block shuffle order.</summary>
        private const int CELL_EFFECT_RANDOM_SEED_SALT = 486187739;
        private const int STAGE_FIELD_RANDOM_SEED_SALT = 923521;

        /// <summary>Remaining player moves in the current player turn.</summary>
        private int _remainingMoveCount;

        /// <summary>Remaining placement edits in the current player turn.</summary>
        private int _remainingEditCount;

        /// <summary>Gets logical board.</summary>
        public BoardModel Board { get; }

        /// <summary>Gets persistent block placement service for this stage battle.</summary>
        public BlockPlacementService PlacementService { get; }

        /// <summary>Gets player-owned cell-effect placement service for this stage battle.</summary>
        public CellEffectPlacementService CellEffectPlacementService { get; }

        /// <summary>Gets fixed stage field-effect runtime service.</summary>
        public StageFieldEffectService StageFieldEffects { get; }

        /// <summary>Gets direct-collision expansion resolver for field-effect block chains.</summary>
        public BlockCollisionResolver BlockCollisions { get; }

        /// <summary>Gets deterministic segment slide resolver.</summary>
        public GridMovementResolver MovementResolver { get; }

        /// <summary>Gets persistent player combat model shared by the complete run.</summary>
        public PlayerCombatModel Player { get; }

        /// <summary>Gets current player turn effects.</summary>
        public PlayerTurnEffectContext TurnEffects { get; }

        /// <summary>Gets composable block effect resolver.</summary>
        public BlockEffectResolver BlockEffects { get; }

        /// <summary>Gets ordered enemy party model for this stage.</summary>
        public EnemyPartyModel EnemyParty { get; }

        /// <summary>Gets deterministic enemy action resolver.</summary>
        public EnemyTurnResolver EnemyTurnResolver { get; }

        /// <summary>Gets remaining slide moves this turn.</summary>
        public int RemainingMoveCount => _remainingMoveCount;

        /// <summary>Gets remaining placement edits this turn.</summary>
        public int RemainingEditCount => _remainingEditCount;

        /// <summary>Creates stage-local models while reusing persistent run player/deck progression.</summary>
        public BattleRuntimeContext(
            BattleBoardSettingsSO boardSettings,
            PlayerRunModel runModel,
            StageBattleSetup stageSetup)
        {
            Board = new BoardModel(boardSettings);
            Player = runModel.Player;
            TurnEffects = new PlayerTurnEffectContext(Player);
            BlockEffects = new BlockEffectResolver(TurnEffects);
            EnemyParty = new EnemyPartyModel(stageSetup.Encounter);
            PlacementService = new BlockPlacementService(Board, boardSettings, runModel.Deck, stageSetup.Encounter, stageSetup.RandomSeed);
            int cellEffectSeed = unchecked(stageSetup.RandomSeed * 397 ^ CELL_EFFECT_RANDOM_SEED_SALT);
            CellEffectPlacementService = new CellEffectPlacementService(Board, runModel.Deck, cellEffectSeed);
            int stageFieldSeed = unchecked(stageSetup.RandomSeed * 7919 ^ STAGE_FIELD_RANDOM_SEED_SALT);
            StageFieldEffects = new StageFieldEffectService(Board, stageSetup.FieldEffects, stageFieldSeed);
            BlockCollisions = new BlockCollisionResolver(Board, StageFieldEffects);
            MovementResolver = new GridMovementResolver(Board);
            EnemyTurnResolver = new EnemyTurnResolver(Player, EnemyParty);
            ResetTurnResources();
        }

        /// <summary>Copies upgradeable maximum move/edit stats into current-turn counters.</summary>
        public void ResetTurnResources()
        {
            _remainingMoveCount = Player.MaxMoveCount;
            _remainingEditCount = Player.MaxEditCount;
        }

        /// <summary>Consumes one successful manual movement if a move remains.</summary>
        public bool TryConsumeMove()
        {
            if (_remainingMoveCount <= 0)
            {
                return false;
            }

            _remainingMoveCount--;
            return true;
        }

        /// <summary>Consumes one successful placement edit if an edit remains.</summary>
        public bool TryConsumeEdit()
        {
            if (_remainingEditCount <= 0)
            {
                return false;
            }

            _remainingEditCount--;
            return true;
        }
    }
}
