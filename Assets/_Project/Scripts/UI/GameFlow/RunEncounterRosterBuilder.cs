using System;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using SlotRogue.Data.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public static class RunEncounterRosterBuilder
    {
        public static RunEncounterRoster Build(
            RunMapNodeDefinition encounterNode,
            int floor,
            MonsterDefinition inspectorFallback)
        {
            if (encounterNode == null)
            {
                throw new ArgumentNullException(nameof(encounterNode));
            }

            RunEncounterDefinition encounter = encounterNode.Encounter;
            if (encounter != null && encounter.HasEntries)
            {
                return BuildFromEncounter(encounter, encounterNode, floor, inspectorFallback);
            }

            throw new InvalidOperationException(
                $"Run map node '{encounterNode.NodeId}' has no encounter definition. Assign a RunEncounterDefinition with at least one entry.");
        }

        private static RunEncounterRoster BuildFromEncounter(
            RunEncounterDefinition encounter,
            RunMapNodeDefinition encounterNode,
            int floor,
            MonsterDefinition inspectorFallback)
        {
            RunEncounterEntry[] entries = encounter.entries;
            int count = entries.Length;
            var enemies = new CombatParticipant[count];
            var schedules = new MonsterTurnSchedule[count];
            var formationSlots = new int[count];

            for (int index = 0; index < count; index++)
            {
                RunEncounterEntry entry = entries[index];
                int maxHp = ResolveEntryMaxHp(entry, encounterNode, inspectorFallback);
                enemies[index] = new CombatParticipant(
                    maxHp,
                    id: new CombatParticipantId(100 + index),
                    team: CombatTeam.Enemy);
                schedules[index] = ResolveEntryTurnSchedule(entry, encounterNode, floor, inspectorFallback);
                formationSlots[index] = entry.formationSlot;
            }

            return new RunEncounterRoster(enemies, schedules, formationSlots);
        }

        private static int ResolveEntryMaxHp(
            RunEncounterEntry entry,
            RunMapNodeDefinition encounterNode,
            MonsterDefinition inspectorFallback)
        {
            if (entry.monster != null)
            {
                return Mathf.Max(1, entry.monster.maxHp);
            }

            return ResolveLegacyMaxHp(encounterNode, inspectorFallback);
        }

        private static MonsterTurnSchedule ResolveEntryTurnSchedule(
            RunEncounterEntry entry,
            RunMapNodeDefinition encounterNode,
            int floor,
            MonsterDefinition inspectorFallback)
        {
            if (entry.turnPatternOverride != null)
            {
                return MonsterTurnScheduleFactory.FromPattern(entry.turnPatternOverride);
            }

            if (entry.monster != null && entry.monster.turnPattern != null)
            {
                return MonsterTurnScheduleFactory.FromPattern(entry.monster.turnPattern);
            }

            return ResolveLegacyTurnSchedule(encounterNode, floor, inspectorFallback);
        }

        private static int ResolveLegacyMaxHp(
            RunMapNodeDefinition encounterNode,
            MonsterDefinition inspectorFallback)
        {
            if (inspectorFallback != null)
            {
                return Mathf.Max(1, inspectorFallback.maxHp);
            }

            return GetMonsterMaxHp(encounterNode);
        }

        private static MonsterTurnSchedule ResolveLegacyTurnSchedule(
            RunMapNodeDefinition encounterNode,
            int floor,
            MonsterDefinition inspectorFallback)
        {
            if (inspectorFallback != null && inspectorFallback.turnPattern != null)
            {
                return MonsterTurnScheduleFactory.FromPattern(inspectorFallback.turnPattern);
            }

            return CreateMonsterTurnSchedule(encounterNode, floor);
        }

        public static int ResolveFormationSlot(
            RunEncounterRoster roster,
            int rosterIndex,
            int maxSlotCount)
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

        private static int GetMonsterMaxHp(RunMapNodeDefinition encounterNode)
        {
            int floor = Mathf.Max(1, encounterNode.Floor);

            switch (encounterNode.NodeType)
            {
                case RunMapNodeType.Elite:
                    return 32 + (floor * 8);
                case RunMapNodeType.Boss:
                    return 46 + (floor * 10);
                default:
                    return 22 + (floor * 6);
            }
        }

        private static MonsterTurnSchedule CreateMonsterTurnSchedule(
            RunMapNodeDefinition encounterNode,
            int floor)
        {
            if (encounterNode.NodeType == RunMapNodeType.Boss)
            {
                return new MonsterTurnSchedule(
                    new[] { new CombatEffect(CombatEffectKind.Damage, 6 + floor, CombatEffectTarget.Enemy) },
                    new[] { new CombatEffect(CombatEffectKind.Shield, 5 + floor, CombatEffectTarget.Self) },
                    new[] { new CombatEffect(CombatEffectKind.Damage, 9 + floor, CombatEffectTarget.Enemy) });
            }

            if (encounterNode.NodeType == RunMapNodeType.Elite)
            {
                return new MonsterTurnSchedule(
                    new[] { new CombatEffect(CombatEffectKind.Damage, 5 + floor, CombatEffectTarget.Enemy) },
                    new[] { new CombatEffect(CombatEffectKind.Shield, 4 + floor, CombatEffectTarget.Self) },
                    new[] { new CombatEffect(CombatEffectKind.Damage, 7 + floor, CombatEffectTarget.Enemy) });
            }

            return new MonsterTurnSchedule(
                new[] { new CombatEffect(CombatEffectKind.Damage, 3 + floor, CombatEffectTarget.Enemy) },
                new[] { new CombatEffect(CombatEffectKind.Shield, 2 + floor, CombatEffectTarget.Self) },
                new[] { new CombatEffect(CombatEffectKind.Damage, 5 + floor, CombatEffectTarget.Enemy) });
        }
    }
}
