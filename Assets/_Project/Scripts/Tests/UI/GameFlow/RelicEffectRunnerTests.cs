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
            Assert.That(result.Contributions, Has.Count.EqualTo(1));
            Assert.That(result.Contributions[0].RelicId, Is.EqualTo("C-01"));
            Assert.That(result.Contributions[0].DamagePerHit, Is.EqualTo(4));
            Assert.That(result.Contributions[0].TriggerPatternIndex, Is.Zero);
        }

        [Test]
        public void SymbolRelic_DoesNotTrigger_WhenSymbolAbsent()
        {
            var owned = new[] { RelicCatalog.GetById("C-01") };

            RelicResolveResult result = _runner.Resolve(Matches(SlotSymbolType.Lemon, 3), owned, FullHp());

            Assert.That(result.AdditionalDamage, Is.EqualTo(0));
        }

        [Test]
        public void ContributionAccumulator_MultipliesDamageByAttackCount()
        {
            RelicResolveResult result = _runner.Resolve(
                Matches(SlotSymbolType.Cherry, 3),
                new[] { RelicCatalog.GetById("C-01") },
                FullHp());
            var accumulator = new RelicContributionAccumulator();

            accumulator.RecordTurn(result.Contributions, attackCount: 3);
            IReadOnlyList<RelicContributionSnapshot> snapshot = accumulator.Snapshot();

            Assert.That(snapshot, Has.Count.EqualTo(1));
            Assert.That(snapshot[0].TriggerCount, Is.EqualTo(1));
            Assert.That(snapshot[0].Damage, Is.EqualTo(12));
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
        public void TagRelic_DoesNotDoubleCountOverlappingCells()
        {
            var owned = new[] { RelicCatalog.GetById("C-10") };
            IReadOnlyList<SlotCell> sharedCells = new[]
            {
                new SlotCell(0, 0),
                new SlotCell(1, 0),
                new SlotCell(2, 0),
            };
            var overlappingMatches = new[]
            {
                Single(SlotSymbolType.Cherry, sharedCells),
                Single(SlotSymbolType.Cherry, sharedCells),
            };

            RelicResolveResult result = _runner.Resolve(overlappingMatches, owned, FullHp());

            Assert.That(result.AdditionalDamage, Is.EqualTo(0));
        }

        [Test]
        public void TagRelic_RecordsPatternWhereAccumulatedThresholdIsReached()
        {
            var owned = new[] { RelicCatalog.GetById("C-10") };
            var matches = new[]
            {
                Single(
                    SlotSymbolType.Cherry,
                    new[]
                    {
                        new SlotCell(0, 0),
                        new SlotCell(1, 0),
                    }),
                Single(
                    SlotSymbolType.Lemon,
                    new[]
                    {
                        new SlotCell(2, 0),
                        new SlotCell(3, 0),
                    }),
            };

            RelicResolveResult result = _runner.Resolve(matches, owned, FullHp());

            Assert.That(result.Contributions, Has.Count.EqualTo(1));
            Assert.That(result.Contributions[0].TriggerPatternIndex, Is.EqualTo(1));
        }

        [Test]
        public void BurnRelic_IsExcludedUntilCombatCoreMatchesV23()
        {
            RelicDefinition c07 = RelicCatalog.GetById("C-07");

            RelicResolveResult result = _runner.Resolve(
                Matches(SlotSymbolType.Cherry, 4),
                new[] { c07 },
                FullHp());

            Assert.That(c07.Phase1, Is.False);
            Assert.That(result.StatusEffectsToApply, Is.Empty);
        }

        [Test]
        public void InfectRelic_IsExcludedUntilCombatCoreMatchesV23()
        {
            RelicDefinition c08 = RelicCatalog.GetById("C-08");

            RelicResolveResult result = _runner.Resolve(
                Matches(SlotSymbolType.Clover, 4),
                new[] { c08 },
                FullHp());

            Assert.That(c08.Phase1, Is.False);
            Assert.That(result.StatusEffectsToApply, Is.Empty);
        }

        [Test]
        public void VulnerableRelic_IsNotPhase1_AndProducesNothing()
        {
            RelicDefinition c09 = RelicCatalog.GetById("C-09"); // 취약 — Phase 2
            Assert.That(c09.Phase1, Is.False, "취약 유물은 Phase 1 미지원이어야 한다");

            var owned = new[] { c09 };
            RelicResolveResult result = _runner.Resolve(Matches(SlotSymbolType.Lemon, 4), owned, FullHp());

            Assert.That(result.StatusEffectsToApply.Count, Is.EqualTo(0), "취약은 전투에 넘기지 않는다");
            Assert.That(result.AdditionalDamage, Is.EqualTo(0));
        }

        [Test]
        public void BurnChaser_IsExcludedUntilCombatCoreMatchesV23()
        {
            RelicDefinition u16 = RelicCatalog.GetById("U-16");
            var burningEnemy = new RelicBattleContext(30, 30, 40, 40, true, enemyHasBurn: true);

            RelicResolveResult result = _runner.Resolve(
                Matches(SlotSymbolType.Cherry, 3),
                new[] { u16 },
                burningEnemy);

            Assert.That(u16.Phase1, Is.False);
            Assert.That(result.AdditionalDamage, Is.EqualTo(0));
        }

        [Test]
        public void StatusChaser_IsExcludedUntilAllV23StatusesAreQueryable()
        {
            RelicDefinition u13 = RelicCatalog.GetById("U-13");
            var infectedEnemy = new RelicBattleContext(30, 30, 40, 40, true, enemyHasInfect: true);

            RelicResolveResult result = _runner.Resolve(
                Matches(SlotSymbolType.Cherry, 3),
                new[] { u13 },
                infectedEnemy);

            Assert.That(u13.Phase1, Is.False);
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

        [Test]
        public void MultipleOwnedRelics_AccumulateAcrossMatchedPatterns()
        {
            var owned = new[]
            {
                RelicCatalog.GetById("S-01"),
                RelicCatalog.GetById("S-02"),
                RelicCatalog.GetById("S-03"),
            };
            var matches = new[]
            {
                Single(SlotSymbolType.Cherry, 3),
                Single(SlotSymbolType.Clover, 3),
                Single(SlotSymbolType.Bell, 3),
            };

            RelicResolveResult result = _runner.Resolve(matches, owned, FullHp());

            Assert.That(result.AdditionalDamage, Is.EqualTo(3));
            Assert.That(result.AdditionalBlock, Is.EqualTo(3));
            Assert.That(result.HealAmount, Is.EqualTo(2));
            Assert.That(result.ActivationSummary, Does.Contain("체리 단검"));
            Assert.That(result.ActivationSummary, Does.Contain("클로버 방패"));
            Assert.That(result.ActivationSummary, Does.Contain("종 치료제"));
            Assert.That(result.Contributions, Has.Count.EqualTo(3));
        }

        [Test]
        public void RelicTurnResolver_DelegatesOwnedRelicsPatternsAndBattleContext()
        {
            var resolver = new RelicTurnResolver(_runner);
            var context = new RelicTurnContext(new RelicBattleContext(10, 30, 40, 40, false));

            RelicResolveResult result = resolver.Resolve(
                new[] { RelicCatalog.GetById("U-03") },
                Matches(SlotSymbolType.Clover, 3),
                context);

            Assert.That(result.HealAmount, Is.EqualTo(6));
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

            return Single(symbol, cells);
        }

        private static SlotPatternMatch Single(
            SlotSymbolType symbol,
            IReadOnlyList<SlotCell> cells)
        {
            var definition = new SlotPatternDefinition(
                "test", "Test", 0, 1f, SlotPatternRank.HorizontalSm, false, cells);
            return new SlotPatternMatch(definition, symbol, cells, cells.Count, 0);
        }
    }
}
