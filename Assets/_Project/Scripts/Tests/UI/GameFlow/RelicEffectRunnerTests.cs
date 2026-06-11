using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class RelicEffectRunnerTests
    {
        private RelicEffectRunner _runner = null!;

        [SetUp]
        public void SetUp()
        {
            _runner = new RelicEffectRunner();
        }

        private static RelicBattleContext FullHp() => new RelicBattleContext(30, 30, 40, 40, false);

        [Test]
        public void SymbolRelic_AddsDamage_WhenSymbolMatched()
        {
            var owned = new[] { RelicCatalog.GetById("C-01") }; // 체리 3개 이상 → 피해 +4

            RelicResolveResult result = _runner.Resolve(Matches(SlotSymbolType.Cherry, 3), owned, FullHp());

            Assert.That(result.AdditionalDamage, Is.EqualTo(4));
            Assert.That(result.AdditionalBlock, Is.EqualTo(0));
            Assert.That(result.HealAmount, Is.EqualTo(0));
        }

        [Test]
        public void SymbolRelic_DoesNotTrigger_WhenSymbolAbsent()
        {
            var owned = new[] { RelicCatalog.GetById("C-01") };

            RelicResolveResult result = _runner.Resolve(Matches(SlotSymbolType.Lemon, 3), owned, FullHp());

            Assert.That(result.AdditionalDamage, Is.EqualTo(0));
        }

        [Test]
        public void TagRelic_AddsDamage_WhenTagCountReachesFour()
        {
            var owned = new[] { RelicCatalog.GetById("C-10") }; // 과일 태그 4개 이상 → 피해 +6

            RelicResolveResult result = _runner.Resolve(Matches(SlotSymbolType.Cherry, 4), owned, FullHp());

            Assert.That(result.AdditionalDamage, Is.EqualTo(6));
        }

        [Test]
        public void TagRelic_DoesNotTrigger_BelowThreshold()
        {
            var owned = new[] { RelicCatalog.GetById("C-10") };

            RelicResolveResult result = _runner.Resolve(Matches(SlotSymbolType.Cherry, 3), owned, FullHp());

            Assert.That(result.AdditionalDamage, Is.EqualTo(0));
        }

        [Test]
        public void BurnRelic_ProducesBurnRequest()
        {
            var owned = new[] { RelicCatalog.GetById("C-07") }; // 붉은 태그 4 → 화상 1 (Burn 지원)

            RelicResolveResult result = _runner.Resolve(Matches(SlotSymbolType.Cherry, 4), owned, FullHp());

            Assert.That(result.StatusEffectsToApply.Count, Is.EqualTo(1));
            Assert.That(result.StatusEffectsToApply[0].Kind, Is.EqualTo(StatusEffectKind.Burn));
            Assert.That(result.StatusEffectsToApply[0].Stacks, Is.EqualTo(1));
        }

        [Test]
        public void CorrosionRelic_IsNotPhase1_AndProducesNothing()
        {
            RelicDefinition c08 = RelicCatalog.GetById("C-08"); // 부식 — Phase 2
            Assert.That(c08.Phase1, Is.False, "부식 유물은 Phase 1 미지원이어야 한다");

            var owned = new[] { c08 };
            RelicResolveResult result = _runner.Resolve(Matches(SlotSymbolType.Clover, 4), owned, FullHp());

            Assert.That(result.StatusEffectsToApply.Count, Is.EqualTo(0), "부식은 전투에 넘기지 않는다");
            Assert.That(result.AdditionalDamage, Is.EqualTo(0));
        }

        [Test]
        public void HealRelic_AddsConditionalBonus_WhenPlayerHpLow()
        {
            var owned = new[] { RelicCatalog.GetById("U-03") }; // HP+4, HP 50% 이하면 +2
            var lowHp = new RelicBattleContext(10, 30, 40, 40, false); // 33% ≤ 50%

            RelicResolveResult result = _runner.Resolve(Matches(SlotSymbolType.Clover, 3), owned, lowHp);

            Assert.That(result.HealAmount, Is.EqualTo(6)); // 4 + 2
        }

        [Test]
        public void EnemyHpGate_BlocksWhenEnemyHealthy()
        {
            var owned = new[] { RelicCatalog.GetById("U-01") }; // 적 HP 50% 이하 조건
            var healthyEnemy = new RelicBattleContext(30, 30, 40, 40, false); // 적 100%

            RelicResolveResult result = _runner.Resolve(Matches(SlotSymbolType.Cherry, 3), owned, healthyEnemy);

            Assert.That(result.AdditionalDamage, Is.EqualTo(0));
        }

        [Test]
        public void NonPhase1Relic_IsIgnored()
        {
            var owned = new[] { RelicCatalog.GetById("R-05") }; // 패시브 배율(Phase 2)

            RelicResolveResult result = _runner.Resolve(Matches(SlotSymbolType.Clover, 4), owned, FullHp());

            Assert.That(result.AdditionalDamage, Is.EqualTo(0));
        }

        // ── helpers ──────────────────────────────────────────────────────

        private static IReadOnlyList<SlotPatternMatch> Matches(SlotSymbolType symbol, int cellCount) =>
            new[] { Single(symbol, cellCount) };

        private static SlotPatternMatch Single(SlotSymbolType symbol, int cellCount)
        {
            var cells = new List<SlotCell>(cellCount);
            for (int i = 0; i < cellCount; i++)
            {
                cells.Add(new SlotCell(i, 0));
            }

            var definition = new SlotPatternDefinition(
                "test", "Test", 0, 1f, SlotPatternRank.HorizontalSm, false, cells);
            return new SlotPatternMatch(definition, symbol, cells, cellCount, 0);
        }
    }
}
