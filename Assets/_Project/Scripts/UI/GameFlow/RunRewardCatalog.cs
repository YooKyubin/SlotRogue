using System.Collections.Generic;

namespace SlotRogue.UI.GameFlow
{
    public static class RunRewardCatalog
    {
        private static readonly RunRewardDefinition[] AllDefinitions =
        {
            new(RunRewardType.Heal, "Field Ration", "Recover 8 HP."),
            new(RunRewardType.DamageBonus, "Sharpening Stone", "Future spins gain +2 damage."),
            new(RunRewardType.DefenseBonus, "Guard Polish", "Future spins gain +2 defense."),
        };

        public static IReadOnlyList<RunRewardDefinition> All => AllDefinitions;
    }
}
