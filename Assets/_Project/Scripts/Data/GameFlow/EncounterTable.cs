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
        [Serializable]
        private sealed class ThemeGroup
        {
            [SerializeField] private EncounterDefinition[] _encounters;

            public ThemeGroup()
            {
            }

            public ThemeGroup(EncounterDefinition[] encounters)
            {
                _encounters = encounters;
            }

            public IReadOnlyList<EncounterDefinition> Encounters =>
                _encounters ?? Array.Empty<EncounterDefinition>();

            public bool HasSerializedEncounters => _encounters != null;
        }

        // ThemeGroup array index is the theme identity. Reordering groups changes
        // deterministic selection results for the same run seed.
        [SerializeField] private ThemeGroup[] _themeGroups;

        public int ThemeGroupCount => _themeGroups?.Length ?? 0;

        public IReadOnlyList<EncounterDefinition> GetEncounters(int themeGroupIndex)
        {
            if (_themeGroups == null)
            {
                throw new InvalidOperationException("EncounterTable theme group array cannot be null.");
            }

            if (themeGroupIndex < 0 || themeGroupIndex >= _themeGroups.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(themeGroupIndex),
                    themeGroupIndex,
                    $"ThemeGroupIndex must be between 0 and {_themeGroups.Length - 1}.");
            }

            ThemeGroup group = _themeGroups[themeGroupIndex];
            if (group == null)
            {
                throw new InvalidOperationException($"ThemeGroup[{themeGroupIndex}] cannot be null.");
            }

            return group.Encounters;
        }

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
            if (_themeGroups == null)
            {
                errors.Add("ThemeGroup array cannot be null.");
                return;
            }

            if (_themeGroups.Length == 0)
            {
                errors.Add("At least one ThemeGroup is required.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int groupIndex = 0; groupIndex < _themeGroups.Length; groupIndex++)
            {
                ThemeGroup group = _themeGroups[groupIndex];
                string groupPrefix = $"ThemeGroup[{groupIndex}]";
                if (group == null)
                {
                    errors.Add($"{groupPrefix}: ThemeGroup cannot be null.");
                    continue;
                }

                if (!group.HasSerializedEncounters)
                {
                    errors.Add($"{groupPrefix}: Encounter array cannot be null.");
                    continue;
                }

                IReadOnlyList<EncounterDefinition> encounters = group.Encounters;
                if (encounters.Count == 0)
                {
                    errors.Add($"{groupPrefix}: At least one EncounterDefinition is required.");
                }

                for (int encounterIndex = 0; encounterIndex < encounters.Count; encounterIndex++)
                {
                    EncounterDefinition encounter = encounters[encounterIndex];
                    string prefix = $"{groupPrefix}.Encounter[{encounterIndex}]";
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
}
