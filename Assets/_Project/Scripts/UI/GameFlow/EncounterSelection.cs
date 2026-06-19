using System;
using System.Collections.Generic;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EncounterSelection
    {
        public IReadOnlyList<SelectedEncounterMonster> Monsters { get; }

        public EncounterSelection(IReadOnlyList<SelectedEncounterMonster> monsters)
        {
            if (monsters == null)
            {
                throw new ArgumentNullException(nameof(monsters));
            }

            if (monsters.Count == 0)
            {
                throw new ArgumentException("Encounter selection must contain at least one monster.", nameof(monsters));
            }

            var copy = new SelectedEncounterMonster[monsters.Count];
            for (int index = 0; index < monsters.Count; index++)
            {
                copy[index] = monsters[index] ?? throw new ArgumentException(
                    "Encounter selection cannot contain null monsters.",
                    nameof(monsters));
            }

            Monsters = copy;
        }
    }
}
