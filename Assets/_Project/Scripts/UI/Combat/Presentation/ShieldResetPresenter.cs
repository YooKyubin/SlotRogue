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

            if (combatEvent.IsPlayerParticipant)
            {
                viewModel.SetPlayerShield(0);
            }
            else
            {
                viewModel.SetMonsterShield(0);
            }

            RefreshHUD();
            await CombatPresentationTweens.DelayAsync(BlinkDuration, Host.LinkTarget, cancellationToken);
        }
    }
}
