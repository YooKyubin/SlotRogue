using System;

using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.ViewModels
{
    public sealed class SlotMachineViewModel
    {
        public SlotMachineViewModel()
            : this(
                new SlotMachineService(),
                new SlotPatternResolver(),
                new SlotResultCalculator(),
                new SlotCombatRequestBuilder())
        {
        }

        public SlotMachineViewModel(
            SlotMachineService slotMachineService,
            SlotPatternResolver patternResolver,
            SlotResultCalculator resultCalculator,
            SlotCombatRequestBuilder combatRequestBuilder)
        {
            _slotMachineService = slotMachineService ?? new SlotMachineService();
            _patternResolver = patternResolver ?? new SlotPatternResolver();
            _resultCalculator = resultCalculator ?? new SlotResultCalculator();
            _combatRequestBuilder = combatRequestBuilder ?? new SlotCombatRequestBuilder();
            CanSpin = true;
            CurrentPatternResult = SlotPatternResult.NoMatch;
            CurrentCalculationResult = SlotCalculationResult.Empty;
            CurrentCombatRequest = SlotCombatRequest.Empty;
        }

        public event Action<SlotMachineState> StateChanged;

        public bool CanSpin { get; private set; }

        public SlotSpinResult CurrentSpinResult { get; private set; }

        public SlotPatternResult CurrentPatternResult { get; private set; }

        public SlotCalculationResult CurrentCalculationResult { get; private set; }

        public SlotCombatRequest CurrentCombatRequest { get; private set; }

        public void Spin()
        {
            if (!CanSpin)
            {
                return;
            }

            CanSpin = false;

            CurrentSpinResult = _slotMachineService.Spin();
            CurrentPatternResult = _patternResolver.Resolve(CurrentSpinResult);
            CurrentCalculationResult = _resultCalculator.Calculate(CurrentPatternResult);
            CurrentCombatRequest = _combatRequestBuilder.Build(CurrentPatternResult, CurrentCalculationResult);

            CanSpin = true;
            StateChanged?.Invoke(new SlotMachineState(CanSpin, CurrentSpinResult, CurrentPatternResult, CurrentCalculationResult, CurrentCombatRequest));
        }

        private readonly SlotMachineService _slotMachineService;
        private readonly SlotPatternResolver _patternResolver;
        private readonly SlotResultCalculator _resultCalculator;
        private readonly SlotCombatRequestBuilder _combatRequestBuilder;
    }
}
