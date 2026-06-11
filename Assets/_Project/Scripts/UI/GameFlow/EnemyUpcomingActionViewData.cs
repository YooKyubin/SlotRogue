namespace SlotRogue.UI.GameFlow
{
    public readonly struct EnemyUpcomingActionViewData
    {
        public EnemyUpcomingActionViewData(EnemyUpcomingActionKind kind, int amount)
        {
            Kind = kind;
            Amount = amount;
        }

        public EnemyUpcomingActionKind Kind { get; }

        public int Amount { get; }
    }
}
