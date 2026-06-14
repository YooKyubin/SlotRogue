using System;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunEncounterRoster
    {
        public RunEncounterRoster(EnemyRuntime[] enemyRuntimes, int[] formationSlots)
        {
            EnemyRuntimes = enemyRuntimes ?? Array.Empty<EnemyRuntime>();
            FormationSlots = formationSlots ?? Array.Empty<int>();
        }

        public EnemyRuntime[] EnemyRuntimes { get; }

        public CombatParticipant[] Enemies
        {
            get
            {
                var enemies = new CombatParticipant[EnemyRuntimes.Length];
                for (int index = 0; index < EnemyRuntimes.Length; index++)
                {
                    enemies[index] = EnemyRuntimes[index].Participant;
                }

                return enemies;
            }
        }

        public int[] FormationSlots { get; }
    }
}
