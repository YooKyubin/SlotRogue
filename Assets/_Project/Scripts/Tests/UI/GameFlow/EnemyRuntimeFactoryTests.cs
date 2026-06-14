using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using SlotRogue.UI.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class EnemyRuntimeFactoryTests
    {
        [Test]
        public void EnemyActionPlannerFactory_CreatePattern_ReturnsFixedSequencePlanner()
        {
            MonsterTurnPatternDefinition pattern = Pattern(
                Turn(Step(CombatEffectKind.Damage, 3, CombatTargetMode.SelectedEnemy)),
                Turn(Step(CombatEffectKind.Shield, 5, CombatTargetMode.Self)));
            var factory = new EnemyActionPlannerFactory();

            IEnemyActionPlanner planner = factory.Create(pattern);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);
            EnemyActionContext context = Context(enemy);

            AssertPlan(planner.PlanNext(context), CombatEffectKind.Damage, 3);
            AssertPlan(planner.PlanNext(context), CombatEffectKind.Shield, 5);
            AssertPlan(planner.PlanNext(context), CombatEffectKind.Damage, 3);
        }

        [Test]
        public void EnemyActionPlannerFactory_CreatePattern_MatchesLegacyScheduleFactoryOrder()
        {
            MonsterTurnPatternDefinition pattern = Pattern(
                Turn(Step(CombatEffectKind.Damage, 4, CombatTargetMode.SelectedEnemy)),
                Turn(Step(CombatEffectKind.Heal, 2, CombatTargetMode.Self)));
            IEnemyActionPlanner planner = new EnemyActionPlannerFactory().Create(pattern);
            MonsterTurnSchedule legacySchedule = MonsterTurnScheduleFactory.FromPattern(pattern);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);
            EnemyActionContext context = Context(enemy);

            Assert.That(planner.PlanNext(context).Effects, Is.EqualTo(legacySchedule.ConsumeUpcomingTurn()));
            Assert.That(planner.PlanNext(context).Effects, Is.EqualTo(legacySchedule.ConsumeUpcomingTurn()));
        }

        [Test]
        public void EnemyRuntimeFactory_CreateDefinition_UsesMonsterHpAndEnemyIdentity()
        {
            MonsterDefinition definition = ScriptableObject.CreateInstance<MonsterDefinition>();
            definition.maxHp = 17;
            definition.turnPattern = Pattern(Turn(Step(CombatEffectKind.Damage, 6, CombatTargetMode.SelectedEnemy)));
            var factory = new EnemyRuntimeFactory();

            EnemyRuntime runtime = factory.Create(definition, rosterIndex: 2);
            runtime.PlanNextAction(Context(runtime.Participant));

            Assert.That(runtime.Participant.MaxHp, Is.EqualTo(17));
            Assert.That(runtime.Participant.CurrentHp, Is.EqualTo(17));
            Assert.That(runtime.Participant.Team, Is.EqualTo(CombatTeam.Enemy));
            Assert.That(runtime.Participant.Id.Value, Is.EqualTo(102));
            AssertPlan(runtime.UpcomingPlan, CombatEffectKind.Damage, 6);
            Assert.That(typeof(EnemyRuntime).GetProperty("Definition"), Is.Null);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void RunEncounterRosterBuilder_BuildForTier_CreatesEnemyRuntime()
        {
            RunEncounterRoster roster = RunEncounterRosterBuilder.BuildForTier(
                SlotRogue.Data.GameFlow.EncounterTier.Normal,
                level: 1);
            BattleSystem battle = new();
            CombatParticipant player = RunCombatParticipantFactory.CreatePlayer(maxHp: 30, currentHp: 30);

            battle.StartBattle(player, roster.EnemyRuntimes);

            Assert.That(roster.EnemyRuntimes.Length, Is.EqualTo(1));
            Assert.That(battle.TryGetUpcomingEnemyTurn(roster.Enemies[0].Id, out EnemyUpcomingTurn upcoming), Is.True);
            AssertPlan(upcoming.Plan, CombatEffectKind.Damage, 4);
        }

        private static MonsterTurnPatternDefinition Pattern(params MonsterTurnStepDefinition[] turns)
        {
            MonsterTurnPatternDefinition pattern = ScriptableObject.CreateInstance<MonsterTurnPatternDefinition>();
            pattern.turns = turns;
            return pattern;
        }

        private static MonsterTurnStepDefinition Turn(params CombatEffectStep[] steps)
        {
            return new MonsterTurnStepDefinition { actions = steps };
        }

        private static CombatEffectStep Step(
            CombatEffectKind kind,
            int amount,
            CombatTargetMode targetMode)
        {
            return new CombatEffectStep
            {
                kind = kind,
                amount = amount,
                targetMode = targetMode,
            };
        }

        private static EnemyActionContext Context(CombatParticipant self)
        {
            CombatParticipant player = new(30, 30, shield: 0, new CombatParticipantId(1), CombatTeam.Player);
            return new EnemyActionContext(self, player, new[] { self }, turnNumber: 0);
        }

        private static CombatParticipant Enemy(int id, int maxHp)
        {
            return new CombatParticipant(maxHp, maxHp, shield: 0, new CombatParticipantId(id), CombatTeam.Enemy);
        }

        private static void AssertPlan(
            EnemyActionPlan plan,
            CombatEffectKind expectedKind,
            int expectedAmount)
        {
            Assert.That(plan.Effects.Count, Is.EqualTo(1));
            Assert.That(plan.Effects[0].Kind, Is.EqualTo(expectedKind));
            Assert.That(plan.Effects[0].Amount, Is.EqualTo(expectedAmount));
        }
    }
}
