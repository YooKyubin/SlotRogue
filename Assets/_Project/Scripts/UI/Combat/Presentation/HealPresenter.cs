using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class HealPresenter : CombatPresenterBase
    {
        public HealPresenter(CombatPresentationHost host)
            : base(host)
        {
        }

        public override UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            if (combatEvent.Kind != CombatEventKind.EffectApplied ||
                combatEvent.Effect.Kind != CombatEffectKind.Heal)
            {
                return UniTask.CompletedTask;
            }

            viewModel.ApplyParticipantSnapshot(
                combatEvent.TargetParticipantId,
                combatEvent.TargetAfter,
                combatEvent.IsPlayerParticipant);
            return UniTask.CompletedTask;
        }
    }
}
