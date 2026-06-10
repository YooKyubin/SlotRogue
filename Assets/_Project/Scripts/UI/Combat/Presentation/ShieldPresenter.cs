using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class ShieldPresenter : CombatPresenterBase
    {
        public ShieldPresenter(CombatPresentationHost host)
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
                combatEvent.Effect.Kind != CombatEffectKind.Shield)
            {
                return;
            }

            viewModel.ApplyParticipantSnapshot(
                combatEvent.TargetParticipantId,
                combatEvent.TargetAfter,
                combatEvent.IsPlayerParticipant);

            var request = new ShieldPresentationRequest(
                combatEvent.ApplyResult.ShieldGained,
                combatEvent.IsPlayerParticipant,
                combatEvent.TargetParticipantId);
            await Host.Commands.ShowShieldGainAsync(request, cancellationToken);
        }
    }
}
