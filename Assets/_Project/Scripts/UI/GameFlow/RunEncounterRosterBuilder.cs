using System;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using SlotRogue.Data.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public static class RunEncounterRosterBuilder
    {
        public static RunEncounterRoster Build(RunMapNodeDefinition encounterNode, int floor)
        {
            if (encounterNode == null)
            {
                throw new ArgumentNullException(nameof(encounterNode));
            }

            RunEncounterDefinition encounter = encounterNode.Encounter;
            if (encounter != null && encounter.HasEntries)
            {
                return BuildFromEncounter(encounter, encounterNode, floor);
            }

            throw new InvalidOperationException(
                $"Run map node '{encounterNode.NodeId}' has no encounter definition. Assign a RunEncounterDefinition with at least one entry.");
        }

        private static RunEncounterRoster BuildFromEncounter(RunEncounterDefinition encounter, RunMapNodeDefinition encounterNode, int floor)
        {
            RunEncounterEntry[] entries = encounter.entries;
            int count = entries.Length;
            var enemies = new CombatParticipant[count];
            var schedules = new MonsterTurnSchedule[count];
            var formationSlots = new int[count];

            for (int index = 0; index < count; index++)
            {
                RunEncounterEntry entry = entries[index];
                int maxHp = ResolveEntryMaxHp(entry);
                enemies[index] = RunCombatParticipantFactory.CreateEnemy(index, maxHp);
                schedules[index] = ResolveEntryTurnSchedule(entry);
                formationSlots[index] = entry.formationSlot;
            }

            return new RunEncounterRoster(enemies, schedules, formationSlots);
        }

        private static int ResolveEntryMaxHp(RunEncounterEntry entry)
        {
            if (entry.monster == null)
            {
                Debug.LogError("missing : entry.monster");
                return 99999;
            }
            return Mathf.Max(1, entry.monster.maxHp);
        }

        private static MonsterTurnSchedule ResolveEntryTurnSchedule(RunEncounterEntry entry)
        {
            if (entry.monster == null || entry.monster.turnPattern == null)
            {
                Debug.LogError("missing : entry.monster || entry.monster.turnPattern");
            }

            return MonsterTurnScheduleFactory.FromPattern(entry.monster.turnPattern);
        }

        public static int ResolveFormationSlot(RunEncounterRoster roster, int rosterIndex, int maxSlotCount)
        {
            if (roster == null || rosterIndex < 0 || rosterIndex >= roster.Enemies.Length)
            {
                return rosterIndex;
            }

            int slotCount = Mathf.Max(1, maxSlotCount);
            if (roster.FormationSlots == null || rosterIndex >= roster.FormationSlots.Length)
            {
                return Mathf.Clamp(rosterIndex, 0, slotCount - 1);
            }

            int formationSlot = roster.FormationSlots[rosterIndex];
            if (formationSlot < 0 || formationSlot >= slotCount)
            {
                Debug.LogWarning(
                    $"[RunEncounterRosterBuilder] Formation slot {formationSlot} out of range; using roster index {rosterIndex}.");
                return Mathf.Clamp(rosterIndex, 0, slotCount - 1);
            }

            return formationSlot;
        }
    }
}
