namespace SlotRogue.Slot.Data
{
    public sealed class SlotCalculationResult
    {
        public static readonly SlotCalculationResult Empty = new(0, 0, 0, 0, false);

        public SlotCalculationResult(int damage, int defense, int attackCount, int healAmount, bool isCritical)
        {
            Damage = damage;
            Defense = defense;
            AttackCount = attackCount;
            HealAmount = healAmount;
            IsCritical = isCritical;
        }

        public int Damage { get; }

        public int Defense { get; }

        public int AttackCount { get; }

        public int HealAmount { get; }

        public bool IsCritical { get; }
    }
}
