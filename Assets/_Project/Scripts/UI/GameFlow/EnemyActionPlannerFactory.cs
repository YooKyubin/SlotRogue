using System;
using System.Collections.Generic;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyActionPlannerFactory
    {
        public IEnemyActionPlanner Create(MonsterTurnPatternDefinition pattern)
        {
            if (pattern == null)
            {
                return CreateDefaultFallback();
            }

            if (pattern.turns == null || pattern.turns.Length == 0)
            {
                return CreateEmptyPlanner();
            }

            var turnEffects = new CombatEffect[pattern.turns.Length][];
            for (int turnIndex = 0; turnIndex < pattern.turns.Length; turnIndex++)
            {
                turnEffects[turnIndex] = ToCombatEffects(pattern.turns[turnIndex].actions);
            }

            return Create(turnEffects);
        }

        public IEnemyActionPlanner Create(IReadOnlyList<IReadOnlyList<CombatEffect>> turnEffects)
        {
            if (turnEffects == null || turnEffects.Count == 0)
            {
                return CreateDefaultFallback();
            }

            var plans = new EnemyActionPlan[turnEffects.Count];
            for (int turnIndex = 0; turnIndex < turnEffects.Count; turnIndex++)
            {
                plans[turnIndex] = new EnemyActionPlan(turnEffects[turnIndex]);
            }

            return new FixedSequenceEnemyActionPlanner(plans);
        }

        private static IEnemyActionPlanner CreateDefaultFallback()
        {
            return new FixedSequenceEnemyActionPlanner(new[]
            {
                new EnemyActionPlan(new[]
                {
                    new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy),
                }),
            });
        }

        private static IEnemyActionPlanner CreateEmptyPlanner()
        {
            return new FixedSequenceEnemyActionPlanner(new[]
            {
                new EnemyActionPlan(null),
            });
        }

        private static CombatEffect[] ToCombatEffects(IReadOnlyList<CombatEffectStep> steps)
        {
            if (steps == null || steps.Count == 0)
            {
                return Array.Empty<CombatEffect>();
            }

            var effects = new CombatEffect[steps.Count];
            for (int index = 0; index < steps.Count; index++)
            {
                effects[index] = steps[index].ToCombatEffect();
            }

            return effects;
        }
    }
}
