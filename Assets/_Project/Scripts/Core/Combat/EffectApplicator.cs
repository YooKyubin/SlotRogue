using System;

namespace SlotRogue.Core.Combat
{
    public sealed class EffectApplicator
    {
        public EffectApplyResult Apply(
            CombatEffect effect,
            CombatParticipant source,
            CombatParticipant opponent)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (opponent == null)
            {
                throw new ArgumentNullException(nameof(opponent));
            }

            CombatParticipant target = ResolveTarget(effect.Target, source, opponent);
            return ApplyToParticipant(effect, target);
        }

        public EffectApplyResult ApplyToParticipant(CombatEffect effect, CombatParticipant target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            return effect.Kind switch
            {
                CombatEffectKind.Damage => ApplyDamage(target, effect.Amount),
                CombatEffectKind.Shield => ApplyShield(target, effect.Amount),
                CombatEffectKind.Heal => ApplyHeal(target, effect.Amount),
                _ => EffectApplyResult.None,
            };
        }

        private static CombatParticipant ResolveTarget(
            CombatEffectTarget target,
            CombatParticipant source,
            CombatParticipant opponent)
        {
            return target.Mode == CombatTargetMode.Self ? source : opponent;
        }

        private static EffectApplyResult ApplyDamage(CombatParticipant target, int amount)
        {
            if (amount <= 0)
            {
                return EffectApplyResult.None;
            }

            int shieldConsumed = Math.Min(target.Shield, amount);
            int damageDealt = amount - shieldConsumed;

            target.Shield -= shieldConsumed;
            target.CurrentHp = Math.Max(0, target.CurrentHp - damageDealt);

            return new EffectApplyResult(damageDealt, shieldConsumed, 0, 0);
        }

        private static EffectApplyResult ApplyShield(CombatParticipant target, int amount)
        {
            if (amount <= 0)
            {
                return EffectApplyResult.None;
            }

            target.Shield += amount;
            return new EffectApplyResult(0, 0, amount, 0);
        }

        private static EffectApplyResult ApplyHeal(CombatParticipant target, int amount)
        {
            if (amount <= 0)
            {
                return EffectApplyResult.None;
            }

            int healApplied = Math.Min(amount, target.MaxHp - target.CurrentHp);
            target.CurrentHp += healApplied;

            return new EffectApplyResult(0, 0, 0, healApplied);
        }
    }
}
