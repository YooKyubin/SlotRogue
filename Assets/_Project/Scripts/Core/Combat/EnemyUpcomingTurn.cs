namespace SlotRogue.Core.Combat
{
    public readonly struct EnemyUpcomingTurn
    {
        public EnemyUpcomingTurn(
            CombatParticipantId participantId,
            EnemyActionPlan plan)
        {
            ParticipantId = participantId;
            Plan = plan ?? new EnemyActionPlan(null);
        }

        public CombatParticipantId ParticipantId { get; }

        public EnemyActionPlan Plan { get; }
    }
}
