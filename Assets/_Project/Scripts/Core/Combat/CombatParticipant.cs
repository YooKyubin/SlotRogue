namespace SlotRogue.Core.Combat
{
    public sealed class CombatParticipant
    {
        public CombatParticipant(
            int maxHp,
            int currentHp = -1,
            int shield = 0,
            CombatParticipantId id = default,
            CombatTeam team = CombatTeam.None)
        {
            MaxHp = maxHp;
            CurrentHp = currentHp < 0 ? maxHp : currentHp;
            Shield = shield;
            Id = id;
            Team = team;
        }

        public CombatParticipantId Id { get; }

        public CombatTeam Team { get; }

        public int MaxHp { get; }

        public int CurrentHp { get; internal set; }

        public int Shield { get; internal set; }

        public bool IsDead => CurrentHp <= 0;
    }
}
