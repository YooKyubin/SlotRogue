using System;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunEncounterRoster
    {
        public RunEncounterRoster(EnemyCombatant[] enemyCombatants, int[] formationSlots)
        {
            EnemyCombatants = enemyCombatants ?? Array.Empty<EnemyCombatant>();
            FormationSlots = formationSlots ?? Array.Empty<int>();
        }

        public EnemyCombatant[] EnemyCombatants { get; }

        public CombatParticipant[] Enemies
        {
            get
            {
                var enemies = new CombatParticipant[EnemyCombatants.Length];
                for (int index = 0; index < EnemyCombatants.Length; index++)
                {
                    enemies[index] = EnemyCombatants[index].Participant;
                }

                return enemies;
            }
        }

        public int[] FormationSlots { get; }
    }
}
