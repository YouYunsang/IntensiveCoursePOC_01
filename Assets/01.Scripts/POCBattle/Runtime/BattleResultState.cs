using PocBattle.Core;
using PocBattle.Data;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Terminal battle state that publishes either victory or defeat until restart is requested.
    /// </summary>
    public sealed class BattleResultState : BattleStateBase
    {
        /// <summary>Result published when this terminal state enters.</summary>
        private readonly BattleResult _result;

        /// <summary>
        /// Creates one terminal result state.
        /// </summary>
        public BattleResultState(
            BattleRuntimeContext context,
            BattlePresentationSettingsSO presentationSettings,
            BattleEventChannelSO eventChannel,
            BattlePresentationPublisher publisher,
            BattleResult result)
            : base(context, presentationSettings, eventChannel, publisher)
        {
            _result = result;
        }

        /// <summary>
        /// Publishes the immutable terminal result.
        /// </summary>
        public override void Enter()
        {
            EventChannel.RaiseBattleResultChanged(_result);
        }
    }
}
