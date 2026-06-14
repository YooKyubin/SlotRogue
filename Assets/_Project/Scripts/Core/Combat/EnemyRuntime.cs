using System;

using UnityEngine;

namespace SlotRogue.Core.Combat
{
    public sealed class EnemyRuntime
    {
        private readonly IEnemyActionPlanner _actionPlanner;
        private EnemyActionPlan _upcomingPlan;
        private bool _hasUpcomingPlan;

        public EnemyRuntime(CombatParticipant participant, IEnemyActionPlanner actionPlanner)
        {
            Participant = participant ?? throw new ArgumentNullException(nameof(participant));
            _actionPlanner = actionPlanner ?? throw new ArgumentNullException(nameof(actionPlanner));
            _upcomingPlan = new EnemyActionPlan(null);
            _hasUpcomingPlan = false;
        }

        public CombatParticipant Participant { get; }

        public EnemyActionPlan UpcomingPlan
        {
            get
            {
                if (!_hasUpcomingPlan)
                {
                    Debug.LogWarning(
                        "EnemyRuntime.UpcomingPlan was read before PlanNextAction was called.");
                }

                return _upcomingPlan;
            }
        }

        public void PlanNextAction(EnemyActionContext context)
        {
            _upcomingPlan = _actionPlanner.PlanNext(context) ?? new EnemyActionPlan(null);
            _hasUpcomingPlan = true;
        }
    }
}
