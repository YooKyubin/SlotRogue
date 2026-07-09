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

                if (combatEvent.TargetBefore.Shield > 0 && combatEvent.TargetAfter.Shield == 0)
                {
                    await Host.Commands.ShowShieldBreakAsync(shieldRequest, cancellationToken);
                }
                else
                {
                    await Host.Commands.ShowShieldHitAsync(shieldRequest, cancellationToken);
                }
            }

            viewModel.ApplyParticipantSnapshot(
                combatEvent.TargetParticipantId,
                combatEvent.TargetAfter,
                combatEvent.IsPlayerParticipant);

            var request = new FloatingCombatTextRequest(
                FloatingCombatTextKind.Damage,
                combatEvent.ApplyResult.DamageDealt,
                ShouldUseDamageScaledFontSize(combatEvent),
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

            if (ShouldRequestPlayerDirectDamageVFX(combatEvent))
            {
                CombatDamageVFXRequest damageVFXRequest = new(
                    CombatDamageVFXProfile.PlayerDirectDamage,
                    combatEvent.TargetParticipantId,
                    combatEvent.ApplyResult.DamageDealt);
                presentationTasks.Add(
                    Host.Commands.ShowCombatDamageVFXAsync(damageVFXRequest, cancellationToken));
            }

            if (Host.StatusCommands != null)
            {
                if (combatEvent.IsPlayerParticipant &&
                    combatEvent.Effect.DamageOrigin == DamageOrigin.Reflection &&
                    combatEvent.SourceParticipantId.IsValid)
                {
                    presentationTasks.Add(
                        Host.StatusCommands.PlayEnemyStatusActivationAsync(
                            combatEvent.SourceParticipantId,
                            StatusEffectKind.Thorns,
                            cancellationToken));
                }

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

            if (ShouldPlayEnemyDeath(combatEvent))
            {
                await Host.Commands.PlayEnemyDeathAsync(
                    combatEvent.TargetParticipantId,
                    cancellationToken);
            }
        }

        private static bool ShouldPlayEnemyDeath(CombatEvent combatEvent)
        {
            return !combatEvent.IsPlayerParticipant &&
                combatEvent.TargetParticipantId.IsValid &&
                combatEvent.TargetBefore.Hp > 0 &&
                combatEvent.TargetAfter.Hp <= 0;
        }

        private static bool ShouldRequestPlayerDirectDamageVFX(CombatEvent combatEvent)
        {
            return combatEvent.Kind == CombatEventKind.EffectApplied &&
                combatEvent.Phase == BattlePhase.Resolving &&
                !combatEvent.IsPlayerParticipant &&
                combatEvent.TargetParticipantId.IsValid &&
                combatEvent.Effect.Kind == CombatEffectKind.Damage &&
                combatEvent.Effect.DamageOrigin == DamageOrigin.DirectAction &&
                combatEvent.ApplyResult.DamageDealt > 0;
        }

        private static bool ShouldUseDamageScaledFontSize(CombatEvent combatEvent)
        {
            return !combatEvent.IsPlayerParticipant &&
                combatEvent.Effect.Kind == CombatEffectKind.Damage &&
                (combatEvent.Effect.DamageOrigin == DamageOrigin.DirectAction ||
                 combatEvent.Effect.DamageOrigin == DamageOrigin.Status);
        }
    }
}
