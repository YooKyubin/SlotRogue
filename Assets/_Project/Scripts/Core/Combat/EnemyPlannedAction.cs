using System;
using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class EnemyPlannedAction
    {
        private readonly EnemyActionEffect[] _effects;

        public EnemyPlannedAction(EnemyActionKey actionKey, IReadOnlyList<EnemyActionEffect> effects)
        {
            ActionKey = actionKey;
            _effects = Clone(effects);
        }

        public EnemyActionKey ActionKey { get; }

        public IReadOnlyList<EnemyActionEffect> Effects => Clone(_effects);

        private static EnemyActionEffect[] Clone(IReadOnlyList<EnemyActionEffect> effects)
        {
            if (effects == null || effects.Count == 0)
            {
                return Array.Empty<EnemyActionEffect>();
            }

            var copy = new EnemyActionEffect[effects.Count];
            for (int index = 0; index < effects.Count; index++)
            {
                copy[index] = effects[index];
            }

            return copy;
        }
    }
}
