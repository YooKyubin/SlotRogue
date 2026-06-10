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
            CombatParticipant target = CombatParticipantFactory.CreateEnemy(maxHp: 20);

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
            CombatParticipant target = CombatParticipantFactory.CreateEnemy(maxHp: 20, shield: 3);

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
            CombatParticipant target = CombatParticipantFactory.CreateEnemy(maxHp: 20, shield: 10);

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
            CombatParticipant target = CombatParticipantFactory.CreateEnemy(maxHp: 20);

            EffectApplyResult result = _applicator.ApplyToParticipant(
                new CombatEffect(CombatEffectKind.Shield, 5, CombatEffectTarget.Self),
                target);

            Assert.That(result.ShieldGained, Is.EqualTo(5));
            Assert.That(target.Shield, Is.EqualTo(5));
        }

        [Test]
        public void ApplyHeal_CapsAtMaxHp()
        {
            CombatParticipant target = CombatParticipantFactory.CreateEnemy(maxHp: 20, currentHp: 18);

            EffectApplyResult result = _applicator.ApplyToParticipant(
                new CombatEffect(CombatEffectKind.Heal, 10, CombatEffectTarget.Self),
                target);

            Assert.That(result.HealApplied, Is.EqualTo(2));
            Assert.That(target.CurrentHp, Is.EqualTo(20));
        }
    }

    internal static class CombatParticipantFactory
    {
        public static CombatParticipant CreatePlayer(
            int maxHp = 30,
            int currentHp = -1,
            int shield = 0,
            int id = 1)
        {
            return CreateParticipant(maxHp, currentHp, shield, id, CombatTeam.Player);
        }

        public static CombatParticipant CreateEnemy(
            int maxHp = 20,
            int currentHp = -1,
            int shield = 0,
            int id = 100)
        {
            return CreateParticipant(maxHp, currentHp, shield, id, CombatTeam.Enemy);
        }

        public static CombatParticipant CreateParticipant(
            int maxHp,
            int currentHp,
            int shield,
            int id,
            CombatTeam team)
        {
            return new CombatParticipant(
                maxHp,
                currentHp,
                shield,
                new CombatParticipantId(id),
                team);
        }
    }
}
