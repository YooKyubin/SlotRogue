using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class ShieldResetPresenter : CombatPresenterBase
    {
        private const float BlinkDuration = 0.1f;

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

            viewModel.SetParticipantShield(
                combatEvent.TargetParticipantId,
                0,
                combatEvent.IsPlayerParticipant);

            RefreshHUD();
            await CombatPresentationTweens.DelayAsync(BlinkDuration, Host.LinkTarget, cancellationToken);
        }
    }
}
