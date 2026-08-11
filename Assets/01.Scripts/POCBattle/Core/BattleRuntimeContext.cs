using PocBattle.Data;

namespace PocBattle.Core
{
    /// <summary>
    /// Composition object that owns pure runtime models and services for one battle session.
    /// </summary>
    public sealed class BattleRuntimeContext
    {
        /// <summary>Remaining player moves in the current player turn.</summary>
        private int _remainingMoveCount;

        /// <summary>Remaining placement edits in the current player turn.</summary>
        private int _remainingEditCount;

        /// <summary>Gets logical board.</summary>
        public BoardModel Board { get; }

        /// <summary>Gets persistent block placement service.</summary>
        public BlockPlacementService PlacementService { get; }

        /// <summary>Gets deterministic slide resolver.</summary>
        public GridMovementResolver MovementResolver { get; }

        /// <summary>Gets mutable player combat model.</summary>
        public PlayerCombatModel Player { get; }

        /// <summary>Gets current player turn effects.</summary>
        public PlayerTurnEffectContext TurnEffects { get; }

        /// <summary>Gets composable block effect resolver.</summary>
        public BlockEffectResolver BlockEffects { get; }

        /// <summary>Gets ordered enemy party model.</summary>
        public EnemyPartyModel EnemyParty { get; }

        /// <summary>Gets deterministic enemy action resolver.</summary>
        public EnemyTurnResolver EnemyTurnResolver { get; }

        /// <summary>Gets remaining slide moves this turn.</summary>
        public int RemainingMoveCount => _remainingMoveCount;

        /// <summary>Gets remaining placement edits this turn.</summary>
        public int RemainingEditCount => _remainingEditCount;

        /// <summary>
        /// Creates all pure runtime models and services without GameObject-to-GameObject references.
        /// </summary>
        public BattleRuntimeContext(
            BattleBoardSettingsSO boardSettings,
            PlayerBaseStatsSO playerBaseStats,
            DeckDefinitionSO deck,
            EncounterDefinitionSO encounter,
            int randomSeed)
        {
            Board = new BoardModel(boardSettings);
            Player = new PlayerCombatModel(playerBaseStats);
            TurnEffects = new PlayerTurnEffectContext(Player);
            BlockEffects = new BlockEffectResolver(TurnEffects);
            EnemyParty = new EnemyPartyModel(encounter);
            PlacementService = new BlockPlacementService(Board, boardSettings, deck, encounter, randomSeed);
            MovementResolver = new GridMovementResolver(Board);
            EnemyTurnResolver = new EnemyTurnResolver(Player, EnemyParty);
            ResetTurnResources();
        }

        /// <summary>
        /// Copies the player's upgradeable maximum move/edit stats into current-turn counters.
        /// </summary>
        public void ResetTurnResources()
        {
            _remainingMoveCount = Player.MaxMoveCount;
            _remainingEditCount = Player.MaxEditCount;
        }

        /// <summary>
        /// Consumes one successful movement if a move remains.
        /// </summary>
        public bool TryConsumeMove()
        {
            if (_remainingMoveCount <= 0)
            {
                return false;
            }

            _remainingMoveCount--;
            return true;
        }

        /// <summary>
        /// Consumes one successful placement edit if an edit remains.
        /// </summary>
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
