using System;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyEncounterUnit
    {
        // MonsterDefinition is intentionally omitted for now: the current infinite-mode
        // builder creates enemies from EncounterTier + level, so no definition object exists yet.
        public EnemyEncounterUnit(EnemyCombatant combatant, int formationSlot)
        {
            Combatant = combatant ?? throw new ArgumentNullException(nameof(combatant));
            FormationSlot = formationSlot;
        }

        public EnemyCombatant Combatant { get; }

        public int FormationSlot { get; }
    }
}
