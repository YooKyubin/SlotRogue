using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.ViewModels
{
    public readonly struct SlotMachineState
    {
        public SlotMachineState(
            bool canSpin,
            SlotSpinResult spinResult,
            SlotPatternResult patternResult,
            SlotCalculationResult calculationResult,
            SlotCombatRequest combatRequest)
        {
            CanSpin = canSpin;
            SpinResult = spinResult;
            PatternResult = patternResult;
            CalculationResult = calculationResult;
            CombatRequest = combatRequest;
        }

        public bool CanSpin { get; }
        public SlotSpinResult SpinResult { get; }
        public SlotPatternResult PatternResult { get; }
        public SlotCalculationResult CalculationResult { get; }
        public SlotCombatRequest CombatRequest { get; }
    }
}
