using System;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyCombatantFactory
    {
        private readonly EnemyActionPlannerFactory _plannerFactory;

        public EnemyCombatantFactory()
            : this(new EnemyActionPlannerFactory())
        {
        }

        public EnemyCombatantFactory(EnemyActionPlannerFactory plannerFactory)
        {
            _plannerFactory = plannerFactory ?? throw new ArgumentNullException(nameof(plannerFactory));
        }

        public EnemyCombatant Create(MonsterDefinition definition, int rosterIndex)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            IEnemyActionPlanner planner = _plannerFactory.Create(definition.turnPattern);
            return Create(rosterIndex, definition.maxHp, planner);
        }

        public EnemyCombatant Create(
            int rosterIndex,
            int maxHp,
            IEnemyActionPlanner planner)
        {
            CombatParticipant participant = RunCombatParticipantFactory.CreateEnemy(rosterIndex, maxHp);
            return new EnemyCombatant(
                participant,
                planner ?? throw new ArgumentNullException(nameof(planner)));
        }
    }
}
