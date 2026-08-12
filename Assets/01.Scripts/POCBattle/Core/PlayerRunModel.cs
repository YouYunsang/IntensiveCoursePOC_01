using System;
using PocBattle.Data;

namespace PocBattle.Core
{
    /// <summary>
    /// Persistent mutable player progression for one complete run: combat state, runtime deck, and gold.
    /// </summary>
    public sealed class PlayerRunModel
    {
        /// <summary>Persistent combat state shared between stage battle contexts.</summary>
        private readonly PlayerCombatModel _player;

        /// <summary>Persistent mutable deck shared between stages.</summary>
        private readonly PlayerDeckModel _deck;

        /// <summary>Current run gold.</summary>
        private int _gold;

        /// <summary>Gets persistent player combat state.</summary>
        public PlayerCombatModel Player => _player;

        /// <summary>Gets persistent runtime deck.</summary>
        public PlayerDeckModel Deck => _deck;

        /// <summary>Gets current run gold.</summary>
        public int Gold => _gold;

        /// <summary>
        /// Creates a fresh run from immutable player stats and starting deck data.
        /// </summary>
        public PlayerRunModel(PlayerBaseStatsSO baseStats, DeckDefinitionSO startingDeck)
        {
            if (baseStats == null)
            {
                throw new ArgumentNullException(nameof(baseStats));
            }

            _player = new PlayerCombatModel(baseStats);
            _deck = new PlayerDeckModel(startingDeck, baseStats.DeckCapacity);
            _gold = 0;
        }

        /// <summary>Adds non-negative enemy reward gold.</summary>
        public void AddGold(int amount)
        {
            _gold += Math.Max(0, amount);
        }
    }
}
