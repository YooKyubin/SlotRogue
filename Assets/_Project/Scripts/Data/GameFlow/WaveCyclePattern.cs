using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotRogue.Data.GameFlow
{
    [Serializable]
    public sealed class WaveCyclePattern
    {
        [SerializeField] private EncounterTier[] _tiers;

        public IReadOnlyList<EncounterTier> Tiers => _tiers ?? Array.Empty<EncounterTier>();

        public bool TryValidate(out string error)
        {
            var errors = new List<string>();
            AppendValidationErrors(errors);
            error = string.Join(Environment.NewLine, errors);
            return errors.Count == 0;
        }

        internal void AppendValidationErrors(List<string> errors, string prefix = "")
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            string label = string.IsNullOrEmpty(prefix) ? "Pattern" : prefix;
            if (_tiers == null)
            {
                errors.Add($"{label}: Tier array cannot be null.");
                return;
            }

            if (_tiers.Length == 0)
            {
                errors.Add($"{label}: At least one tier is required.");
            }
        }
    }
}
