namespace SlotRogue.UI.SlotPresentation
{
    public sealed class SlotPresentationStep
    {
        private SlotPresentationStep(
            SlotPresentationStepKind kind,
            SlotPatternPresentationResult pattern,
            SlotRelicTriggerPresentationResult relic,
            SlotFinalPresentationResult finalResult)
        {
            Kind = kind;
            Pattern = pattern;
            Relic = relic;
            FinalResult = finalResult;
        }

        public SlotPresentationStepKind Kind { get; }

        public SlotPatternPresentationResult Pattern { get; }

        public SlotRelicTriggerPresentationResult Relic { get; }

        public SlotFinalPresentationResult FinalResult { get; }

        public static SlotPresentationStep ForPattern(SlotPatternPresentationResult pattern)
        {
            return new SlotPresentationStep(SlotPresentationStepKind.Pattern, pattern, null, null);
        }

        public static SlotPresentationStep ForRelic(SlotRelicTriggerPresentationResult relic)
        {
            return new SlotPresentationStep(SlotPresentationStepKind.Relic, null, relic, null);
        }

        public static SlotPresentationStep ForFinal(SlotFinalPresentationResult finalResult)
        {
            return new SlotPresentationStep(SlotPresentationStepKind.Final, null, null, finalResult);
        }
    }
}
