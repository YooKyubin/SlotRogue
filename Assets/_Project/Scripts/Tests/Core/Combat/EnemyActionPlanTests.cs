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
        public void NullEffects_BecomesEmptyPlan()
        {
            var plan = new EnemyActionPlan(null);

            Assert.That(plan.Effects, Is.Empty);
        }
    }
}
