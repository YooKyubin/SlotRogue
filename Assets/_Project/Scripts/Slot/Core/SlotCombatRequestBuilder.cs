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

            string patternName = patternResult.HasMatch
                ? patternResult.PatternName
                : SlotCombatRequest.Empty.PatternName;

            return new SlotCombatRequest(
                calculationResult.Damage,
                calculationResult.Defense,
                calculationResult.AttackCount,
                calculationResult.HealAmount,
                calculationResult.IsCritical,
                patternName);
        }
    }
}
