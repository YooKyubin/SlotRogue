using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class EnemyActionPlanTests
    {
        [Test]
        public void Constructor_CopiesSourceEffects()
        {
            var source = new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy),
            };
            var plan = new EnemyActionPlan(source);

            source[0] = new CombatEffect(CombatEffectKind.Shield, 9, CombatEffectTarget.Self);

            Assert.That(plan.Effects[0].Kind, Is.EqualTo(CombatEffectKind.Damage));
            Assert.That(plan.Effects[0].Amount, Is.EqualTo(3));
        }

        [Test]
        public void Effects_ReturnsCopy()
        {
            var plan = new EnemyActionPlan(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy),
            });

            IReadOnlyList<CombatEffect> firstRead = plan.Effects;
            IReadOnlyList<CombatEffect> secondRead = plan.Effects;

            Assert.That(firstRead, Is.Not.SameAs(secondRead));
            Assert.That(secondRead[0].Kind, Is.EqualTo(CombatEffectKind.Damage));
            Assert.That(secondRead[0].Amount, Is.EqualTo(3));
        }

        [Test]
        public void FromActions_PreservesActionBoundaries()
        {
            EnemyActionPlan plan = EnemyActionPlan.FromActions(new[]
            {
                new EnemyPlannedAction(
                    new EnemyActionKey(1),
                    new[]
                    {
                        EnemyActionEffect.FromCombatEffect(
                            new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy)),
                    }),
                new EnemyPlannedAction(
                    new EnemyActionKey(2),
                    new[]
                    {
                        EnemyActionEffect.FromCombatEffect(
                            new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy)),
                        EnemyActionEffect.LockSlot(lockCount: 1, durationTurns: 2),
                    }),
            });

            Assert.That(plan.Actions.Count, Is.EqualTo(2));
            Assert.That(plan.Actions[0].ActionKey.Value, Is.EqualTo(1));
            Assert.That(plan.Actions[1].ActionKey.Value, Is.EqualTo(2));
            Assert.That(plan.Actions[1].Effects.Count, Is.EqualTo(2));
            Assert.That(plan.Actions[1].Effects[1].Kind, Is.EqualTo(EnemyActionEffectKind.LockSlot));
            Assert.That(plan.Effects.Count, Is.EqualTo(2));
        }

        [Test]
        public void Actions_ReturnsCopy()
        {
            EnemyActionPlan plan = EnemyActionPlan.FromActions(new[]
            {
                new EnemyPlannedAction(
                    new EnemyActionKey(1),
                    new[]
                    {
                        EnemyActionEffect.FromCombatEffect(
                            new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy)),
                    }),
            });

            IReadOnlyList<EnemyPlannedAction> firstRead = plan.Actions;
            IReadOnlyList<EnemyPlannedAction> secondRead = plan.Actions;

            Assert.That(firstRead, Is.Not.SameAs(secondRead));
            Assert.That(secondRead[0].ActionKey.Value, Is.EqualTo(1));
        }

        [Test]
        public void NullEffects_BecomesEmptyPlan()
        {
            var plan = new EnemyActionPlan(null);

            Assert.That(plan.Effects, Is.Empty);
        }
    }
}
