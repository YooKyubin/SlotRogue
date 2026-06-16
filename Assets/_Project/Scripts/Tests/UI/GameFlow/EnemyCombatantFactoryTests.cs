using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using SlotRogue.UI.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class EnemyCombatantFactoryTests
    {
        [Test]
        public void EnemyActionPlannerFactory_CreatePattern_ReturnsFixedSequencePlanner()
        {
            MonsterTurnPatternDefinition pattern = Pattern(
                Turn(Action("Attack", Damage(3, CombatTargetMode.SelectedEnemy))),
                Turn(Action("Guard", Shield(5, CombatTargetMode.Self))));
            var factory = new EnemyActionPlannerFactory();

            IEnemyActionPlanner planner = factory.Create(pattern);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);
            EnemyActionContext context = Context(enemy);

            AssertPlan(planner.PlanNext(context), CombatEffectKind.Damage, 3);
            AssertPlan(planner.PlanNext(context), CombatEffectKind.Shield, 5);
            AssertPlan(planner.PlanNext(context), CombatEffectKind.Damage, 3);
        }

        [Test]
        public void EnemyActionPlannerFactory_CreatePattern_PreservesActionOrder()
        {
            MonsterTurnPatternDefinition pattern = Pattern(
                Turn(
                    Action("Attack", Damage(4, CombatTargetMode.SelectedEnemy)),
                    Action("Guard", Shield(3, CombatTargetMode.Self)),
                    Action("Recover", Heal(2, CombatTargetMode.Self))));
            IEnemyActionPlanner planner = new EnemyActionPlannerFactory().Create(pattern);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);
            EnemyActionContext context = Context(enemy);

            var effects = planner.PlanNext(context).Effects;

            Assert.That(effects.Count, Is.EqualTo(3));
            Assert.That(effects[0].Kind, Is.EqualTo(CombatEffectKind.Damage));
            Assert.That(effects[0].Amount, Is.EqualTo(4));
            Assert.That(effects[1].Kind, Is.EqualTo(CombatEffectKind.Shield));
            Assert.That(effects[1].Amount, Is.EqualTo(3));
            Assert.That(effects[2].Kind, Is.EqualTo(CombatEffectKind.Heal));
            Assert.That(effects[2].Amount, Is.EqualTo(2));
        }

        [Test]
        public void EnemyActionPlannerFactory_CreatePattern_PreservesActionBoundaries()
        {
            MonsterTurnPatternDefinition pattern = Pattern(
                Turn(
                    Action("Attack", Damage(4, CombatTargetMode.SelectedEnemy)),
                    Action("Lock", new LockSlotEffectDefinition(lockCount: 1, durationTurns: 2))));
            IEnemyActionPlanner planner = new EnemyActionPlannerFactory().Create(pattern);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);

            EnemyActionPlan plan = planner.PlanNext(Context(enemy));

            Assert.That(plan.Actions.Count, Is.EqualTo(2));
            Assert.That(plan.Actions[0].Effects.Count, Is.EqualTo(1));
            Assert.That(plan.Actions[1].Effects.Count, Is.EqualTo(1));
            Assert.That(plan.Actions[1].Effects[0].Kind, Is.EqualTo(EnemyActionEffectKind.LockSlot));
            Assert.That(plan.Actions[1].Effects[0].LockCount, Is.EqualTo(1));
            Assert.That(plan.Actions[1].Effects[0].DurationTurns, Is.EqualTo(2));
        }

        [Test]
        public void EnemyActionPlannerFactory_CreatePattern_PreservesTargetParticipantId()
        {
            MonsterTurnPatternDefinition pattern = Pattern(
                Turn(Action("Focus", Damage(4, CombatTargetMode.SelectedEnemy, targetParticipantId: 101))));
            IEnemyActionPlanner planner = new EnemyActionPlannerFactory().Create(pattern);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);

            CombatEffect effect = planner.PlanNext(Context(enemy)).Effects[0];

            Assert.That(effect.Target.ParticipantId.Value, Is.EqualTo(101));
        }

        [Test]
        public void EnemyActionDefinition_StoresDisplayDataAndPolymorphicEffect()
        {
            var action = new EnemyActionDefinition(
                "Lock Strike",
                intentIcon: null,
                new LockSlotEffectDefinition(lockCount: 1, durationTurns: 2));

            Assert.That(action.DisplayName, Is.EqualTo("Lock Strike"));
            Assert.That(action.IntentIcon, Is.Null);
            Assert.That(action.Effect, Is.TypeOf<LockSlotEffectDefinition>());
        }

        [Test]
        public void EnemyActionPlannerFactory_Build_ReturnsPresentationMapForActionKeys()
        {
            MonsterTurnPatternDefinition pattern = Pattern(
                Turn(Action("Attack", Damage(3, CombatTargetMode.SelectedEnemy))));
            EnemyActionPlannerBuildResult result = new EnemyActionPlannerFactory().Build(pattern);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);

            EnemyActionPlan plan = result.Planner.PlanNext(Context(enemy));
            EnemyActionKey actionKey = plan.Actions[0].ActionKey;

            Assert.That(result.PresentationMap.TryGet(actionKey, out EnemyActionPresentation presentation), Is.True);
            Assert.That(presentation.DisplayName, Is.EqualTo("Attack"));
        }

        [Test]
        public void EnemyActionPlannerFactory_CreatePattern_NullPattern_ReturnsDefaultFallback()
        {
            IEnemyActionPlanner planner = new EnemyActionPlannerFactory().Create((MonsterTurnPatternDefinition)null);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);

            EnemyActionPlan plan = planner.PlanNext(Context(enemy));

            AssertPlan(plan, CombatEffectKind.Damage, 2);
        }

        [Test]
        public void EnemyActionPlannerFactory_CreatePattern_EmptyTurns_ReturnsEmptyPlan()
        {
            MonsterTurnPatternDefinition pattern = Pattern();
            IEnemyActionPlanner planner = new EnemyActionPlannerFactory().Create(pattern);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);

            EnemyActionPlan plan = planner.PlanNext(Context(enemy));

            Assert.That(plan.Effects, Is.Empty);
            Object.DestroyImmediate(pattern);
        }

        [Test]
        public void EnemyActionPlannerFactory_CreateTurnEffects_NullOrEmpty_ReturnsDefaultFallback()
        {
            var factory = new EnemyActionPlannerFactory();
            IEnemyActionPlanner fromNull = factory.Create((IReadOnlyList<IReadOnlyList<CombatEffect>>)null);
            IEnemyActionPlanner fromEmpty = factory.Create(System.Array.Empty<IReadOnlyList<CombatEffect>>());
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);

            AssertPlan(fromNull.PlanNext(Context(enemy)), CombatEffectKind.Damage, 2);
            AssertPlan(fromEmpty.PlanNext(Context(enemy)), CombatEffectKind.Damage, 2);
        }

        [Test]
        public void EnemyCombatantFactory_CreateDefinition_UsesMonsterHpAndEnemyIdentity()
        {
            MonsterDefinition definition = ScriptableObject.CreateInstance<MonsterDefinition>();
            definition.maxHp = 17;
            definition.turnPattern = Pattern(Turn(Action("Attack", Damage(6, CombatTargetMode.SelectedEnemy))));
            var factory = new EnemyCombatantFactory();

            EnemyCombatant combatant = factory.Create(definition, rosterIndex: 2);
            combatant.PlanNextAction(Context(combatant.Participant));

            Assert.That(combatant.Participant.MaxHp, Is.EqualTo(17));
            Assert.That(combatant.Participant.CurrentHp, Is.EqualTo(17));
            Assert.That(combatant.Participant.Team, Is.EqualTo(CombatTeam.Enemy));
            Assert.That(combatant.Participant.Id.Value, Is.EqualTo(102));
            AssertPlan(combatant.UpcomingPlan, CombatEffectKind.Damage, 6);
            Assert.That(typeof(EnemyCombatant).GetProperty("Definition"), Is.Null);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void RunEncounterRosterBuilder_BuildForTier_CreatesEnemyCombatant()
        {
            RunEncounterRoster roster = RunEncounterRosterBuilder.BuildForTier(
                SlotRogue.Data.GameFlow.EncounterTier.Normal,
                level: 1);
            BattleSystem battle = new();
            CombatParticipant player = RunCombatParticipantFactory.CreatePlayer(maxHp: 30, currentHp: 30);

            battle.StartBattle(player, new[] { roster.Enemies[0].Combatant });

            Assert.That(roster.Enemies.Count, Is.EqualTo(1));
            Assert.That(battle.TryGetUpcomingEnemyTurn(roster.Enemies[0].Combatant.Participant.Id, out EnemyUpcomingTurn upcoming), Is.True);
            AssertPlan(upcoming.Plan, CombatEffectKind.Damage, 4);
        }

        [Test]
        public void EnemyEncounterUnit_StoresCombatantAndFormationSlot()
        {
            EnemyCombatant combatant = new(
                Enemy(id: 101, maxHp: 12),
                new FixedSequenceEnemyActionPlanner(new[]
                {
                    Plan(new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy)),
                }));

            var unit = new EnemyEncounterUnit(combatant, formationSlot: 2);

            Assert.That(unit.Combatant, Is.SameAs(combatant));
            Assert.That(unit.FormationSlot, Is.EqualTo(2));
        }

        [Test]
        public void RunEncounterRoster_StoresEnemyUnitsInOrder()
        {
            EnemyCombatant first = new(
                Enemy(id: 101, maxHp: 12),
                new FixedSequenceEnemyActionPlanner(new[]
                {
                    Plan(new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy)),
                }));
            EnemyCombatant second = new(
                Enemy(id: 102, maxHp: 14),
                new FixedSequenceEnemyActionPlanner(new[]
                {
                    Plan(new CombatEffect(CombatEffectKind.Shield, 4, CombatEffectTarget.Self)),
                }));
            var source = new[]
            {
                new EnemyEncounterUnit(first, formationSlot: 1),
                new EnemyEncounterUnit(second, formationSlot: 0),
            };

            RunEncounterRoster roster = new(source);

            source[0] = new EnemyEncounterUnit(second, formationSlot: 2);
            Assert.That(roster.Enemies.Count, Is.EqualTo(2));
            Assert.That(roster.Enemies[0].Combatant, Is.SameAs(first));
            Assert.That(roster.Enemies[0].FormationSlot, Is.EqualTo(1));
            Assert.That(roster.Enemies[1].Combatant, Is.SameAs(second));
            Assert.That(roster.Enemies[0].Combatant.Participant, Is.SameAs(first.Participant));
        }

        private static MonsterTurnPatternDefinition Pattern(params MonsterTurnStepDefinition[] turns)
        {
            MonsterTurnPatternDefinition pattern = ScriptableObject.CreateInstance<MonsterTurnPatternDefinition>();
            pattern.turns = turns;
            return pattern;
        }

        private static MonsterTurnStepDefinition Turn(params EnemyActionDefinition[] actions)
        {
            return new MonsterTurnStepDefinition { actions = actions };
        }

        private static EnemyActionDefinition Action(
            string displayName,
            EnemyEffectDefinition effect)
        {
            return new EnemyActionDefinition(displayName, intentIcon: null, effect);
        }

        private static DamageEffectDefinition Damage(
            int amount,
            CombatTargetMode targetMode,
            int targetParticipantId = 0)
        {
            return new DamageEffectDefinition(
                amount,
                new CombatEffectTargetDefinition(targetMode, targetParticipantId));
        }

        private static ShieldEffectDefinition Shield(
            int amount,
            CombatTargetMode targetMode)
        {
            return new ShieldEffectDefinition(
                amount,
                new CombatEffectTargetDefinition(targetMode));
        }

        private static HealEffectDefinition Heal(
            int amount,
            CombatTargetMode targetMode)
        {
            return new HealEffectDefinition(
                amount,
                new CombatEffectTargetDefinition(targetMode));
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

        private static EnemyActionPlan Plan(params CombatEffect[] effects)
        {
            return new EnemyActionPlan(effects);
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
