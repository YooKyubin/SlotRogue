using System;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyEncounterUnit
    {
        // MonsterDefinition is intentionally omitted for now: the current infinite-mode
        // builder creates enemies from EncounterTier + level, so no definition object exists yet.
        public EnemyEncounterUnit(EnemyCombatant combatant, int formationSlot)
            : this(combatant, formationSlot, EnemyActionPresentationMap.Empty)
        {
        }

        public EnemyEncounterUnit(
            EnemyCombatant combatant,
            int formationSlot,
            EnemyActionPresentationMap presentationMap)
        {
            Combatant = combatant ?? throw new ArgumentNullException(nameof(combatant));
            FormationSlot = formationSlot;
            PresentationMap = presentationMap ?? EnemyActionPresentationMap.Empty;
        }

        public EnemyCombatant Combatant { get; }

        public int FormationSlot { get; }

        public EnemyActionPresentationMap PresentationMap { get; }
    }
}
