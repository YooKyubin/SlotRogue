using System.Collections.Generic;
using System.Text;
using SlotRogue.Core.Combat;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 보유 유물 전체의 조건을 이번 스핀 족보 결과에 대해 검사하고, "이번 턴 전투에 넘길 값"만 계산한다.
    /// 전투를 직접 실행하지 않으며, 전투 코어(BattleSystem/StatusEffectEngine)도 건드리지 않는다.
    /// 효과 분기는 문자열이 아니라 <see cref="RelicEffectType"/>로만 처리한다.
    ///
    /// Phase 1 계산 대상: AddDamage / AddBlock / Heal.
    /// 화상/감염/취약/약화/가시/흡혈은 v23 전투 규칙이 코어에 갖춰질 때까지 계산하지 않는다
    /// (해당 유물은 카탈로그에서 Phase1=false라 보상풀에도 등장하지 않는다).
    /// </summary>
    public sealed class RelicEffectRunner
    {
        public RelicResolveResult Resolve(
            IReadOnlyList<SlotPatternMatch> patternMatches,
            IReadOnlyList<RelicDefinition> ownedRelics,
            RelicBattleContext context)
        {
            int additionalDamage = 0;
            int additionalBlock = 0;
            int healAmount = 0;
            var statuses = new List<StatusEffectRequest>();
            var activated = new List<string>();

            if (ownedRelics != null)
            {
                for (int index = 0; index < ownedRelics.Count; index++)
                {
                    RelicDefinition relic = ownedRelics[index];
                    if (relic == null || !relic.Phase1)
                    {
                        continue;
                    }

                    if (!IsTriggered(relic, patternMatches, context))
                    {
                        continue;
                    }

                    bool applied = ApplyEffect(
                        relic, context, ref additionalDamage, ref additionalBlock, ref healAmount, statuses);
                    if (applied)
                    {
                        activated.Add(relic.Name);
                    }
                }
            }

            return new RelicResolveResult(
                additionalDamage, additionalBlock, healAmount, statuses, BuildSummary(activated));
        }

        // ── 조건 판정 ────────────────────────────────────────────────────

        private static bool IsTriggered(
            RelicDefinition relic,
            IReadOnlyList<SlotPatternMatch> patternMatches,
            RelicBattleContext context)
        {
            switch (relic.TriggerType)
            {
                case RelicTriggerType.MatchSymbol:
                    if (!relic.TriggerSymbol.HasValue ||
                        !AnyPatternOfSymbol(patternMatches, relic.TriggerSymbol.Value, relic.RequiredCount))
                    {
                        return false;
                    }

                    break;

                case RelicTriggerType.MatchTag:
                    if (!relic.TriggerTag.HasValue ||
                        TagCellCount(patternMatches, relic.TriggerTag.Value) < relic.RequiredCount)
                    {
                        return false;
                    }

                    break;

                case RelicTriggerType.Conditional:
                    // Phase 1에서 지원하는 조건부는 "상태이상 적 + 피해 족보"(U-13/U-16/U-17)뿐.
                    if (!MeetsEnemyStatusRequirement(relic.EnemyStatusRequirement, context))
                    {
                        return false;
                    }

                    if (PatternCount(patternMatches) == 0)
                    {
                        return false;
                    }

                    break;

                default:
                    return false; // Passive / BattleStart / BattleEnd / Reward 는 Phase 1 비대상.
            }

            if (relic.EnemyHpBelowPercent > 0 &&
                !IsHpBelow(context.EnemyCurrentHp, context.EnemyMaxHp, relic.EnemyHpBelowPercent))
            {
                return false;
            }

            return true;
        }

        // ── 효과 계산 (값만 만든다) ──────────────────────────────────────

        private static bool ApplyEffect(
            RelicDefinition relic,
            RelicBattleContext context,
            ref int additionalDamage,
            ref int additionalBlock,
            ref int healAmount,
            List<StatusEffectRequest> statuses)
        {
            switch (relic.EffectType)
            {
                case RelicEffectType.AddDamage:
                    additionalDamage += relic.EffectValue;

                    // U-13 상태 추격자: 적이 화상/감염 상태면 보조 수치를 추가한다.
                    if (relic.EffectValue2 > 0 &&
                        relic.EnemyStatusRequirement == EnemyStatusRequirement.Any &&
                        (context.EnemyHasBurn || context.EnemyHasInfect))
                    {
                        additionalDamage += relic.EffectValue2;
                    }

                    return true;

                case RelicEffectType.AddBlock:
                    additionalBlock += relic.EffectValue;
                    return true;

                case RelicEffectType.Heal:
                    healAmount += relic.EffectValue;
                    if (relic.PlayerHpBelowPercentForBonus > 0 &&
                        IsHpBelow(context.PlayerCurrentHp, context.PlayerMaxHp, relic.PlayerHpBelowPercentForBonus))
                    {
                        healAmount += relic.EffectValue2;
                    }

                    return true;

                case RelicEffectType.ApplyBurn:
                case RelicEffectType.ApplyInfect:
                case RelicEffectType.ApplyVulnerable:
                case RelicEffectType.ApplyWeak:
                case RelicEffectType.GainThorns:
                    // 상태이상 계열은 전투 코어의 v23 동작이 준비될 때까지 실행하지 않는다.
                    Debug.LogWarning(
                        $"[Relic] Unsupported status effect in Phase 1: {relic.EffectType} ({relic.Id}). Skipped.");
                    return false;

                default:
                    // Phase 2 효과(배율/보상 변형/부활 등)는 계산하지 않는다.
                    return false;
            }
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────────

        private static bool MeetsEnemyStatusRequirement(
            EnemyStatusRequirement requirement, RelicBattleContext context)
        {
            switch (requirement)
            {
                case EnemyStatusRequirement.Any:
                    // v23 U-13은 화상/감염/취약/약화만 대상으로 하며 Freeze는 포함하지 않는다.
                    return context.EnemyHasBurn || context.EnemyHasInfect;
                case EnemyStatusRequirement.Burn:
                    return context.EnemyHasBurn;
                case EnemyStatusRequirement.Infect:
                    return context.EnemyHasInfect;
                default:
                    return true;
            }
        }

        private static bool AnyPatternOfSymbol(
            IReadOnlyList<SlotPatternMatch> matches, SlotSymbolType symbol, int requiredCount)
        {
            if (matches == null)
            {
                return false;
            }

            int minLength = requiredCount < 1 ? 1 : requiredCount;
            for (int index = 0; index < matches.Count; index++)
            {
                SlotPatternMatch match = matches[index];
                if (match?.MatchedCells == null)
                {
                    continue;
                }

                if (match.Symbol == symbol && match.MatchedCells.Count >= minLength)
                {
                    return true;
                }
            }

            return false;
        }

        // 겹치는 족보가 같은 칸을 공유해도 태그 심볼 한 칸은 한 번만 센다.
        private static int TagCellCount(IReadOnlyList<SlotPatternMatch> matches, SymbolTag tag)
        {
            if (matches == null)
            {
                return 0;
            }

            var matchedCells = new HashSet<SlotCell>();
            for (int index = 0; index < matches.Count; index++)
            {
                SlotPatternMatch match = matches[index];
                if (match?.MatchedCells == null)
                {
                    continue;
                }

                if (SymbolTagMap.HasTag(match.Symbol, tag))
                {
                    for (int cellIndex = 0; cellIndex < match.MatchedCells.Count; cellIndex++)
                    {
                        matchedCells.Add(match.MatchedCells[cellIndex]);
                    }
                }
            }

            return matchedCells.Count;
        }

        private static int PatternCount(IReadOnlyList<SlotPatternMatch> matches) => matches?.Count ?? 0;

        private static bool IsHpBelow(int currentHp, int maxHp, int percent)
        {
            if (maxHp <= 0 || percent <= 0)
            {
                return false;
            }

            return currentHp * 100 <= maxHp * percent;
        }

        private static string BuildSummary(List<string> activatedNames)
        {
            if (activatedNames.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (int index = 0; index < activatedNames.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(activatedNames[index]);
            }

            return builder.ToString();
        }
    }

    /// <summary>유물 조건 판정에 필요한 전투 상태 스냅샷(읽기 전용 값).</summary>
    public readonly struct RelicBattleContext
    {
        public RelicBattleContext(
            int playerCurrentHp,
            int playerMaxHp,
            int enemyCurrentHp,
            int enemyMaxHp,
            bool enemyHasAnyStatus,
            bool enemyHasBurn = false,
            bool enemyHasInfect = false)
        {
            PlayerCurrentHp = playerCurrentHp;
            PlayerMaxHp = playerMaxHp;
            EnemyCurrentHp = enemyCurrentHp;
            EnemyMaxHp = enemyMaxHp;
            EnemyHasAnyStatus = enemyHasAnyStatus;
            EnemyHasBurn = enemyHasBurn;
            EnemyHasInfect = enemyHasInfect;
        }

        public int PlayerCurrentHp { get; }
        public int PlayerMaxHp { get; }
        public int EnemyCurrentHp { get; }
        public int EnemyMaxHp { get; }
        public bool EnemyHasAnyStatus { get; }

        /// <summary>적이 화상(Burn) 상태인지(U-13/U-16용).</summary>
        public bool EnemyHasBurn { get; }

        /// <summary>적이 감염(전투에서는 Poison) 상태인지(U-13/U-17용).</summary>
        public bool EnemyHasInfect { get; }
    }
}
