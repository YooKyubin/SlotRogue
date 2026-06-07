using System;

namespace SlotRogue.Core.Combat
{
    public sealed class EffectApplicator
    {
        public EffectApplyResult ApplyToParticipant(CombatEffect effect, CombatParticipant target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (effect.Amount <= 0)
            {
                return EffectApplyResult.None;
            }

            return effect.Kind switch
            {
                CombatEffectKind.Damage => target.ApplyDamage(effect.Amount),
                CombatEffectKind.Shield => target.GainShield(effect.Amount),
                CombatEffectKind.Heal => target.Heal(effect.Amount),
                _ => EffectApplyResult.None,
            };
        }
    }
}
