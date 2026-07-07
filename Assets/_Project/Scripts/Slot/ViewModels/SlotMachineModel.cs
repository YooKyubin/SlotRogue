using System;
using System.Collections.Generic;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.ViewModels
{
    public sealed class SlotMachineModel
    {
        public SlotMachineModel()
            : this(
                new SlotMachineService(),
                new SlotPatternResolver(),
                new SlotResultCalculator(),
                new SlotCombatRequestBuilder())
        {
        }

        public SlotMachineModel(
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
            CurrentPatternMatches = Array.Empty<SlotPatternMatch>();
            CurrentPatternResult = SlotPatternResult.NoMatch;
            CurrentCalculationResult = SlotCalculationResult.Empty;
            CurrentCombatRequest = SlotCombatRequest.Empty;
        }

        public event Action<SlotMachineState> StateChanged;

        public bool CanSpin { get; private set; }

        public SlotSpinResult CurrentSpinResult { get; private set; }

        /// <summary>
        /// SO 족보 기준으로 한 번만 판정한 전체 패턴 목록. 연출/계산/전투 요청이 모두 이 목록을 공유한다.
        /// </summary>
        public IReadOnlyList<SlotPatternMatch> CurrentPatternMatches { get; private set; }

        /// <summary>
        /// 유물/텍스트 표시에 쓰이는 대표 패턴. <see cref="CurrentPatternMatches"/>에서 파생되며,
        /// 더 이상 <see cref="SlotPatternResolver.Resolve"/>를 호출하지 않는다.
        /// </summary>
        public SlotPatternResult CurrentPatternResult { get; private set; }

        public SlotCalculationResult CurrentCalculationResult { get; private set; }

        public SlotCombatRequest CurrentCombatRequest { get; private set; }

        public bool IsCurrentSpinResolved { get; private set; }

        public void Spin()
        {
            if (!CanSpin)
            {
                return;
            }

            CanSpin = false;
            SlotSpinResult spinResult = _slotMachineService.Spin();
            CanSpin = true;
            SetCurrentSpinResult(spinResult);
        }

        public bool TrySwapAdjacentSymbols(int firstIndex, int secondIndex)
        {
            if (CurrentSpinResult == null ||
                !SlotSpinResult.AreAdjacent(firstIndex, secondIndex))
            {
                return false;
            }

            SetCurrentSpinResult(CurrentSpinResult.SwapAdjacent(firstIndex, secondIndex));
            return true;
        }

        public IReadOnlyList<SlotPatternMatch> PreviewCurrentPatternMatches()
        {
            return CurrentSpinResult != null
                ? _patternResolver.ResolveAll(CurrentSpinResult)
                : Array.Empty<SlotPatternMatch>();
        }

        public SlotPatternResult PreviewCurrentPatternResult()
        {
            return BuildRepresentativePatternResult(PreviewCurrentPatternMatches());
        }

        public void ResolveCurrentSpinResult()
        {
            if (CurrentSpinResult == null || IsCurrentSpinResolved)
            {
                return;
            }

            CurrentPatternMatches = _patternResolver.ResolveAll(CurrentSpinResult);
            CurrentPatternResult = BuildRepresentativePatternResult(CurrentPatternMatches);
            CurrentCalculationResult = _resultCalculator.Calculate(CurrentPatternMatches);
            CurrentCombatRequest = _combatRequestBuilder.Build(CurrentPatternResult, CurrentCalculationResult);
            IsCurrentSpinResolved = true;
            PublishState();
        }

        private void SetCurrentSpinResult(SlotSpinResult spinResult)
        {
            CurrentSpinResult = spinResult ?? throw new ArgumentNullException(nameof(spinResult));
            ClearResolvedResult();
            PublishState();
        }

        private void ClearResolvedResult()
        {
            CurrentPatternMatches = Array.Empty<SlotPatternMatch>();
            CurrentPatternResult = SlotPatternResult.NoMatch;
            CurrentCalculationResult = SlotCalculationResult.Empty;
            CurrentCombatRequest = SlotCombatRequest.Empty;
            IsCurrentSpinResolved = false;
        }

        private void PublishState()
        {
            StateChanged?.Invoke(new SlotMachineState(
                CanSpin,
                CurrentSpinResult,
                CurrentPatternResult,
                CurrentCalculationResult,
                CurrentCombatRequest));
        }

        // 유물 적용/텍스트 표시를 위한 대표 패턴을 matches에서 파생한다(Resolve() 미사용).
        // 가장 점수가 높은 패턴을 대표로 삼고, 매치가 없으면 NoMatch를 반환한다.
        private static SlotPatternResult BuildRepresentativePatternResult(
            IReadOnlyList<SlotPatternMatch> matches)
        {
            if (matches == null || matches.Count == 0)
            {
                return SlotPatternResult.NoMatch;
            }

            SlotPatternMatch best = null;
            for (int index = 0; index < matches.Count; index++)
            {
                SlotPatternMatch candidate = matches[index];
                if (candidate == null || candidate.MatchedCells == null || candidate.MatchedCells.Count == 0)
                {
                    continue;
                }

                if (best == null || candidate.CalculatedValue > best.CalculatedValue)
                {
                    best = candidate;
                }
            }

            if (best == null)
            {
                return SlotPatternResult.NoMatch;
            }

            SlotCell firstCell = best.MatchedCells[0];
            return new SlotPatternResult(
                true,
                best.PresentationTitle,
                best.Symbol,
                firstCell.Row,
                firstCell.Col,
                best.MatchedCells.Count,
                best.CalculatedValue);
        }

        private readonly SlotMachineService _slotMachineService;
        private readonly SlotPatternResolver _patternResolver;
        private readonly SlotResultCalculator _resultCalculator;
        private readonly SlotCombatRequestBuilder _combatRequestBuilder;
    }
}
