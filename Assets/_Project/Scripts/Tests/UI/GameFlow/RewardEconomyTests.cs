using NUnit.Framework;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Tests.GameFlow
{
    /// <summary>
    /// 보상 수치 규칙(RewardEconomy)의 순수 계산을 검증합니다.
    /// GameFlowSession 전역 상태 없이 RunVitals만으로 결정적으로 테스트할 수 있습니다.
    /// </summary>
    public sealed class RewardEconomyTests
    {
        private static RunVitals Vitals(int max, int current) =>
            new(max, current, damageBonus: 0, defenseBonus: 0);

        [Test]
        public void Heal_DoesNotExceedMaxHp()
        {
            RunVitals result = RewardEconomy.Apply(Vitals(100, 96), RunRewardType.Heal);

            Assert.That(result.CurrentHp, Is.EqualTo(100));
        }

        [Test]
        public void Heal_AddsHealAmountWhenBelowMax()
        {
            RunVitals result = RewardEconomy.Apply(Vitals(100, 50), RunRewardType.Heal);

            Assert.That(result.CurrentHp, Is.EqualTo(50 + RewardEconomy.HealAmount));
        }

        [Test]
        public void FullHeal_RestoresToMax()
        {
            RunVitals result = RewardEconomy.Apply(Vitals(100, 1), RunRewardType.FullHeal);

            Assert.That(result.CurrentHp, Is.EqualTo(100));
        }

        [Test]
        public void MaxHpUp_RaisesMaxAndHealsBySameAmount()
        {
            RunVitals result = RewardEconomy.Apply(Vitals(100, 100), RunRewardType.MaxHpUp);

            Assert.That(result.MaxHp, Is.EqualTo(100 + RewardEconomy.MaxHpUpAmount));
            Assert.That(result.CurrentHp, Is.EqualTo(100 + RewardEconomy.MaxHpUpAmount));
        }

        [Test]
        public void DamageBonus_Accumulates()
        {
            RunVitals once = RewardEconomy.Apply(Vitals(100, 100), RunRewardType.DamageBonus);
            RunVitals twice = RewardEconomy.Apply(once, RunRewardType.GreaterDamage);

            Assert.That(twice.DamageBonus,
                Is.EqualTo(RewardEconomy.DamageBonusStep + RewardEconomy.GreaterDamageStep));
        }

        [Test]
        public void DefenseBonus_Accumulates()
        {
            RunVitals result = RewardEconomy.Apply(Vitals(100, 100), RunRewardType.GreaterDefense);

            Assert.That(result.DefenseBonus, Is.EqualTo(RewardEconomy.GreaterDefenseStep));
        }
    }
}
