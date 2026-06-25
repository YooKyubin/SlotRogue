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
        private readonly ICombatEventPresenter _statusEffectPresenter;
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
            ICombatEventPresenter statusEffectPresenter,
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
            _statusEffectPresenter = statusEffectPresenter;
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
                new StatusEffectPresenter(host),
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

                case CombatEventKind.StatusTicked:
                    return _damagePresenter.PresentAsync(
                        combatEvent,
                        viewModel,
                        context,
                        cancellationToken);

                case CombatEventKind.StatusApplied:
                case CombatEventKind.StatusValueChanged:
                case CombatEventKind.StatusExpired:
                    return _statusEffectPresenter.PresentAsync(
                        combatEvent,
                        viewModel,
                        context,
                        cancellationToken);

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

    public sealed class StatusEffectPresenter : ICombatEventPresenter
    {
        private readonly ICombatStatusPresentationCommands _commands;

        public StatusEffectPresenter(CombatPresentationHost host)
        {
            _commands = host.StatusCommands ??
                throw new System.ArgumentException(
                    "Combat status presentation commands are required.",
                    nameof(host));
        }

        public async UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            if (combatEvent.IsPlayerParticipant)
            {
                return;
            }

            switch (combatEvent.Kind)
            {
                case CombatEventKind.StatusApplied:
                {
                    StatusEffectViewData status = Map(combatEvent);
                    viewModel.AddOrReplaceStatus(combatEvent.TargetParticipantId, status);
                    await _commands.AddEnemyStatusAsync(
                        combatEvent.TargetParticipantId,
                        status,
                        cancellationToken);
                    viewModel.PublishStatusChanged();
                    break;
                }
                case CombatEventKind.StatusValueChanged:
                {
                    StatusEffectViewData status = Map(combatEvent);
                    viewModel.AddOrReplaceStatus(combatEvent.TargetParticipantId, status);
                    await _commands.UpdateEnemyStatusValueAsync(
                        combatEvent.TargetParticipantId,
                        status,
                        cancellationToken);
                    viewModel.PublishStatusChanged();
                    break;
                }
                case CombatEventKind.StatusExpired:
                    viewModel.RemoveStatus(
                        combatEvent.TargetParticipantId,
                        combatEvent.StatusEffectKind);
                    await _commands.RemoveEnemyStatusAsync(
                        combatEvent.TargetParticipantId,
                        combatEvent.StatusEffectKind,
                        cancellationToken);
                    viewModel.PublishStatusChanged();
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(combatEvent),
                        combatEvent.Kind,
                        "Status presenter received an unsupported combat event.");
            }
        }

        private static StatusEffectViewData Map(CombatEvent combatEvent)
        {
            return StatusEffectPresentationMapper.Map(
                combatEvent.StatusEffectKind,
                combatEvent.StatusMagnitude,
                combatEvent.StatusStackCount,
                combatEvent.StatusDuration);
        }
    }
}
