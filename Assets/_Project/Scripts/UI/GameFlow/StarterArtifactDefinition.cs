using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    public sealed class StarterArtifactDefinition
    {
        public StarterArtifactDefinition(
            StarterArtifactId id,
            string displayName,
            string description,
            SlotSymbolType targetSymbol,
            int minimumMatchLength,
            int bonusDamage,
            int bonusDefense,
            int bonusHeal)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            TargetSymbol = targetSymbol;
            MinimumMatchLength = minimumMatchLength;
            BonusDamage = bonusDamage;
            BonusDefense = bonusDefense;
            BonusHeal = bonusHeal;
        }

        public StarterArtifactId Id { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public SlotSymbolType TargetSymbol { get; }

        public int MinimumMatchLength { get; }

        public int BonusDamage { get; }

        public int BonusDefense { get; }

        public int BonusHeal { get; }
    }
}
