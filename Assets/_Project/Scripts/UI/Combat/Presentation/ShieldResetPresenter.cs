using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class ShieldResetPresenter : CombatPresenterBase
    {
        public ShieldResetPresenter(CombatPresentationHost host)
            : base(host)
        {
        }

        public override async UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            if (combatEvent.Kind != CombatEventKind.ShieldReset)
            {
                return;
            }

            if (combatEvent.TargetBefore.Shield > 0)
            {
                var request = new ShieldPresentationRequest(
                    combatEvent.TargetBefore.Shield,
                    combatEvent.IsPlayerParticipant,
                    combatEvent.TargetParticipantId);
                UniTask shieldExpireTask = Host.Commands.ShowShieldExpireAsync(request, cancellationToken);
                viewModel.SetParticipantShield(
                    combatEvent.TargetParticipantId,
                    0,
                    combatEvent.IsPlayerParticipant);
                UniTask hpBarTask = Host.Commands.WaitHealthBarAsync(
                    combatEvent.TargetParticipantId,
                    combatEvent.IsPlayerParticipant,
                    cancellationToken);
                await UniTask.WhenAll(shieldExpireTask, hpBarTask);
                return;
            }

            viewModel.SetParticipantShield(
                combatEvent.TargetParticipantId,
                0,
                combatEvent.IsPlayerParticipant);
        }
    }
}
