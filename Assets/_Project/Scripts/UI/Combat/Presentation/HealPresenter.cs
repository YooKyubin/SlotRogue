using System.Collections.Generic;
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

        public override async UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            if (combatEvent.Kind != CombatEventKind.EffectApplied ||
                combatEvent.Effect.Kind != CombatEffectKind.Heal)
            {
                return;
            }

            viewModel.ApplyParticipantSnapshot(
                combatEvent.TargetParticipantId,
                combatEvent.TargetAfter,
                combatEvent.IsPlayerParticipant);

            var request = new FloatingCombatTextRequest(
                FloatingCombatTextKind.Heal,
                combatEvent.ApplyResult.HealApplied,
                useDamageScaledFontSize: false,
                combatEvent.IsPlayerParticipant,
                combatEvent.TargetParticipantId);

            var presentationTasks = new List<UniTask>
            {
                Host.Commands.ShowFloatingCombatTextAsync(request, cancellationToken),
                Host.Commands.WaitHealthBarAsync(
                    combatEvent.TargetParticipantId,
                    combatEvent.IsPlayerParticipant,
                    cancellationToken),
            };

            if (!combatEvent.IsPlayerParticipant &&
                combatEvent.StatusEffectKind == StatusEffectKind.Lifesteal &&
                Host.StatusCommands != null)
            {
                presentationTasks.Add(
                    Host.StatusCommands.PlayEnemyStatusActivationAsync(
                        combatEvent.TargetParticipantId,
                        StatusEffectKind.Lifesteal,
                        cancellationToken));
            }

            await UniTask.WhenAll(presentationTasks);
        }
    }
}
