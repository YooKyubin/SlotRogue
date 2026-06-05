using System.Collections.Generic;

namespace SlotRogue.UI.GameFlow
{
    public static class RunRewardCatalog
    {
        private static readonly RunRewardDefinition[] AllDefinitions =
        {
            new(RunRewardType.Heal, "전투 식량", "HP 8 회복."),
            new(RunRewardType.DamageBonus, "숫돌", "이후 스핀에서 피해 +2."),
            new(RunRewardType.DefenseBonus, "방어 연마제", "이후 스핀에서 방어 +2."),
        };

        public static IReadOnlyList<RunRewardDefinition> All => AllDefinitions;
    }
}
