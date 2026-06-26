using System.Threading;
using System.Collections.Generic;
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
            if ((combatEvent.Kind != CombatEventKind.EffectApplied &&
                 combatEvent.Kind != CombatEventKind.StatusTicked) ||
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
                combatEvent.Kind == CombatEventKind.EffectApplied && context.IsCritical,
                combatEvent.IsPlayerParticipant,
                combatEvent.TargetParticipantId);

            var presentationTasks = new List<UniTask>
            {
                Host.Commands.ShowFloatingDamageAsync(request, cancellationToken),
                Host.Commands.WaitHealthBarAsync(
                    combatEvent.TargetParticipantId,
                    combatEvent.IsPlayerParticipant,
                    cancellationToken),
            };

            if (combatEvent.IsPlayerParticipant &&
                combatEvent.Effect.DamageOrigin == DamageOrigin.Reflection)
            {
                presentationTasks.Add(Host.Commands.PlayPlayerHitFeedbackAsync(cancellationToken));
            }

            if (Host.StatusCommands != null)
            {
                for (int index = 0; index < combatEvent.AppliedStatusModifiers.Count; index++)
                {
                    AppliedStatusModifier modifier = combatEvent.AppliedStatusModifiers[index];
                    if (modifier.OwnerTeam != CombatTeam.Enemy)
                    {
                        continue;
                    }

                    presentationTasks.Add(
                        Host.StatusCommands.PlayEnemyStatusModifierActivationAsync(
                            modifier.OwnerParticipantId,
                            modifier.Kind,
                            cancellationToken));
                }
            }

            await UniTask.WhenAll(presentationTasks);
        }
    }
}
