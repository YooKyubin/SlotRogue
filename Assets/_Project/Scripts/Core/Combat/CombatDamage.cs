using System;

namespace SlotRogue.Core.Combat
{
    public static class CombatDamage
    {
        public static int Apply(int rawAttack, int defense)
        {
            return Math.Max(0, rawAttack - defense);
        }
    }
}
