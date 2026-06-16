using System;

namespace SlotRogue.Core.Combat
{
    public readonly struct EnemyActionEffect
    {
        private EnemyActionEffect(
            EnemyActionEffectKind kind,
            CombatEffect combatEffect,
            int lockCount,
            int durationTurns)
        {
            Kind = kind;
            CombatEffect = combatEffect;
            LockCount = Math.Max(0, lockCount);
            DurationTurns = Math.Max(0, durationTurns);
        }

        public EnemyActionEffectKind Kind { get; }

        public CombatEffect CombatEffect { get; }

        public int LockCount { get; }

        public int DurationTurns { get; }

        public static EnemyActionEffect FromCombatEffect(CombatEffect combatEffect) =>
            new(EnemyActionEffectKind.Combat, combatEffect, lockCount: 0, durationTurns: 0);

        public static EnemyActionEffect LockSlot(int lockCount, int durationTurns) =>
            new(EnemyActionEffectKind.LockSlot, default, lockCount, durationTurns);
    }
}
