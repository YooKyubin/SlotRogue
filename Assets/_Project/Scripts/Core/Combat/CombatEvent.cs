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
            bool isPlayerParticipant = false)
        {
            Kind = kind;
            Phase = phase;
            Effect = effect;
            ApplyResult = applyResult;
            EndReason = endReason;
            IsPlayerParticipant = isPlayerParticipant;
        }

        public CombatEventKind Kind { get; }

        public BattlePhase Phase { get; }

        public CombatEffect Effect { get; }

        public EffectApplyResult ApplyResult { get; }

        public BattleEndReason EndReason { get; }

        public bool IsPlayerParticipant { get; }
    }
}
