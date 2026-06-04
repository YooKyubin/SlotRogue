using System;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunEncounterRoster
    {
        public RunEncounterRoster(
            CombatParticipant[] enemies,
            MonsterTurnSchedule[] schedules,
            int[] formationSlots)
        {
            Enemies = enemies ?? Array.Empty<CombatParticipant>();
            Schedules = schedules ?? Array.Empty<MonsterTurnSchedule>();
            FormationSlots = formationSlots ?? Array.Empty<int>();
        }

        public CombatParticipant[] Enemies { get; }

        public MonsterTurnSchedule[] Schedules { get; }

        public int[] FormationSlots { get; }
    }
}
