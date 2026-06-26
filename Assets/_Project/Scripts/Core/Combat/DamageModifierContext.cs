namespace SlotRogue.Core.Combat
{
    public readonly struct DamageModifierContext
    {
        public DamageOrigin DamageOrigin { get; }
        public CombatParticipantId SourceParticipantId { get; }
        public CombatParticipantId TargetParticipantId { get; }
        public int CurrentDamage { get; }
        public StatusEffectSnapshot StatusSnapshot { get; }

        public DamageModifierContext(
            DamageOrigin damageOrigin,
            CombatParticipantId sourceParticipantId,
            CombatParticipantId targetParticipantId,
            int currentDamage,
            StatusEffectSnapshot statusSnapshot)
        {
            DamageOrigin = damageOrigin;
            SourceParticipantId = sourceParticipantId;
            TargetParticipantId = targetParticipantId;
            CurrentDamage = currentDamage;
            StatusSnapshot = statusSnapshot;
        }
    }

    public readonly struct StatusEffectSnapshot
    {
        public StatusEffectKind Kind { get; }
        public int RemainingTurns { get; }
        public int Magnitude { get; }
        public int StackCount { get; }

        public StatusEffectSnapshot(
            StatusEffectKind kind,
            int remainingTurns,
            int magnitude,
            int stackCount)
        {
            Kind = kind;
            RemainingTurns = remainingTurns;
            Magnitude = magnitude;
            StackCount = stackCount;
        }
    }
}
