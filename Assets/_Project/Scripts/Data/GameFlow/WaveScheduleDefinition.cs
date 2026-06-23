using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotRogue.Data.GameFlow
{
    [CreateAssetMenu(
        fileName = "WaveScheduleDefinition",
        menuName = "SlotRogue/GameFlow/Wave Schedule Definition")]
    public sealed class WaveScheduleDefinition : ScriptableObject
    {
        [SerializeField] private WaveCyclePattern[] _patterns;

        public IReadOnlyList<WaveCyclePattern> Patterns => _patterns ?? Array.Empty<WaveCyclePattern>();

        public bool TryValidate(out string error)
        {
            var errors = new List<string>();
            AppendValidationErrors(errors);
            error = string.Join(Environment.NewLine, errors);
            return errors.Count == 0;
        }

        private void OnValidate()
        {
            if (!TryValidate(out string error))
            {
                Debug.LogError($"[WaveScheduleDefinition] {name} validation failed:{Environment.NewLine}{error}", this);
            }
        }

        private void AppendValidationErrors(List<string> errors)
        {
            if (_patterns == null)
            {
                errors.Add("Pattern array cannot be null.");
                return;
            }

            if (_patterns.Length == 0)
            {
                errors.Add("At least one pattern is required.");
                return;
            }

            int expectedLength = -1;
            for (int index = 0; index < _patterns.Length; index++)
            {
                WaveCyclePattern pattern = _patterns[index];
                string prefix = $"Pattern[{index}]";
                if (pattern == null)
                {
                    errors.Add($"{prefix}: Pattern cannot be null.");
                    continue;
                }

                pattern.AppendValidationErrors(errors, prefix);
                int length = pattern.Tiers.Count;
                if (length == 0)
                {
                    continue;
                }

                if (expectedLength < 0)
                {
                    expectedLength = length;
                }
                else if (length != expectedLength)
                {
                    errors.Add($"{prefix}: Pattern length must be {expectedLength}, but was {length}.");
                }
            }
        }
    }
}
