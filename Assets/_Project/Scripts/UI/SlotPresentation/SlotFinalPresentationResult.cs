namespace SlotRogue.UI.SlotPresentation
{
    public sealed class SlotFinalPresentationResult
    {
        public SlotFinalPresentationResult(
            int damage,
            int defense,
            int attackCount,
            int healAmount,
            string summaryText)
        {
            Damage = damage;
            Defense = defense;
            AttackCount = attackCount;
            HealAmount = healAmount;
            SummaryText = summaryText ?? string.Empty;
        }

        public int Damage { get; }

        public int Defense { get; }

        public int AttackCount { get; }

        public int HealAmount { get; }

        public string SummaryText { get; }
    }
}
