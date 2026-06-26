using System;
using System.Collections.Generic;
using SlotRogue.Slot.Data;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunCombatRequestResult
    {
        public RunCombatRequestResult(
            SlotCombatRequest baseRequest,
            SlotCombatRequest finalRequest,
            string relicActivationSummary,
            string runBonusSummary,
            IReadOnlyList<TargetedStatusEffectSpec> statusEffectsToApply = null,
            IReadOnlyList<RelicContributionDelta> derivedHealContributions = null)
        {
            BaseRequest = baseRequest;
            FinalRequest = finalRequest;
            AttackPower = CalculateAttackPower(finalRequest);
            RelicActivationSummary = relicActivationSummary ?? string.Empty;
            RunBonusSummary = runBonusSummary ?? string.Empty;
            StatusEffectsToApply = statusEffectsToApply ?? Array.Empty<TargetedStatusEffectSpec>();
            DerivedHealContributions =
                derivedHealContributions ?? Array.Empty<RelicContributionDelta>();
        }

        public SlotCombatRequest BaseRequest { get; }

        public SlotCombatRequest FinalRequest { get; }

        public int AttackPower { get; }

        public string RelicActivationSummary { get; }

        public string RunBonusSummary { get; }

        public IReadOnlyList<TargetedStatusEffectSpec> StatusEffectsToApply { get; }

        /// <summary>
        /// 흡혈/방어전환으로 이번 턴 발생한 회복의 유물별 기여(패배 화면 집계용).
        /// 회복량은 이미 <see cref="FinalRequest"/>의 HealAmount에 합산되어 있다.
        /// </summary>
        public IReadOnlyList<RelicContributionDelta> DerivedHealContributions { get; }

        private static int CalculateAttackPower(SlotCombatRequest request)
        {
            if (request == null || request.Damage <= 0)
            {
                return 0;
            }

            return request.Damage * Math.Max(1, request.AttackCount);
        }
    }

    public readonly struct TargetedStatusEffectSpec
    {
        public TargetedStatusEffectSpec(StatusEffectSpec spec, CombatTargetMode targetMode)
        {
            Spec = spec;
            TargetMode = targetMode;
        }

        public StatusEffectSpec Spec { get; }

        public CombatTargetMode TargetMode { get; }
    }
}
