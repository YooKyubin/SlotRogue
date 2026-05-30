using System;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat
{
    [Serializable]
    public struct MonsterTurnDefinition
    {
        public CombatEffectDefinition[] actions;
    }
}
