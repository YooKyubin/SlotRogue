using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using UnityEngine;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class BattleResolverTests
    {
        private const int DefaultPlayerMaxHp = 30;
        private const int DefaultMonsterMaxHp = 50;

        [Test]
        public void ProcessSpin_DefendThenAttack_AppliesPendingDefenseOnNextSpin()
        {
            var defend = CreateAction(MonsterActionKind.Defend, defendValue: 5);
            var attack = CreateAction(MonsterActionKind.Attack, rawAttack: 1);
            var resolver = CreateResolver(defend, attack);

            resolver.ProcessSpin(new CombatSpinOutcome(0, 0));
            resolver.ProcessSpin(new CombatSpinOutcome(12, 0));

            Assert.That(resolver.State.MonsterHp, Is.EqualTo(DefaultMonsterMaxHp - 7));
            Assert.That(resolver.State.PendingMonsterDefense, Is.EqualTo(0));
        }

        [Test]
        public void ProcessSpin_AttackZero_KeepsPendingDefense()
        {
            var defend = CreateAction(MonsterActionKind.Defend, defendValue: 5);
            var resolver = CreateResolver(defend);

            resolver.ProcessSpin(new CombatSpinOutcome(0, 0));
            resolver.ProcessSpin(new CombatSpinOutcome(0, 0));

            Assert.That(resolver.State.MonsterHp, Is.EqualTo(DefaultMonsterMaxHp));
            Assert.That(resolver.State.PendingMonsterDefense, Is.EqualTo(5));
        }

        [Test]
        public void ProcessSpin_MonsterAttack_AppliesPlayerDefenseFromSpin()
        {
            var attack = CreateAction(MonsterActionKind.Attack, rawAttack: 10);
            var resolver = CreateResolver(attack);

            resolver.ProcessSpin(new CombatSpinOutcome(0, 4));

            Assert.That(resolver.State.PlayerHp, Is.EqualTo(DefaultPlayerMaxHp - 6));
        }

        [Test]
        public void ProcessSpin_PatternIndex_AdvancesAndLoops()
        {
            var step0 = CreateAction(MonsterActionKind.Defend, defendValue: 1);
            var step1 = CreateAction(MonsterActionKind.Defend, defendValue: 2);
            var step2 = CreateAction(MonsterActionKind.Defend, defendValue: 3);
            var resolver = CreateResolver(step0, step1, step2);

            resolver.ProcessSpin(new CombatSpinOutcome(0, 0));
            Assert.That(resolver.State.PatternIndex, Is.EqualTo(1));
            Assert.That(resolver.State.PendingMonsterDefense, Is.EqualTo(1));

            resolver.ProcessSpin(new CombatSpinOutcome(0, 0));
            Assert.That(resolver.State.PatternIndex, Is.EqualTo(2));
            Assert.That(resolver.State.PendingMonsterDefense, Is.EqualTo(2));

            resolver.ProcessSpin(new CombatSpinOutcome(0, 0));
            Assert.That(resolver.State.PatternIndex, Is.EqualTo(0));
            Assert.That(resolver.State.PendingMonsterDefense, Is.EqualTo(3));
        }

        [Test]
        public void ProcessSpin_OverrideRawAttack_UsesStepOverride()
        {
            var attack = CreateAction(MonsterActionKind.Attack, rawAttack: 5);
            var pattern = CreatePattern(loop: true, CreateStep(attack, overrideRawAttack: true, overrideValue: 15));
            var monster = CreateMonster(pattern);
            var resolver = new BattleResolver(monster, DefaultPlayerMaxHp);

            resolver.ProcessSpin(new CombatSpinOutcome(0, 0));

            Assert.That(resolver.State.PlayerHp, Is.EqualTo(DefaultPlayerMaxHp - 15));
        }

        [Test]
        public void OnSpinResolved_MonsterHpZero_EndsWithVictory()
        {
            var resolver = CreateResolver(CreateAction(MonsterActionKind.Defend, defendValue: 1));
            resolver.State.MonsterHp = 10;

            resolver.OnSpinResolved(new CombatSpinOutcome(10, 0));

            Assert.That(resolver.State.EndReason, Is.EqualTo(BattleEndReason.Victory));
            Assert.That(resolver.State.IsBattleOver, Is.True);
        }

        [Test]
        public void ProcessSpin_MonsterHpZero_EndsWithVictory()
        {
            var resolver = CreateResolver(CreateAction(MonsterActionKind.Defend, defendValue: 1));
            resolver.State.MonsterHp = 10;

            resolver.ProcessSpin(new CombatSpinOutcome(10, 0));

            Assert.That(resolver.State.EndReason, Is.EqualTo(BattleEndReason.Victory));
            Assert.That(resolver.State.IsBattleOver, Is.True);
        }

        [Test]
        public void ProcessSpin_PlayerHpZero_EndsWithDefeat()
        {
            var attack = CreateAction(MonsterActionKind.Attack, rawAttack: 30);
            var resolver = CreateResolver(attack);

            resolver.ProcessSpin(new CombatSpinOutcome(0, 0));

            Assert.That(resolver.State.EndReason, Is.EqualTo(BattleEndReason.Defeat));
        }

        [Test]
        public void ProcessSpin_SimultaneousDeath_PrefersDefeat()
        {
            var attack = CreateAction(MonsterActionKind.Attack, rawAttack: 30);
            var resolver = CreateResolver(attack);
            resolver.State.MonsterHp = 10;

            resolver.ProcessSpin(new CombatSpinOutcome(10, 0));

            Assert.That(resolver.State.MonsterHp, Is.LessThanOrEqualTo(0));
            Assert.That(resolver.State.PlayerHp, Is.LessThanOrEqualTo(0));
            Assert.That(resolver.State.EndReason, Is.EqualTo(BattleEndReason.Defeat));
        }

        [Test]
        public void ProcessSpin_AttackZero_StillAdvancesPatternIndex()
        {
            var step0 = CreateAction(MonsterActionKind.Defend, defendValue: 1);
            var step1 = CreateAction(MonsterActionKind.Defend, defendValue: 2);
            var resolver = CreateResolver(step0, step1);

            resolver.ProcessSpin(new CombatSpinOutcome(0, 0));

            Assert.That(resolver.State.PatternIndex, Is.EqualTo(1));
            Assert.That(resolver.State.MonsterHp, Is.EqualTo(DefaultMonsterMaxHp));
        }

        [Test]
        public void ProcessSpin_WhenBattleOver_IsNoOp()
        {
            var resolver = CreateResolver(CreateAction(MonsterActionKind.Defend, defendValue: 1));
            resolver.State.MonsterHp = 0;
            resolver.State.EndReason = BattleEndReason.Victory;
            resolver.State.PatternIndex = 2;
            resolver.State.PlayerHp = 10;

            resolver.ProcessSpin(new CombatSpinOutcome(5, 0));

            Assert.That(resolver.State.PatternIndex, Is.EqualTo(2));
            Assert.That(resolver.State.PlayerHp, Is.EqualTo(10));
        }

        [Test]
        public void Begin_SetsHpToMaxValues()
        {
            var monster = CreateMonster(CreatePattern(loop: true));
            monster.MaxHp = 40;

            var state = BattleState.Begin(monster, DefaultPlayerMaxHp);

            Assert.That(state.PlayerHp, Is.EqualTo(DefaultPlayerMaxHp));
            Assert.That(state.MonsterHp, Is.EqualTo(40));
            Assert.That(state.EndReason, Is.EqualTo(BattleEndReason.None));
        }

        private static BattleResolver CreateResolver(params MonsterActionDefinition[] steps)
        {
            var pattern = CreatePattern(loop: true, CreateSteps(steps));
            return new BattleResolver(CreateMonster(pattern), DefaultPlayerMaxHp);
        }

        private static MonsterDefinition CreateMonster(MonsterPattern pattern)
        {
            var monster = ScriptableObject.CreateInstance<MonsterDefinition>();
            monster.MaxHp = DefaultMonsterMaxHp;
            monster.Pattern = pattern;
            return monster;
        }

        private static MonsterPattern CreatePattern(bool loop, params PatternStep[] steps)
        {
            var pattern = ScriptableObject.CreateInstance<MonsterPattern>();
            pattern.Loop = loop;
            pattern.Steps = steps;
            return pattern;
        }

        private static PatternStep[] CreateSteps(MonsterActionDefinition[] definitions)
        {
            var steps = new PatternStep[definitions.Length];
            for (var index = 0; index < definitions.Length; index++)
            {
                steps[index] = CreateStep(definitions[index]);
            }

            return steps;
        }

        private static PatternStep CreateStep(
            MonsterActionDefinition action,
            bool overrideRawAttack = false,
            int overrideValue = 0)
        {
            return new PatternStep
            {
                Action = action,
                OverrideRawAttack = overrideRawAttack,
                OverrideRawAttackValue = overrideValue
            };
        }

        private static MonsterActionDefinition CreateAction(
            MonsterActionKind kind,
            int rawAttack = 0,
            int defendValue = 0)
        {
            var action = ScriptableObject.CreateInstance<MonsterActionDefinition>();
            action.Kind = kind;
            action.RawAttack = rawAttack;
            action.DefendValue = defendValue;
            return action;
        }
    }
}
