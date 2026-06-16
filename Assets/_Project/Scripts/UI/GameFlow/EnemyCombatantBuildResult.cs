using System;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyCombatantBuildResult
    {
        public EnemyCombatantBuildResult(
            EnemyCombatant combatant,
            EnemyActionPresentationMap presentationMap)
        {
            Combatant = combatant ?? throw new ArgumentNullException(nameof(combatant));
            PresentationMap = presentationMap ?? EnemyActionPresentationMap.Empty;
        }

        public EnemyCombatant Combatant { get; }

        public EnemyActionPresentationMap PresentationMap { get; }
    }
}
