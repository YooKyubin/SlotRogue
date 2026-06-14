using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class EnemyCombatantTests
    {
        [Test]
        public void Constructor_StoresParticipant()
        {
            CombatParticipant enemy = Enemy(100);
            var combatant = new EnemyCombatant(enemy, new SequencePlanner(Plan(CombatEffectKind.Damage, 1)));

            Assert.That(combatant.Participant, Is.SameAs(enemy));
        }

        [Test]
        public void UpcomingPlan_BeforePlanning_ReturnsEmptyPlan()
        {
            var combatant = new EnemyCombatant(
                Enemy(100),
                new SequencePlanner(Plan(CombatEffectKind.Damage, 1)));

            Assert.That(combatant.UpcomingPlan.Effects, Is.Empty);
        }

        [Test]
        public void PlanNextAction_StoresPlannerResultAsUpcomingPlan()
        {
            var combatant = new EnemyCombatant(
                Enemy(100),
                new SequencePlanner(Plan(CombatEffectKind.Damage, 3)));

            combatant.PlanNextAction(CreateContext(combatant.Participant));

            AssertPlan(combatant.UpcomingPlan, CombatEffectKind.Damage, 3);
        }

        [Test]
        public void PlanNextAction_MultipleCalls_UpdatesUpcomingPlanInPlannerOrder()
        {
            var combatant = new EnemyCombatant(
                Enemy(100),
                new SequencePlanner(
                    Plan(CombatEffectKind.Damage, 3),
                    Plan(CombatEffectKind.Shield, 5)));

            combatant.PlanNextAction(CreateContext(combatant.Participant));
            AssertPlan(combatant.UpcomingPlan, CombatEffectKind.Damage, 3);

            combatant.PlanNextAction(CreateContext(combatant.Participant));
            AssertPlan(combatant.UpcomingPlan, CombatEffectKind.Shield, 5);
        }

        [Test]
        public void PublicApi_DoesNotExposePlanner()
        {
            PropertyInfo property = typeof(EnemyCombatant).GetProperty("ActionPlanner");
            FieldInfo field = typeof(EnemyCombatant).GetField(
                "ActionPlanner",
                BindingFlags.Instance | BindingFlags.Public);

            Assert.That(property, Is.Null);
            Assert.That(field, Is.Null);
        }

        private static EnemyActionContext CreateContext(CombatParticipant enemy)
        {
            CombatParticipant player = Player();

            return new EnemyActionContext(enemy, player, new[] { enemy }, turnNumber: 0);
        }

        private static EnemyActionPlan Plan(CombatEffectKind kind, int amount) =>
            new(new[] { Effect(kind, amount) });

        private static CombatEffect Effect(CombatEffectKind kind, int amount) =>
            new(kind, amount, kind == CombatEffectKind.Shield
                ? CombatEffectTarget.Self
                : CombatEffectTarget.Enemy);

        private static void AssertPlan(
            EnemyActionPlan plan,
            CombatEffectKind expectedKind,
            int expectedAmount)
        {
            IReadOnlyList<CombatEffect> effects = plan.Effects;
            Assert.That(effects.Count, Is.EqualTo(1));
            Assert.That(effects[0].Kind, Is.EqualTo(expectedKind));
            Assert.That(effects[0].Amount, Is.EqualTo(expectedAmount));
        }

        private static CombatParticipant Player() =>
            new(maxHp: 50, currentHp: 50, shield: 0, new CombatParticipantId(1), CombatTeam.Player);

        private static CombatParticipant Enemy(int id) =>
            new(maxHp: 20, currentHp: 20, shield: 0, new CombatParticipantId(id), CombatTeam.Enemy);

        private sealed class SequencePlanner : IEnemyActionPlanner
        {
            private readonly EnemyActionPlan[] _plans;
            private int _index;

            public SequencePlanner(params EnemyActionPlan[] plans)
            {
                _plans = plans;
            }

            public EnemyActionPlan PlanNext(EnemyActionContext context)
            {
                EnemyActionPlan plan = _plans[_index % _plans.Length];
                _index++;
                return plan;
            }
        }
    }
}
