using System;
using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class FixedSequenceEnemyActionPlanner : IEnemyActionPlanner
    {
        private readonly EnemyActionPlan[] _plans;
        private int _upcomingPlanIndex;

        public FixedSequenceEnemyActionPlanner(IReadOnlyList<EnemyActionPlan> plans)
        {
            _plans = ClonePlans(plans);
            _upcomingPlanIndex = 0;
        }

        public EnemyActionPlan PlanNext(EnemyActionContext context)
        {
            EnemyActionPlan plan = _plans[_upcomingPlanIndex % _plans.Length];
            _upcomingPlanIndex = (_upcomingPlanIndex + 1) % _plans.Length;
            return plan;
        }

        private static EnemyActionPlan[] ClonePlans(IReadOnlyList<EnemyActionPlan> plans)
        {
            if (plans == null || plans.Count == 0)
            {
                return new[] { new EnemyActionPlan(null) };
            }

            var copy = new EnemyActionPlan[plans.Count];
            for (int index = 0; index < plans.Count; index++)
            {
                EnemyActionPlan plan = plans[index];
                copy[index] = plan == null
                    ? new EnemyActionPlan(null)
                    : new EnemyActionPlan(plan.Effects);
            }

            return copy;
        }
    }
}
