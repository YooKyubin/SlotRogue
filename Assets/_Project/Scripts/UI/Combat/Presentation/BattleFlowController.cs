using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class BattleFlowController
    {
        private readonly CombatPresentationPipeline _pipeline;
        private readonly CombatViewModel _viewModel;
        private bool _isBusy;

        public BattleFlowController(CombatPresentationPipeline pipeline, CombatViewModel viewModel)
        {
            _pipeline = pipeline;
            _viewModel = viewModel;
        }

        public bool IsBusy => _isBusy;

        public CombatViewModel ViewModel => _viewModel;

        public async UniTask<BattleApplyResult> RunTurnAsync(
            BattleSystem battle,
            IReadOnlyList<CombatEffect> effects,
            CombatParticipantId selectedTargetId,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            if (_isBusy)
            {
                return BattleApplyResult.Rejected(battle.CurrentPhase);
            }

            _isBusy = true;
            try
            {
                int startIndex = battle.Events.Count;
                BattleApplyResult result = battle.ApplyPlayerTurn(effects, selectedTargetId);

                if (!result.Accepted)
                {
                    return result;
                }

                for (int index = startIndex; index < battle.Events.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    CombatEvent combatEvent = battle.Events[index];
                    await _pipeline.PresentAsync(combatEvent, _viewModel, context, cancellationToken);
                }

                _viewModel.SyncFrom(battle);
                return result;
            }
            finally
            {
                _isBusy = false;
            }
        }
    }
}
