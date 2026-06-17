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
                AddPatternsAndRelics(steps, result.Patterns, result.RelicTriggers);
                AddUnassignedRelics(steps, result.RelicTriggers);

                if (result.FinalResult != null)
                {
                    steps.Add(SlotPresentationStep.ForFinal(result.FinalResult));
                }
            }

            Steps = steps;
        }

        public IReadOnlyList<SlotPresentationStep> Steps { get; }

        private static void AddPatternsAndRelics(
            List<SlotPresentationStep> steps,
            IReadOnlyList<SlotPatternPresentationResult> patterns,
            IReadOnlyList<SlotRelicTriggerPresentationResult> relics)
        {
            if (patterns == null)
            {
                return;
            }

            AddPatternsByFinaleState(steps, patterns, relics, false);
            AddPatternsByFinaleState(steps, patterns, relics, true);
        }

        private static void AddPatternsByFinaleState(
            List<SlotPresentationStep> steps,
            IReadOnlyList<SlotPatternPresentationResult> patterns,
            IReadOnlyList<SlotRelicTriggerPresentationResult> relics,
            bool isFinale)
        {
            for (int index = 0; index < patterns.Count; index++)
            {
                SlotPatternPresentationResult pattern = patterns[index];

                if (pattern != null && pattern.IsFinale == isFinale)
                {
                    steps.Add(SlotPresentationStep.ForPattern(pattern));
                    AddRelicsForPattern(steps, relics, index);
                }
            }
        }

        private static void AddRelicsForPattern(
            List<SlotPresentationStep> steps,
            IReadOnlyList<SlotRelicTriggerPresentationResult> relics,
            int patternIndex)
        {
            if (relics == null)
            {
                return;
            }

            for (int index = 0; index < relics.Count; index++)
            {
                SlotRelicTriggerPresentationResult relic = relics[index];
                if (relic != null && relic.TriggerPatternIndex == patternIndex)
                {
                    steps.Add(SlotPresentationStep.ForRelic(relic));
                }
            }
        }

        private static void AddUnassignedRelics(
            List<SlotPresentationStep> steps,
            IReadOnlyList<SlotRelicTriggerPresentationResult> relics)
        {
            if (relics == null)
            {
                return;
            }

            for (int index = 0; index < relics.Count; index++)
            {
                SlotRelicTriggerPresentationResult relic = relics[index];
                if (relic != null && relic.TriggerPatternIndex < 0)
                {
                    steps.Add(SlotPresentationStep.ForRelic(relic));
                }
            }
        }
    }
}
