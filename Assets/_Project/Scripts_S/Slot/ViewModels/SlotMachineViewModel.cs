using System;

using SlotRogue.Core.Combat;
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
                new SlotCombatRequestConverter())
        {
        }

        public SlotMachineViewModel(
            SlotMachineService slotMachineService,
            SlotPatternResolver patternResolver,
            SlotResultCalculator resultCalculator,
            SlotCombatRequestConverter combatRequestConverter)
        {
            _slotMachineService = slotMachineService ?? new SlotMachineService();
            _patternResolver = patternResolver ?? new SlotPatternResolver();
            _resultCalculator = resultCalculator ?? new SlotResultCalculator();
            _combatRequestConverter = combatRequestConverter ?? new SlotCombatRequestConverter();
            CanSpin = true;
            CurrentPatternResult = SlotPatternResult.NoMatch;
            CurrentCalculationResult = SlotCalculationResult.Empty;
            CurrentCombatRequest = SlotCombatRequest.Empty;
            CurrentCombatOutcome = new CombatSpinOutcome(0, 0);
        }

        public event Action StateChanged;

        public bool CanSpin { get; private set; }

        public SlotSpinResult CurrentSpinResult { get; private set; }

        public SlotPatternResult CurrentPatternResult { get; private set; }

        public SlotCalculationResult CurrentCalculationResult { get; private set; }

        public SlotCombatRequest CurrentCombatRequest { get; private set; }

        public CombatSpinOutcome CurrentCombatOutcome { get; private set; }

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
            CurrentCombatRequest = _combatRequestConverter.Convert(CurrentPatternResult, CurrentCalculationResult);
            CurrentCombatOutcome = _combatRequestConverter.ToCombatSpinOutcome(CurrentCombatRequest);

            CanSpin = true;
            StateChanged?.Invoke();
        }

        private readonly SlotMachineService _slotMachineService;
        private readonly SlotPatternResolver _patternResolver;
        private readonly SlotResultCalculator _resultCalculator;
        private readonly SlotCombatRequestConverter _combatRequestConverter;
    }
}
