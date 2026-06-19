using System;
using System.Collections.Generic;

namespace SlotRogue.UI.GameFlow
{
    public static class EnemyFormationLayout
    {
        public static IReadOnlyList<int> ResolveSlots(int monsterCount)
        {
            return monsterCount switch
            {
                1 => new[] { 1 },
                2 => new[] { 0, 2 },
                3 => new[] { 0, 1, 2 },
                _ => throw new ArgumentOutOfRangeException(
                    nameof(monsterCount),
                    monsterCount,
                    "Enemy formation layout supports 1 to 3 monsters."),
            };
        }
    }
}
