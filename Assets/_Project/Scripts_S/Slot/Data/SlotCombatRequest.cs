namespace SlotRogue.Slot.Data
{
    public sealed class SlotCombatRequest
    {
        public const int BaseAttackDamage = 4;
        public const int BaseAttackCount = 1;
        public const string BaseAttackName = "기본 공격";

        public static readonly SlotCombatRequest Empty = new(0, 0, 0, 0, false, "매치 없음");

        public SlotCombatRequest(
            int damage,
            int defense,
            int attackCount,
            int healAmount,
            bool isCritical,
            string patternName)
        {
            Damage = damage;
            Defense = defense;
            AttackCount = attackCount;
            HealAmount = healAmount;
            IsCritical = isCritical;
            PatternName = patternName;
        }

        public int Damage { get; }

        public int Defense { get; }

        public int AttackCount { get; }

        public int HealAmount { get; }

        public bool IsCritical { get; }

        public string PatternName { get; }
    }
}
