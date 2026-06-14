namespace SlotRogue.Core.Combat
{
    public readonly struct EnemyUpcomingTurn
    {
        public EnemyUpcomingTurn(
            CombatParticipantId participantId,
            int turnIndex,
            EnemyActionPlan plan)
        {
            ParticipantId = participantId;
            TurnIndex = turnIndex;
            Plan = plan ?? new EnemyActionPlan(null);
        }

        public CombatParticipantId ParticipantId { get; }

        public int TurnIndex { get; }

        public EnemyActionPlan Plan { get; }
    }
}
