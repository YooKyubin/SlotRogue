using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class DamagePresenter : CombatPresenterBase
    {
        public DamagePresenter(CombatPresentationHost host)
            : base(host)
        {
        }

        public override async UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            if (combatEvent.Kind != CombatEventKind.EffectApplied ||
                combatEvent.Effect.Kind != CombatEffectKind.Damage)
            {
                return;
            }

            if (combatEvent.ApplyResult.ShieldConsumed > 0)
            {
                var shieldRequest = new ShieldPresentationRequest(
                    combatEvent.ApplyResult.ShieldConsumed,
                    combatEvent.IsPlayerParticipant,
                    combatEvent.TargetParticipantId);
                await Host.Commands.ShowShieldHitAsync(shieldRequest, cancellationToken);

                if (combatEvent.TargetBefore.Shield > 0 && combatEvent.TargetAfter.Shield == 0)
                {
                    await Host.Commands.ShowShieldBreakAsync(shieldRequest, cancellationToken);
                }
            }

            viewModel.ApplyParticipantSnapshot(
                combatEvent.TargetParticipantId,
                combatEvent.TargetAfter,
                combatEvent.IsPlayerParticipant);

            var request = new FloatingDamageRequest(
                combatEvent.ApplyResult.DamageDealt,
                context.IsCritical,
                combatEvent.IsPlayerParticipant,
                combatEvent.TargetParticipantId);

            await Host.Commands.ShowFloatingDamageAsync(request, cancellationToken);
        }
    }
}
