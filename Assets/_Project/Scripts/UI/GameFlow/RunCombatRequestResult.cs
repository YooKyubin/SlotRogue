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
            IReadOnlyList<StatusEffectSpec> statusEffectsToApply = null)
        {
            BaseRequest = baseRequest;
            FinalRequest = finalRequest;
            AttackPower = CalculateAttackPower(finalRequest);
            RelicActivationSummary = relicActivationSummary ?? string.Empty;
            RunBonusSummary = runBonusSummary ?? string.Empty;
            StatusEffectsToApply = statusEffectsToApply ?? Array.Empty<StatusEffectSpec>();
        }

        public SlotCombatRequest BaseRequest { get; }

        public SlotCombatRequest FinalRequest { get; }

        public int AttackPower { get; }

        public string RelicActivationSummary { get; }

        public string RunBonusSummary { get; }

        public IReadOnlyList<StatusEffectSpec> StatusEffectsToApply { get; }

        private static int CalculateAttackPower(SlotCombatRequest request)
        {
            if (request == null || request.Damage <= 0)
            {
                return 0;
            }

            return request.Damage * Math.Max(1, request.AttackCount);
        }
    }
}
