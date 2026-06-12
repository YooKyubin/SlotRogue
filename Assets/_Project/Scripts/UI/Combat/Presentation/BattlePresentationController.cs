using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class BattlePresentationController
    {
        private readonly CombatPresentationPipeline _pipeline;
        private readonly CombatViewModel _viewModel;
        private bool _isBusy;

        public BattlePresentationController(
            CombatPresentationPipeline pipeline,
            CombatViewModel viewModel)
        {
            _pipeline = pipeline;
            _viewModel = viewModel;
        }

        public bool IsBusy => _isBusy;

        public CombatViewModel ViewModel => _viewModel;

        public async UniTask PresentEventsAsync(
            BattleSystem battle,
            int startEventIndex,
            PresentationContext context,
            CancellationToken cancellationToken,
            Func<CombatEvent, int, IReadOnlyList<CombatEvent>, UniTask> beforeEventPresented = null,
            Func<CombatEvent, int, IReadOnlyList<CombatEvent>, UniTask> afterEventPresented = null)
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            try
            {
                for (int index = startEventIndex; index < battle.Events.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    CombatEvent combatEvent = battle.Events[index];

                    if (beforeEventPresented != null)
                    {
                        await beforeEventPresented(combatEvent, index, battle.Events);
                    }

                    await _pipeline.PresentAsync(combatEvent, _viewModel, context, cancellationToken);

                    if (afterEventPresented != null)
                    {
                        await afterEventPresented(combatEvent, index, battle.Events);
                    }
                }

                _viewModel.SyncFrom(battle);
            }
            finally
            {
                _isBusy = false;
            }
        }
    }
}
