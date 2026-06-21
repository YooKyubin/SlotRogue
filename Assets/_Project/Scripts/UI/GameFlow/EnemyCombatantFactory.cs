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
            return CreateWithPresentation(definition, rosterIndex).Combatant;
        }

        public EnemyCombatantBuildResult CreateWithPresentation(MonsterDefinition definition, int rosterIndex)
        {
            return CreateWithPresentation(definition, rosterIndex, maxHpOverride: null);
        }

        public EnemyCombatantBuildResult CreateWithPresentation(
            MonsterDefinition definition,
            int rosterIndex,
            int? maxHpOverride)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            EnemyActionPlannerBuildResult plannerResult = _plannerFactory.Build(definition.turnPattern);
            EnemyCombatant combatant = Create(
                rosterIndex,
                maxHpOverride ?? definition.maxHp,
                plannerResult.Planner);
            return new EnemyCombatantBuildResult(combatant, plannerResult.PresentationMap);
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
