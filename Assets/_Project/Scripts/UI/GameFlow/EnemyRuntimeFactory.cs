using System;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyRuntimeFactory
    {
        private readonly EnemyActionPlannerFactory _plannerFactory;

        public EnemyRuntimeFactory()
            : this(new EnemyActionPlannerFactory())
        {
        }

        public EnemyRuntimeFactory(EnemyActionPlannerFactory plannerFactory)
        {
            _plannerFactory = plannerFactory ?? throw new ArgumentNullException(nameof(plannerFactory));
        }

        public EnemyRuntime Create(MonsterDefinition definition, int rosterIndex)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            IEnemyActionPlanner planner = _plannerFactory.Create(definition.turnPattern);
            return Create(rosterIndex, definition.maxHp, planner);
        }

        public EnemyRuntime Create(
            int rosterIndex,
            int maxHp,
            IEnemyActionPlanner planner)
        {
            CombatParticipant participant = RunCombatParticipantFactory.CreateEnemy(rosterIndex, maxHp);
            return new EnemyRuntime(
                participant,
                planner ?? throw new ArgumentNullException(nameof(planner)));
        }
    }
}
