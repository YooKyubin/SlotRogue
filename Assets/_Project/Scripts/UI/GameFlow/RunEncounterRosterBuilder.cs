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
                enemies[index] = new CombatParticipant(maxHp, id: new CombatParticipantId(100 + index), team: CombatTeam.Enemy);
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

        // ── 등급(Tier) 기반 생성 (무한모드) ──────────────────────────────
        // RunMapNodeDefinition 없이 등급 + 레벨만으로 적 한 마리를 구성합니다.
        // 적 패턴은 fallback 몬스터가 있으면 그 패턴을, 없으면 등급 기본 패턴을 씁니다.
        // HP는 항상 등급/레벨 스케일이 결정하여 난이도 곡선을 보장합니다.

        public static RunEncounterRoster BuildForTier(
            EncounterTier tier,
            int level,
            MonsterDefinition fallback)
        {
            int maxHp = TierMaxHp(tier, level);
            var enemies = new[]
            {
                new CombatParticipant(maxHp, id: new CombatParticipantId(100), team: CombatTeam.Enemy),
            };

            MonsterTurnSchedule schedule =
                fallback != null && fallback.turnPattern != null
                    ? MonsterTurnScheduleFactory.FromPattern(fallback.turnPattern)
                    : TierTurnSchedule(tier, level);

            return new RunEncounterRoster(enemies, new[] { schedule }, new[] { 0 });
        }

        public static EncounterTier ToTier(RunMapNodeType nodeType)
        {
            return nodeType switch
            {
                RunMapNodeType.Elite => EncounterTier.Elite,
                RunMapNodeType.Boss => EncounterTier.Boss,
                _ => EncounterTier.Normal,
            };
        }

        private static int TierMaxHp(EncounterTier tier, int level)
        {
            int lv = Mathf.Max(1, level);

            return tier switch
            {
                EncounterTier.Elite => 32 + (lv * 8),
                EncounterTier.Boss => 46 + (lv * 10),
                _ => 22 + (lv * 6),
            };
        }

        private static MonsterTurnSchedule TierTurnSchedule(EncounterTier tier, int level)
        {
            int lv = Mathf.Max(1, level);

            if (tier == EncounterTier.Boss)
            {
                return new MonsterTurnSchedule(
                    new[] { new CombatEffect(CombatEffectKind.Damage, 6 + lv, CombatEffectTarget.Enemy) },
                    new[] { new CombatEffect(CombatEffectKind.Shield, 5 + lv, CombatEffectTarget.Self) },
                    new[] { new CombatEffect(CombatEffectKind.Damage, 9 + lv, CombatEffectTarget.Enemy) });
            }

            if (tier == EncounterTier.Elite)
            {
                return new MonsterTurnSchedule(
                    new[] { new CombatEffect(CombatEffectKind.Damage, 5 + lv, CombatEffectTarget.Enemy) },
                    new[] { new CombatEffect(CombatEffectKind.Shield, 4 + lv, CombatEffectTarget.Self) },
                    new[] { new CombatEffect(CombatEffectKind.Damage, 7 + lv, CombatEffectTarget.Enemy) });
            }

            return new MonsterTurnSchedule(
                new[] { new CombatEffect(CombatEffectKind.Damage, 3 + lv, CombatEffectTarget.Enemy) },
                new[] { new CombatEffect(CombatEffectKind.Shield, 2 + lv, CombatEffectTarget.Self) },
                new[] { new CombatEffect(CombatEffectKind.Damage, 5 + lv, CombatEffectTarget.Enemy) });
        }
    }
}
