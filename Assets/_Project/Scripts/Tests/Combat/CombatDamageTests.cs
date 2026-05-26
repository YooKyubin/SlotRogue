using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class CombatDamageTests
    {
        [Test]
        public void Apply_SubtractsDefense_FromRawAttack()
        {
            Assert.That(CombatDamage.Apply(12, 5), Is.EqualTo(7));
        }

        [Test]
        public void Apply_ClampsToZero_WhenDefenseExceedsAttack()
        {
            Assert.That(CombatDamage.Apply(5, 10), Is.EqualTo(0));
        }

        [Test]
        public void Apply_ReturnsZero_WhenAttackIsZero()
        {
            Assert.That(CombatDamage.Apply(0, 5), Is.EqualTo(0));
        }
    }
}
