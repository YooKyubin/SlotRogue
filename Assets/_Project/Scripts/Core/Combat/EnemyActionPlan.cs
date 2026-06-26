using System;
using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class EnemyActionPlan
    {
        private readonly EnemyPlannedAction[] _actions;

        public EnemyActionPlan(IReadOnlyList<CombatEffect> effects)
        {
            _actions = WrapCombatEffects(effects);
        }

        private EnemyActionPlan(IReadOnlyList<EnemyPlannedAction> actions)
        {
            _actions = CloneActions(actions);
        }

        public IReadOnlyList<EnemyPlannedAction> Actions => CloneActions(_actions);

        public IReadOnlyList<CombatEffect> Effects => FlattenCombatEffects(_actions);

        public static EnemyActionPlan FromActions(IReadOnlyList<EnemyPlannedAction> actions) =>
            new(actions);

        private static EnemyPlannedAction[] WrapCombatEffects(IReadOnlyList<CombatEffect> effects)
        {
            if (effects == null || effects.Count == 0)
            {
                return Array.Empty<EnemyPlannedAction>();
            }

            var actions = new EnemyPlannedAction[effects.Count];
            for (int index = 0; index < effects.Count; index++)
            {
                actions[index] = new EnemyPlannedAction(
                    new EnemyActionKey(index + 1),
                    string.Empty,
                    EnemyActionEffect.FromCombatEffect(effects[index]));
            }

            return actions;
        }

        private static EnemyPlannedAction[] CloneActions(IReadOnlyList<EnemyPlannedAction> actions)
        {
            if (actions == null || actions.Count == 0)
            {
                return Array.Empty<EnemyPlannedAction>();
            }

            var copy = new EnemyPlannedAction[actions.Count];
            for (int index = 0; index < actions.Count; index++)
            {
                EnemyPlannedAction action = actions[index];
                copy[index] = action == null
                    ? new EnemyPlannedAction(default, string.Empty, null)
                    : new EnemyPlannedAction(
                        action.ActionKey,
                        action.ActionName,
                        action.HasEffect ? action.Effect : null);
            }

            return copy;
        }

        private static CombatEffect[] FlattenCombatEffects(IReadOnlyList<EnemyPlannedAction> actions)
        {
            if (actions == null || actions.Count == 0)
            {
                return Array.Empty<CombatEffect>();
            }

            var effects = new List<CombatEffect>();
            for (int actionIndex = 0; actionIndex < actions.Count; actionIndex++)
            {
                EnemyPlannedAction action = actions[actionIndex];
                if (!action.HasEffect)
                {
                    continue;
                }

                EnemyActionEffect effect = action.Effect;
                if (effect.Kind == EnemyActionEffectKind.Combat)
                {
                    effects.Add(effect.CombatEffect);
                }
            }

            return effects.ToArray();
        }
    }
}
