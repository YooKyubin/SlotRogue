namespace SlotRogue.UI.GameFlow
{
    using UnityEngine;

    public readonly struct EnemyUpcomingActionViewData
    {
        public EnemyUpcomingActionViewData(EnemyUpcomingActionKind kind, int amount)
            : this(kind, amount, string.Empty, null)
        {
        }

        public EnemyUpcomingActionViewData(
            EnemyUpcomingActionKind kind,
            int amount,
            string displayName,
            Sprite intentIcon)
        {
            Kind = kind;
            Amount = amount;
            DisplayName = displayName ?? string.Empty;
            IntentIcon = intentIcon;
        }

        public EnemyUpcomingActionKind Kind { get; }

        public int Amount { get; }

        public string DisplayName { get; }

        public Sprite IntentIcon { get; }
    }
}
