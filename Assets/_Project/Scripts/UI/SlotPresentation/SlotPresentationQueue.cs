using System.Collections.Generic;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class SlotPresentationQueue
    {
        public SlotPresentationQueue(SlotPresentationResult result)
        {
            var steps = new List<SlotPresentationStep>();

            if (result != null)
            {
                AddPatterns(steps, result.Patterns);
                AddRelics(steps, result.RelicTriggers);

                if (result.FinalResult != null)
                {
                    steps.Add(SlotPresentationStep.ForFinal(result.FinalResult));
                }
            }

            Steps = steps;
        }

        public IReadOnlyList<SlotPresentationStep> Steps { get; }

        private static void AddPatterns(
            List<SlotPresentationStep> steps,
            IReadOnlyList<SlotPatternPresentationResult> patterns)
        {
            if (patterns == null)
            {
                return;
            }

            AddPatternsByFinaleState(steps, patterns, false);
            AddPatternsByFinaleState(steps, patterns, true);
        }

        private static void AddPatternsByFinaleState(
            List<SlotPresentationStep> steps,
            IReadOnlyList<SlotPatternPresentationResult> patterns,
            bool isFinale)
        {
            for (int index = 0; index < patterns.Count; index++)
            {
                SlotPatternPresentationResult pattern = patterns[index];

                if (pattern != null && pattern.IsFinale == isFinale)
                {
                    steps.Add(SlotPresentationStep.ForPattern(pattern));
                }
            }
        }

        private static void AddRelics(
            List<SlotPresentationStep> steps,
            IReadOnlyList<SlotRelicTriggerPresentationResult> relics)
        {
            if (relics == null)
            {
                return;
            }

            for (int index = 0; index < relics.Count; index++)
            {
                if (relics[index] != null)
                {
                    steps.Add(SlotPresentationStep.ForRelic(relics[index]));
                }
            }
        }
    }
}
