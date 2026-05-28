using SlotRogue.Core.Combat;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Core
{
    public sealed class SlotCombatRequestConverter
    {
        public SlotCombatRequest Convert(SlotPatternResult patternResult, SlotCalculationResult calculationResult)
        {
            if (patternResult == null || calculationResult == null)
            {
                return SlotCombatRequest.Empty;
            }

            return new SlotCombatRequest(
                calculationResult.Damage,
                calculationResult.Defense,
                calculationResult.AttackCount,
                calculationResult.HealAmount,
                calculationResult.IsCritical,
                patternResult.PatternName);
        }

        public CombatSpinOutcome ToCombatSpinOutcome(SlotCombatRequest request)
        {
            if (request == null)
            {
                return new CombatSpinOutcome(0, 0);
            }

            return new CombatSpinOutcome(request.Damage, request.Defense);
        }
    }
}
