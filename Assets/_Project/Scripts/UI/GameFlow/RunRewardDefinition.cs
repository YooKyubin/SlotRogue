namespace SlotRogue.UI.GameFlow
{
    public sealed class RunRewardDefinition
    {
        public RunRewardDefinition(RunRewardType type, string displayName, string description)
        {
            Type = type;
            DisplayName = displayName;
            Description = description;
        }

        public RunRewardType Type { get; }

        public string DisplayName { get; }

        public string Description { get; }
    }
}
