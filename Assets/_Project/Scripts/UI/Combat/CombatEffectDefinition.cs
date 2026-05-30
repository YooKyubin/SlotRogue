using System;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat
{
    [Serializable]
    public struct CombatEffectDefinition
    {
        public CombatEffectKind kind;

        public int amount;

        public CombatEffectTarget target;

        public CombatEffect ToCombatEffect() => new(kind, amount, target);
    }
}
