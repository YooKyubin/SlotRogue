namespace SlotRogue.Core.Combat
{
    public readonly struct CombatEffectTarget
    {
        public CombatEffectTarget(CombatTargetMode mode, CombatParticipantId participantId = default)
        {
            Mode = mode;
            ParticipantId = participantId;
        }

        public CombatTargetMode Mode { get; }

        public CombatParticipantId ParticipantId { get; }

        public static CombatEffectTarget Self => new(CombatTargetMode.Self);

        public static CombatEffectTarget Enemy => new(CombatTargetMode.SelectedEnemy);

        public static CombatEffectTarget SelectedEnemy(CombatParticipantId participantId) =>
            new(CombatTargetMode.SelectedEnemy, participantId);
    }
}
