using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class CombatPresentationPipeline
    {
        private readonly ICombatEventPresenter _phaseChangedPresenter;
        private readonly ICombatEventPresenter _shieldResetPresenter;
        private readonly ICombatEventPresenter _battleEndedPresenter;
        private readonly ICombatEventPresenter _damagePresenter;
        private readonly ICombatEventPresenter _shieldPresenter;
        private readonly ICombatEventPresenter _healPresenter;
        private readonly ICombatEventPresenter _actionStartedPresenter;
        private readonly ICombatEventPresenter _actionCompletedPresenter;
        private readonly ICombatEventPresenter _fallbackPresenter;

        public CombatPresentationPipeline(
            ICombatEventPresenter phaseChangedPresenter,
            ICombatEventPresenter shieldResetPresenter,
            ICombatEventPresenter battleEndedPresenter,
            ICombatEventPresenter damagePresenter,
            ICombatEventPresenter shieldPresenter,
            ICombatEventPresenter healPresenter,
            ICombatEventPresenter actionStartedPresenter,
            ICombatEventPresenter actionCompletedPresenter,
            ICombatEventPresenter fallbackPresenter)
        {
            _phaseChangedPresenter = phaseChangedPresenter;
            _shieldResetPresenter = shieldResetPresenter;
            _battleEndedPresenter = battleEndedPresenter;
            _damagePresenter = damagePresenter;
            _shieldPresenter = shieldPresenter;
            _healPresenter = healPresenter;
            _actionStartedPresenter = actionStartedPresenter;
            _actionCompletedPresenter = actionCompletedPresenter;
            _fallbackPresenter = fallbackPresenter;
        }

        public static CombatPresentationPipeline CreateDefault(CombatPresentationHost host)
        {
            return new CombatPresentationPipeline(
                new PhaseChangedPresenter(host),
                new ShieldResetPresenter(host),
                new BattleEndedPresenter(host),
                new DamagePresenter(host),
                new ShieldPresenter(host),
                new HealPresenter(host),
                new ActionStartedPresenter(host),
                new ActionCompletedPresenter(host),
                new CombatDummyPresenter());
        }

        public UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            switch (combatEvent.Kind)
            {
                case CombatEventKind.PhaseChanged:
                    return _phaseChangedPresenter.PresentAsync(
                        combatEvent,
                        viewModel,
                        context,
                        cancellationToken);

                case CombatEventKind.ShieldReset:
                    return _shieldResetPresenter.PresentAsync(
                        combatEvent,
                        viewModel,
                        context,
                        cancellationToken);

                case CombatEventKind.BattleEnded:
                    return _battleEndedPresenter.PresentAsync(
                        combatEvent,
                        viewModel,
                        context,
                        cancellationToken);

                case CombatEventKind.EffectApplied:
                    return RouteEffectApplied(combatEvent, viewModel, context, cancellationToken);

                case CombatEventKind.ActionStarted:
                    return _actionStartedPresenter.PresentAsync(
                        combatEvent,
                        viewModel,
                        context,
                        cancellationToken);

                case CombatEventKind.ActionCompleted:
                    return _actionCompletedPresenter.PresentAsync(
                        combatEvent,
                        viewModel,
                        context,
                        cancellationToken);

                default:
                    return _fallbackPresenter.PresentAsync(
                        combatEvent,
                        viewModel,
                        context,
                        cancellationToken);
            }
        }

        private UniTask RouteEffectApplied(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            ICombatEventPresenter presenter = combatEvent.Effect.Kind switch
            {
                CombatEffectKind.Damage => _damagePresenter,
                CombatEffectKind.Shield => _shieldPresenter,
                CombatEffectKind.Heal => _healPresenter,
                _ => _fallbackPresenter,
            };

            return presenter.PresentAsync(combatEvent, viewModel, context, cancellationToken);
        }
    }
}
