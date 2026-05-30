using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class EffectApplicatorTests
    {
        private EffectApplicator _applicator = null!;

        [SetUp]
        public void SetUp()
        {
            _applicator = new EffectApplicator();
        }

        [Test]
        public void ApplyDamage_ReducesHp_WhenShieldIsZero()
        {
            var target = new CombatParticipant(maxHp: 20, currentHp: 20);

            EffectApplyResult result = _applicator.ApplyToParticipant(
                new CombatEffect(CombatEffectKind.Damage, 7, CombatEffectTarget.Self),
                target);

            Assert.That(result.DamageDealt, Is.EqualTo(7));
            Assert.That(result.ShieldConsumed, Is.Zero);
            Assert.That(target.CurrentHp, Is.EqualTo(13));
            Assert.That(target.Shield, Is.Zero);
        }

        [Test]
        public void ApplyDamage_ConsumesShieldBeforeHp()
        {
            var target = new CombatParticipant(maxHp: 20, currentHp: 20, shield: 3);

            EffectApplyResult result = _applicator.ApplyToParticipant(
                new CombatEffect(CombatEffectKind.Damage, 10, CombatEffectTarget.Self),
                target);

            Assert.That(result.ShieldConsumed, Is.EqualTo(3));
            Assert.That(result.DamageDealt, Is.EqualTo(7));
            Assert.That(target.Shield, Is.Zero);
            Assert.That(target.CurrentHp, Is.EqualTo(13));
        }

        [Test]
        public void ApplyDamage_FullyBlockedByShield_DoesNotReduceHp()
        {
            var target = new CombatParticipant(maxHp: 20, currentHp: 20, shield: 10);

            EffectApplyResult result = _applicator.ApplyToParticipant(
                new CombatEffect(CombatEffectKind.Damage, 6, CombatEffectTarget.Self),
                target);

            Assert.That(result.DamageDealt, Is.Zero);
            Assert.That(result.ShieldConsumed, Is.EqualTo(6));
            Assert.That(target.Shield, Is.EqualTo(4));
            Assert.That(target.CurrentHp, Is.EqualTo(20));
        }

        [Test]
        public void ApplyShield_IncreasesShield()
        {
            var target = new CombatParticipant(maxHp: 20);

            EffectApplyResult result = _applicator.ApplyToParticipant(
                new CombatEffect(CombatEffectKind.Shield, 5, CombatEffectTarget.Self),
                target);

            Assert.That(result.ShieldGained, Is.EqualTo(5));
            Assert.That(target.Shield, Is.EqualTo(5));
        }

        [Test]
        public void ApplyHeal_CapsAtMaxHp()
        {
            var target = new CombatParticipant(maxHp: 20, currentHp: 18);

            EffectApplyResult result = _applicator.ApplyToParticipant(
                new CombatEffect(CombatEffectKind.Heal, 10, CombatEffectTarget.Self),
                target);

            Assert.That(result.HealApplied, Is.EqualTo(2));
            Assert.That(target.CurrentHp, Is.EqualTo(20));
        }

        [Test]
        public void Apply_EnemyTarget_AppliesToOpponent()
        {
            var player = new CombatParticipant(maxHp: 30, currentHp: 30);
            var monster = new CombatParticipant(maxHp: 15, currentHp: 15);

            _applicator.Apply(
                new CombatEffect(CombatEffectKind.Damage, 6, CombatEffectTarget.Enemy),
                player,
                monster);

            Assert.That(monster.CurrentHp, Is.EqualTo(9));
            Assert.That(player.CurrentHp, Is.EqualTo(30));
        }

        [Test]
        public void Apply_SelfTarget_AppliesToSource()
        {
            var player = new CombatParticipant(maxHp: 30, currentHp: 20);
            var monster = new CombatParticipant(maxHp: 15, currentHp: 15);

            _applicator.Apply(
                new CombatEffect(CombatEffectKind.Heal, 5, CombatEffectTarget.Self),
                player,
                monster);

            Assert.That(player.CurrentHp, Is.EqualTo(25));
            Assert.That(monster.CurrentHp, Is.EqualTo(15));
        }
    }
}
