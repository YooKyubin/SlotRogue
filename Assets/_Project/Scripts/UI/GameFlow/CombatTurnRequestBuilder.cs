using System;
using System.Collections.Generic;
using System.Text;
using SlotRogue.Core.Combat;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    public sealed class CombatTurnRequestBuilder
    {
        public RunCombatRequestResult Build(
            SlotCombatRequest baseRequest,
            RelicResolveResult relicResult,
            int runDamageBonus,
            int runDefenseBonus)
        {
            SlotCombatRequest normalizedRequest = NormalizeBlankTurn(baseRequest);
            int relicDamage = relicResult?.AdditionalDamage ?? 0;
            int relicBlock = relicResult?.AdditionalBlock ?? 0;
            int relicHeal = relicResult?.HealAmount ?? 0;

            string runBonusSummary = BuildRunBonusSummary(runDamageBonus, runDefenseBonus);

            int finalDamage = normalizedRequest.Damage + relicDamage +
                (normalizedRequest.Damage + relicDamage > 0 ? runDamageBonus : 0);
            int finalDefense = normalizedRequest.Defense + relicBlock + runDefenseBonus;

            // 흡혈/방어전환은 최종 피해·방어가 확정된 지금에야 회복량을 계산할 수 있다.
            var derivedHealContributions = new List<RelicContributionDelta>();
            int derivedHeal = ResolveDerivedHeals(
                relicResult?.DerivedHeals,
                finalDamage * Math.Max(1, normalizedRequest.AttackCount),
                finalDefense,
                derivedHealContributions);

            SlotCombatRequest finalRequest = new(
                finalDamage,
                finalDefense,
                normalizedRequest.AttackCount,
                normalizedRequest.HealAmount + relicHeal + derivedHeal,
                normalizedRequest.IsCritical,
                normalizedRequest.PatternName);

            return new RunCombatRequestResult(
                normalizedRequest,
                finalRequest,
                relicResult?.ActivationSummary,
                runBonusSummary,
                BuildStatusEffectSpecs(relicResult?.StatusEffectsToApply),
                derivedHealContributions);
        }

        // 흡혈: 입힌 피해의 Percent%(턴당 TurnCap 상한). 방어전환: 획득 방어도의 Percent%.
        // 각 규칙은 독립적으로 계산해 합산하며, 줄 단위가 아니라 턴 단위로 1회만 적용한다.
        private static int ResolveDerivedHeals(
            IReadOnlyList<RelicDerivedHeal> derivedHeals,
            int totalOutgoingDamage,
            int gainedDefense,
            List<RelicContributionDelta> contributions)
        {
            if (derivedHeals == null || derivedHeals.Count == 0)
            {
                return 0;
            }

            int total = 0;
            for (int index = 0; index < derivedHeals.Count; index++)
            {
                RelicDerivedHeal rule = derivedHeals[index];
                int sourceAmount = rule.Kind == RelicDerivedHealKind.Lifesteal
                    ? Math.Max(0, totalOutgoingDamage)
                    : Math.Max(0, gainedDefense);

                int heal = Percentage(sourceAmount, rule.Percent);
                if (rule.TurnCap > 0)
                {
                    heal = Math.Min(heal, rule.TurnCap);
                }

                if (heal <= 0)
                {
                    continue;
                }

                total += heal;
                contributions.Add(new RelicContributionDelta(
                    rule.RelicId,
                    rule.RelicName,
                    damagePerHit: 0,
                    block: 0,
                    heal: heal,
                    triggerPatternIndex: rule.TriggerPatternIndex));
            }

            return total;
        }

        private static int Percentage(int amount, int percent)
        {
            if (amount <= 0 || percent <= 0)
            {
                return 0;
            }

            return (int)((long)amount * percent / 100);
        }

        private static SlotCombatRequest NormalizeBlankTurn(SlotCombatRequest request)
        {
            if (request == null || HasNoEffects(request))
            {
                return new SlotCombatRequest(
                    SlotCombatRequest.BaseAttackDamage,
                    0,
                    SlotCombatRequest.BaseAttackCount,
                    0,
                    false,
                    SlotCombatRequest.BaseAttackName);
            }

            return request;
        }

        private static bool HasNoEffects(SlotCombatRequest request) =>
            request.Damage <= 0 &&
            request.Defense <= 0 &&
            request.HealAmount <= 0;

        private static IReadOnlyList<StatusEffectSpec> BuildStatusEffectSpecs(
            IReadOnlyList<StatusEffectRequest> requests)
        {
            if (requests == null || requests.Count == 0)
            {
                return Array.Empty<StatusEffectSpec>();
            }

            int burnStacks = 0;
            int poisonStacks = 0;
            int freezeApplications = 0;

            for (int index = 0; index < requests.Count; index++)
            {
                StatusEffectRequest request = requests[index];
                switch (request.Kind)
                {
                    case StatusEffectKind.Burn:
                        burnStacks += request.Stacks;
                        break;
                    case StatusEffectKind.Poison:
                        poisonStacks += request.Stacks;
                        break;
                    case StatusEffectKind.Freeze:
                        freezeApplications += request.Stacks;
                        break;
                }
            }

            var specs = new List<StatusEffectSpec>(3);
            if (burnStacks > 0)
            {
                specs.Add(new StatusEffectSpec(
                    StatusEffectKind.Burn,
                    duration: 1,
                    magnitude: burnStacks,
                    stackMode: StatusStackMode.Refresh));
            }

            if (poisonStacks > 0)
            {
                specs.Add(new StatusEffectSpec(
                    StatusEffectKind.Poison,
                    duration: 0,
                    magnitude: poisonStacks,
                    stackMode: StatusStackMode.Stack));
            }

            if (freezeApplications > 0)
            {
                specs.Add(new StatusEffectSpec(
                    StatusEffectKind.Freeze,
                    duration: freezeApplications,
                    magnitude: 0,
                    stackMode: StatusStackMode.Refresh));
            }

            return specs;
        }

        private static string BuildRunBonusSummary(int runDamageBonus, int runDefenseBonus)
        {
            var builder = new StringBuilder();

            if (runDamageBonus > 0)
            {
                builder.Append($"피해 +{runDamageBonus}");
            }

            if (runDefenseBonus > 0)
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append($"방어 +{runDefenseBonus}");
            }

            return builder.ToString();
        }
    }
}
