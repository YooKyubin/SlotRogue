using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class ShieldPresenter : CombatPresenterBase
    {
        private const float TweenDuration = 0.15f;

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

            await TweenTargetShieldAsync(combatEvent, viewModel, TweenDuration, cancellationToken);
            viewModel.ApplyParticipantSnapshot(
                combatEvent.TargetParticipantId,
                combatEvent.TargetAfter,
                combatEvent.IsPlayerParticipant);
            RefreshHUD();
        }
    }
}
