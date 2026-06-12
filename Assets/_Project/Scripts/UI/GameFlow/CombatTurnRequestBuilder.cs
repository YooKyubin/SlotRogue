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

            SlotCombatRequest finalRequest = new(
                normalizedRequest.Damage + relicDamage +
                (normalizedRequest.Damage + relicDamage > 0 ? runDamageBonus : 0),
                normalizedRequest.Defense + relicBlock + runDefenseBonus,
                normalizedRequest.AttackCount,
                normalizedRequest.HealAmount + relicHeal,
                normalizedRequest.IsCritical,
                normalizedRequest.PatternName);

            return new RunCombatRequestResult(
                normalizedRequest,
                finalRequest,
                relicResult?.ActivationSummary,
                runBonusSummary,
                BuildStatusEffectSpecs(relicResult?.StatusEffectsToApply));
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
