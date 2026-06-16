using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class FixedSequenceEnemyActionPlannerTests
    {
        [Test]
        public void PlanNext_FirstCall_ReturnsFirstPlan()
        {
            var planner = new FixedSequenceEnemyActionPlanner(new[]
            {
                Plan(CombatEffectKind.Damage, 1),
                Plan(CombatEffectKind.Shield, 2),
            });

            EnemyActionPlan plan = planner.PlanNext(CreateContext());

            AssertPlan(plan, CombatEffectKind.Damage, 1);
        }

        [Test]
        public void PlanNext_EachCall_ReturnsNextPlan()
        {
            var planner = new FixedSequenceEnemyActionPlanner(new[]
            {
                Plan(CombatEffectKind.Damage, 1),
                Plan(CombatEffectKind.Shield, 2),
                Plan(CombatEffectKind.Damage, 3),
            });

            AssertPlan(planner.PlanNext(CreateContext()), CombatEffectKind.Damage, 1);
            AssertPlan(planner.PlanNext(CreateContext()), CombatEffectKind.Shield, 2);
            AssertPlan(planner.PlanNext(CreateContext()), CombatEffectKind.Damage, 3);
        }

        [Test]
        public void PlanNext_AfterLastPlan_CyclesToFirstPlan()
        {
            var planner = new FixedSequenceEnemyActionPlanner(new[]
            {
                Plan(CombatEffectKind.Damage, 1),
                Plan(CombatEffectKind.Shield, 2),
            });

            planner.PlanNext(CreateContext());
            planner.PlanNext(CreateContext());

            AssertPlan(planner.PlanNext(CreateContext()), CombatEffectKind.Damage, 1);
        }

        [Test]
        public void PlanNext_PreservesEffectOrderWithinPlan()
        {
            var planner = new FixedSequenceEnemyActionPlanner(new[]
            {
                new EnemyActionPlan(new[]
                {
                    Effect(CombatEffectKind.Damage, 1),
                    Effect(CombatEffectKind.Shield, 2),
                    Effect(CombatEffectKind.Damage, 3),
                }),
            });

            IReadOnlyList<CombatEffect> effects = planner.PlanNext(CreateContext()).Effects;

            Assert.That(effects.Count, Is.EqualTo(3));
            Assert.That(effects[0].Kind, Is.EqualTo(CombatEffectKind.Damage));
            Assert.That(effects[0].Amount, Is.EqualTo(1));
            Assert.That(effects[1].Kind, Is.EqualTo(CombatEffectKind.Shield));
            Assert.That(effects[1].Amount, Is.EqualTo(2));
            Assert.That(effects[2].Kind, Is.EqualTo(CombatEffectKind.Damage));
            Assert.That(effects[2].Amount, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_CopiesSourcePlanList()
        {
            var source = new[]
            {
                Plan(CombatEffectKind.Damage, 1),
                Plan(CombatEffectKind.Shield, 2),
            };
            var planner = new FixedSequenceEnemyActionPlanner(source);

            source[0] = Plan(CombatEffectKind.Damage, 9);

            AssertPlan(planner.PlanNext(CreateContext()), CombatEffectKind.Damage, 1);
        }

        [Test]
        public void EmptyPlanList_FallsBackToSingleEmptyPlan()
        {
            var planner = new FixedSequenceEnemyActionPlanner(System.Array.Empty<EnemyActionPlan>());

            EnemyActionPlan plan = planner.PlanNext(CreateContext());

            Assert.That(plan.Effects, Is.Empty);
        }

        private static EnemyActionContext CreateContext()
        {
            CombatParticipant player = Player();
            CombatParticipant enemy = Enemy(100);

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
    }
}
