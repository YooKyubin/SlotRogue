using System;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyActionPlannerBuildResult
    {
        public EnemyActionPlannerBuildResult(
            IEnemyActionPlanner planner,
            EnemyActionPresentationMap presentationMap)
        {
            Planner = planner ?? throw new ArgumentNullException(nameof(planner));
            PresentationMap = presentationMap ?? EnemyActionPresentationMap.Empty;
        }

        public IEnemyActionPlanner Planner { get; }

        public EnemyActionPresentationMap PresentationMap { get; }
    }
}
