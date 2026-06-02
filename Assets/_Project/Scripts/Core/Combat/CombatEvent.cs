namespace SlotRogue.Core.Combat
{
    public readonly struct CombatEvent
    {
        public CombatEvent(
            CombatEventKind kind,
            BattlePhase phase = BattlePhase.NotInBattle,
            CombatEffect effect = default,
            EffectApplyResult applyResult = default,
            BattleEndReason endReason = BattleEndReason.None,
            bool isPlayerParticipant = false,
            CombatParticipantId targetParticipantId = default,
            CombatParticipantSnapshot targetBefore = default,
            CombatParticipantSnapshot targetAfter = default)
        {
            Kind = kind;
            Phase = phase;
            Effect = effect;
            ApplyResult = applyResult;
            EndReason = endReason;
            IsPlayerParticipant = isPlayerParticipant;
            TargetParticipantId = targetParticipantId;
            TargetBefore = targetBefore;
            TargetAfter = targetAfter;
        }

        public CombatEventKind Kind { get; }

        public BattlePhase Phase { get; }

        public CombatEffect Effect { get; }

        public EffectApplyResult ApplyResult { get; }

        public BattleEndReason EndReason { get; }

        public bool IsPlayerParticipant { get; }

        public CombatParticipantId TargetParticipantId { get; }

        public CombatParticipantSnapshot TargetBefore { get; }

        public CombatParticipantSnapshot TargetAfter { get; }

        public bool HasTargetSnapshot => Kind == CombatEventKind.EffectApplied;
    }
}
