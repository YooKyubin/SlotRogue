using System;
using SlotRogue.Core.Combat;

namespace SlotRogue.Data.Combat
{
    [Serializable]
    public struct CombatEffectStep
    {
        public CombatEffectKind kind;

        public int amount;

        public CombatEffectTarget target;

        public CombatEffect ToCombatEffect() => new(kind, amount, target);
    }
}
