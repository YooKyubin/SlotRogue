using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Core
{
    public sealed class SlotCombatRequestBuilder
    {
        public SlotCombatRequest Build(SlotPatternResult patternResult, SlotCalculationResult calculationResult)
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
    }
}
