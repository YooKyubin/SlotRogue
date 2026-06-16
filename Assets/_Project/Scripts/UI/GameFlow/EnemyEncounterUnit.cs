using System;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyEncounterUnit
    {
        public EnemyCombatant Combatant { get; }
        public int FormationSlot { get; }
        public EnemyActionPresentationMap PresentationMap { get; }
        public MonsterDefinition Definition { get; }

        public EnemyEncounterUnit(
            EnemyCombatant combatant,
            MonsterDefinition definition,
            int formationSlot)
            : this(combatant, definition, formationSlot, EnemyActionPresentationMap.Empty)
        {
        }

        public EnemyEncounterUnit(
            EnemyCombatant combatant,
            MonsterDefinition definition,
            int formationSlot,
            EnemyActionPresentationMap presentationMap)
        {
            Combatant = combatant ?? throw new ArgumentNullException(nameof(combatant));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            FormationSlot = formationSlot;
            PresentationMap = presentationMap ?? EnemyActionPresentationMap.Empty;
        }
    }
}
