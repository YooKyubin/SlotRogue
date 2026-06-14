using System.Collections.Generic;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using SlotRogue.Data.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public static class RunEncounterRosterBuilder
    {
        public static int ResolveFormationSlot(RunEncounterRoster roster, int rosterIndex, int maxSlotCount)
        {
            if (roster == null || rosterIndex < 0 || rosterIndex >= roster.Enemies.Count)
            {
                return rosterIndex;
            }

            int slotCount = Mathf.Max(1, maxSlotCount);
            int formationSlot = roster.Enemies[rosterIndex].FormationSlot;
            if (formationSlot < 0 || formationSlot >= slotCount)
            {
                Debug.LogWarning(
                    $"[RunEncounterRosterBuilder] Formation slot {formationSlot} out of range; using roster index {rosterIndex}.");
                return Mathf.Clamp(rosterIndex, 0, slotCount - 1);
            }

            return formationSlot;
        }

        // ── 등급(Tier) 기반 생성 (무한모드) ──────────────────────────────
        // 등급 + 레벨만으로 적 한 마리를 구성합니다.
        // HP는 항상 등급/레벨 스케일이 결정하여 난이도 곡선을 보장합니다.

        public static RunEncounterRoster BuildForTier(
            EncounterTier tier,
            int level)
        {
            int maxHp = TierMaxHp(tier, level);
            var plannerFactory = new EnemyActionPlannerFactory();
            var combatantFactory = new EnemyCombatantFactory(plannerFactory);
            EnemyCombatant combatant = combatantFactory.Create(
                rosterIndex: 0,
                maxHp,
                plannerFactory.Create(TierTurnEffects(tier, level)));

            // Tier-based infinite mode currently builds enemies from EncounterTier + level,
            // so there is no MonsterDefinition to attach here yet. Keep the encounter unit
            // limited to runtime + formation until the monster selection path supplies one.
            return new RunEncounterRoster(new[] { new EnemyEncounterUnit(combatant, formationSlot: 0) });
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

        private static IReadOnlyList<IReadOnlyList<CombatEffect>> TierTurnEffects(EncounterTier tier, int level)
        {
            int lv = Mathf.Max(1, level);

            if (tier == EncounterTier.Boss)
            {
                return new[]
                {
                    new[] { new CombatEffect(CombatEffectKind.Damage, 6 + lv, CombatEffectTarget.Enemy) },
                    new[] { new CombatEffect(CombatEffectKind.Shield, 5 + lv, CombatEffectTarget.Self) },
                    new[] { new CombatEffect(CombatEffectKind.Damage, 9 + lv, CombatEffectTarget.Enemy) },
                };
            }

            if (tier == EncounterTier.Elite)
            {
                return new[]
                {
                    new[] { new CombatEffect(CombatEffectKind.Damage, 5 + lv, CombatEffectTarget.Enemy) },
                    new[] { new CombatEffect(CombatEffectKind.Shield, 4 + lv, CombatEffectTarget.Self) },
                    new[] { new CombatEffect(CombatEffectKind.Damage, 7 + lv, CombatEffectTarget.Enemy) },
                };
            }

            return new[]
            {
                new[] { new CombatEffect(CombatEffectKind.Damage, 3 + lv, CombatEffectTarget.Enemy) },
                new[] { new CombatEffect(CombatEffectKind.Shield, 4 + lv, CombatEffectTarget.Self) },
                new[] { new CombatEffect(CombatEffectKind.Damage, 5 + lv, CombatEffectTarget.Enemy) },
                new[] { new CombatEffect(CombatEffectKind.Heal, 1 + lv, CombatEffectTarget.Self) },
            };
        }
    }
}
