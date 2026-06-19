using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotRogue.Data.GameFlow
{
    [CreateAssetMenu(
        fileName = "EncounterTable",
        menuName = "SlotRogue/GameFlow/Encounter Table")]
    public sealed class EncounterTable : ScriptableObject
    {
        [SerializeField] private EncounterDefinition[] _encounters;

        public IReadOnlyList<EncounterDefinition> Encounters =>
            _encounters ?? Array.Empty<EncounterDefinition>();

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
                Debug.LogError($"[EncounterTable] {name} validation failed:{Environment.NewLine}{error}", this);
            }
        }

        private void AppendValidationErrors(List<string> errors)
        {
            if (_encounters == null)
            {
                errors.Add("Encounter array cannot be null.");
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < _encounters.Length; index++)
            {
                EncounterDefinition encounter = _encounters[index];
                string prefix = $"Encounter[{index}]";
                if (encounter == null)
                {
                    errors.Add($"{prefix}: EncounterDefinition cannot be null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(encounter.Id))
                {
                    errors.Add($"{prefix}: Id cannot be empty.");
                }
                else if (!ids.Add(encounter.Id))
                {
                    errors.Add($"{prefix}: Duplicate encounter id '{encounter.Id}'.");
                }

                encounter.AppendValidationErrors(errors, prefix);
            }
        }
    }
}
