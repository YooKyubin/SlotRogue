using System;
using SlotRogue.Data.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class SelectedEncounterMonster
    {
        public MonsterDefinition Definition { get; }

        public int FormationSlot { get; }

        public SelectedEncounterMonster(MonsterDefinition definition, int formationSlot)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            FormationSlot = formationSlot;
        }
    }
}
