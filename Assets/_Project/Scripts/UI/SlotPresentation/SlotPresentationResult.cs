using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class SlotPresentationResult
    {
        public SlotPresentationResult(
            SlotSpinResult spinResult,
            IReadOnlyList<SlotPatternPresentationResult> patterns,
            IReadOnlyList<SlotRelicTriggerPresentationResult> relicTriggers,
            SlotFinalPresentationResult finalResult)
        {
            SpinResult = spinResult;
            Patterns = patterns ?? EmptyPatterns;
            RelicTriggers = relicTriggers ?? EmptyRelicTriggers;
            FinalResult = finalResult;
        }

        public SlotSpinResult SpinResult { get; }

        public IReadOnlyList<SlotPatternPresentationResult> Patterns { get; }

        public IReadOnlyList<SlotRelicTriggerPresentationResult> RelicTriggers { get; }

        public SlotFinalPresentationResult FinalResult { get; }

        private static readonly SlotPatternPresentationResult[] EmptyPatterns = new SlotPatternPresentationResult[0];
        private static readonly SlotRelicTriggerPresentationResult[] EmptyRelicTriggers = new SlotRelicTriggerPresentationResult[0];
    }
}
