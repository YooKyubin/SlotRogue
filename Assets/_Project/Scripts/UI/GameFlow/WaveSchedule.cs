using System;
using System.Collections.Generic;
using SlotRogue.Data.GameFlow;

namespace SlotRogue.UI.GameFlow
{
    public sealed class WaveSchedule
    {
        private static readonly EncounterTier[] DefaultPattern =
        {
            EncounterTier.Normal,
            EncounterTier.Normal,
            EncounterTier.Normal,
            EncounterTier.Normal,
            EncounterTier.Elite,
            EncounterTier.Normal,
            EncounterTier.Normal,
            EncounterTier.Normal,
            EncounterTier.Normal,
            EncounterTier.Boss,
        };

        private readonly EncounterTier[][] _patterns;
        private readonly int _patternLength;

        public WaveSchedule(IReadOnlyList<IReadOnlyList<EncounterTier>> patterns)
        {
            _patterns = CopyAndValidate(patterns);
            _patternLength = _patterns[0].Length;
        }

        public static WaveSchedule CreateDefault()
        {
            return new WaveSchedule(new IReadOnlyList<EncounterTier>[] { DefaultPattern });
        }

        public static WaveSchedule FromDefinition(WaveScheduleDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var patterns = new IReadOnlyList<EncounterTier>[definition.Patterns.Count];
            for (int index = 0; index < definition.Patterns.Count; index++)
            {
                WaveCyclePattern pattern = definition.Patterns[index];
                patterns[index] = pattern?.Tiers;
            }

            return new WaveSchedule(patterns);
        }

        public WaveResult Evaluate(int battleNumber)
        {
            if (battleNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(battleNumber),
                    battleNumber,
                    "BattleNumber must be 1 or greater.");
            }

            int zeroBasedBattle = battleNumber - 1;
            int themeSectionIndex = zeroBasedBattle / _patternLength;
            int positionInWave = zeroBasedBattle % _patternLength;
            int patternIndex = Math.Min(themeSectionIndex, _patterns.Length - 1);
            EncounterTier encounterTier = _patterns[patternIndex][positionInWave];
            return new WaveResult(encounterTier, themeSectionIndex, positionInWave);
        }

        private static EncounterTier[][] CopyAndValidate(
            IReadOnlyList<IReadOnlyList<EncounterTier>> patterns)
        {
            if (patterns == null)
            {
                throw new ArgumentNullException(nameof(patterns));
            }

            if (patterns.Count == 0)
            {
                throw new ArgumentException("At least one wave pattern is required.", nameof(patterns));
            }

            var copy = new EncounterTier[patterns.Count][];
            int expectedLength = -1;
            for (int patternIndex = 0; patternIndex < patterns.Count; patternIndex++)
            {
                IReadOnlyList<EncounterTier> pattern = patterns[patternIndex];
                if (pattern == null)
                {
                    throw new ArgumentException($"Wave pattern at index {patternIndex} cannot be null.", nameof(patterns));
                }

                if (pattern.Count == 0)
                {
                    throw new ArgumentException($"Wave pattern at index {patternIndex} cannot be empty.", nameof(patterns));
                }

                if (expectedLength < 0)
                {
                    expectedLength = pattern.Count;
                }
                else if (pattern.Count != expectedLength)
                {
                    throw new ArgumentException(
                        $"Wave pattern at index {patternIndex} must have length {expectedLength}, but was {pattern.Count}.",
                        nameof(patterns));
                }

                copy[patternIndex] = new EncounterTier[pattern.Count];
                for (int tierIndex = 0; tierIndex < pattern.Count; tierIndex++)
                {
                    copy[patternIndex][tierIndex] = pattern[tierIndex];
                }
            }

            return copy;
        }
    }
}
