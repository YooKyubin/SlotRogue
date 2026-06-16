using System;
using System.Collections.Generic;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunEncounterRoster
    {
        public RunEncounterRoster(IReadOnlyList<EnemyEncounterUnit> enemies)
        {
            if (enemies == null || enemies.Count == 0)
            {
                Enemies = Array.Empty<EnemyEncounterUnit>();
                return;
            }

            var copy = new EnemyEncounterUnit[enemies.Count];
            for (int index = 0; index < enemies.Count; index++)
            {
                copy[index] = enemies[index] ?? throw new ArgumentException("Enemy encounter units cannot contain null entries.", nameof(enemies));
            }

            Enemies = copy;
        }

        public IReadOnlyList<EnemyEncounterUnit> Enemies { get; }
    }
}
