using System;
using System.Collections.Generic;
using SlotRogue.Data.Combat;
using UnityEngine;

namespace SlotRogue.Data.GameFlow
{
    [Serializable]
    public sealed class EncounterDefinition
    {
        private const int MaxMonsterCount = 3;
        private const int UnlimitedMaxCycle = -1;

        [SerializeField] private string _id = string.Empty;
        [SerializeField] private EncounterTier _tier;
        [SerializeField] private MonsterDefinition[] _monsters;
        [SerializeField] private int _weight = 1;
        [SerializeField] private int _minCycle;
        [SerializeField] private int _maxCycle = UnlimitedMaxCycle;

        public string Id => _id ?? string.Empty;

        public EncounterTier Tier => _tier;

        public IReadOnlyList<MonsterDefinition> Monsters => _monsters ?? Array.Empty<MonsterDefinition>();

        public int Weight => _weight;

        public int MinCycle => _minCycle;

        public int MaxCycle => _maxCycle;

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

            string label = string.IsNullOrEmpty(prefix) ? "Encounter" : prefix;

            if (_weight < 1)
            {
                errors.Add($"{label}: Weight must be 1 or greater.");
            }

            if (_minCycle < 0)
            {
                errors.Add($"{label}: MinCycle cannot be negative.");
            }

            if (_maxCycle < UnlimitedMaxCycle)
            {
                errors.Add($"{label}: MaxCycle must be -1 or greater.");
            }

            if (_maxCycle != UnlimitedMaxCycle && _maxCycle < _minCycle)
            {
                errors.Add($"{label}: MaxCycle cannot be smaller than MinCycle unless it is -1.");
            }

            if (_monsters == null)
            {
                errors.Add($"{label}: Monsters array cannot be null.");
                return;
            }

            if (_monsters.Length == 0)
            {
                errors.Add($"{label}: At least one MonsterDefinition is required.");
            }

            if (_monsters.Length > MaxMonsterCount)
            {
                errors.Add($"{label}: Monster count must be between 1 and {MaxMonsterCount}.");
            }

            for (int index = 0; index < _monsters.Length; index++)
            {
                if (_monsters[index] == null)
                {
                    errors.Add($"{label}: Monster at index {index} cannot be null.");
                }
            }
        }

    }
}
