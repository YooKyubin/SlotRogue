using System;
using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class EnemyActionPlan
    {
        private readonly CombatEffect[] _effects;

        public EnemyActionPlan(IReadOnlyList<CombatEffect> effects)
        {
            _effects = Clone(effects);
        }

        public IReadOnlyList<CombatEffect> Effects => Clone(_effects);

        private static CombatEffect[] Clone(IReadOnlyList<CombatEffect> effects)
        {
            if (effects == null || effects.Count == 0)
            {
                return Array.Empty<CombatEffect>();
            }

            var copy = new CombatEffect[effects.Count];
            for (int index = 0; index < effects.Count; index++)
            {
                copy[index] = effects[index];
            }

            return copy;
        }
    }
}
