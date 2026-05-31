namespace SlotRogue.Core.Combat
{
    public sealed class CombatParticipant
    {
        public CombatParticipant(int maxHp, int currentHp = -1, int shield = 0)
        {
            MaxHp = maxHp;
            CurrentHp = currentHp < 0 ? maxHp : currentHp;
            Shield = shield;
        }

        public int MaxHp { get; }

        public int CurrentHp { get; internal set; }

        public int Shield { get; internal set; }

        public bool IsDead => CurrentHp <= 0;
    }
}
