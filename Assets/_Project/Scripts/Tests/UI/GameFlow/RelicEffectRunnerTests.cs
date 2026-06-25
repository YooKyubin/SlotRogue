using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;
using SlotRogue.UI.Combat;
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
        public void LifestealRelic_EmitsDerivedHealRule_WithoutImmediateHeal()
        {
            var owned = new[] { RelicCatalog.GetById("C-16") }; // 레몬 흡혈 12% (턴당 6)

            RelicResolveResult result = _runner.Resolve(Matches(SlotSymbolType.Lemon, 3), owned, FullHp());

            // 회복량은 최종 피해 확정 후 빌더가 계산하므로 이 단계에선 0.
            Assert.That(result.HealAmount, Is.EqualTo(0));
            Assert.That(result.DerivedHeals, Has.Count.EqualTo(1));
            Assert.That(result.DerivedHeals[0].RelicId, Is.EqualTo("C-16"));
            Assert.That(result.DerivedHeals[0].Kind, Is.EqualTo(RelicDerivedHealKind.Lifesteal));
            Assert.That(result.DerivedHeals[0].Percent, Is.EqualTo(12));
            Assert.That(result.DerivedHeals[0].TurnCap, Is.EqualTo(6));
            Assert.That(result.ActivationSummary, Does.Contain("레몬 흡혈액"));
        }

        [Test]
        public void LifestealRelic_DoesNotTrigger_WhenSymbolAbsent()
        {
            var owned = new[] { RelicCatalog.GetById("C-16") };

            RelicResolveResult result = _runner.Resolve(Matches(SlotSymbolType.Cherry, 3), owned, FullHp());

            Assert.That(result.DerivedHeals, Is.Empty);
        }

        [Test]
        public void BlockToHealRelic_EmitsDerivedHealRule_OnAnyPattern()
        {
            var owned = new[] { RelicCatalog.GetById("L-04") }; // 방어도 획득 시 25% 회복

            RelicResolveResult result = _runner.Resolve(Matches(SlotSymbolType.Diamond, 3), owned, FullHp());

            Assert.That(result.DerivedHeals, Has.Count.EqualTo(1));
            Assert.That(result.DerivedHeals[0].Kind, Is.EqualTo(RelicDerivedHealKind.BlockToHeal));
            Assert.That(result.DerivedHeals[0].Percent, Is.EqualTo(25));
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

        [TestCase("C-07", SlotSymbolType.Cherry, StatusEffectKind.Burn, 2)]
        [TestCase("C-08", SlotSymbolType.Clover, StatusEffectKind.Infection, 2)]
        [TestCase("C-09", SlotSymbolType.Lemon, StatusEffectKind.Vulnerable, 1)]
        [TestCase("C-21", SlotSymbolType.Lemon, StatusEffectKind.Weaken, 1)]
        public void EnemyStatusRelic_EmitsSelectedEnemyRequest(
            string relicId,
            SlotSymbolType symbol,
            StatusEffectKind expectedKind,
            int expectedAmount)
        {
            RelicDefinition relic = RelicCatalog.GetById(relicId);

            RelicResolveResult result = _runner.Resolve(
                Matches(symbol, relic.RequiredCount),
                new[] { relic },
                FullHp());

            Assert.That(result.StatusEffectsToApply, Has.Count.EqualTo(1));
            StatusEffectRequest request = result.StatusEffectsToApply[0];
            Assert.That(request.Kind, Is.EqualTo(expectedKind));
            Assert.That(request.Amount, Is.EqualTo(expectedAmount));
            Assert.That(request.TargetMode, Is.EqualTo(CombatTargetMode.SelectedEnemy));
        }

        [Test]
        public void ThornsRelic_EmitsSelfRequest()
        {
            RelicDefinition relic = RelicCatalog.GetById("C-02");

            RelicResolveResult result = _runner.Resolve(
                Matches(SlotSymbolType.Clover, 3),
                new[] { relic },
                FullHp());

            Assert.That(result.StatusEffectsToApply, Has.Count.EqualTo(1));
            StatusEffectRequest request = result.StatusEffectsToApply[0];
            Assert.That(request.Kind, Is.EqualTo(StatusEffectKind.Thorns));
            Assert.That(request.Amount, Is.EqualTo(2));
            Assert.That(request.TargetMode, Is.EqualTo(CombatTargetMode.Self));
        }

        [Test]
        public void StatusRelic_DoesNotEmitRequest_WhenTriggerDoesNotMatch()
        {
            RelicDefinition relic = RelicCatalog.GetById("C-07");

            RelicResolveResult result = _runner.Resolve(
                Matches(SlotSymbolType.Clover, 4),
                new[] { relic },
                FullHp());

            Assert.That(result.StatusEffectsToApply, Is.Empty);
        }

        [Test]
        public void MultipleStatusRelics_EmitEveryRequest()
        {
            var matches = new[]
            {
                Single(SlotSymbolType.Cherry, 4),
                Single(SlotSymbolType.Clover, 4),
            };
            var owned = new[]
            {
                RelicCatalog.GetById("C-07"),
                RelicCatalog.GetById("C-08"),
            };

            RelicResolveResult result = _runner.Resolve(matches, owned, FullHp());

            Assert.That(result.StatusEffectsToApply, Has.Count.EqualTo(2));
            Assert.That(result.StatusEffectsToApply[0].Kind, Is.EqualTo(StatusEffectKind.Burn));
            Assert.That(result.StatusEffectsToApply[1].Kind, Is.EqualTo(StatusEffectKind.Infection));
        }

        [Test]
        public void StatusRelics_FlowToCombatEffectsWithRequestedTargets()
        {
            var matches = new[]
            {
                Single(SlotSymbolType.Cherry, 4),
                Single(SlotSymbolType.Clover, 3),
            };
            var owned = new[]
            {
                RelicCatalog.GetById("C-07"),
                RelicCatalog.GetById("C-02"),
            };
            RelicResolveResult relicResult = _runner.Resolve(matches, owned, FullHp());
            var builder = new CombatTurnRequestBuilder();
            RunCombatRequestResult requestResult = builder.Build(
                new SlotCombatRequest(5, 0, 1, 0, false, "Attack"),
                relicResult,
                runDamageBonus: 0,
                runDefenseBonus: 0);
            var converter = new SlotCombatRequestToCombatEffectsConverter();
            var selectedTargetId = new CombatParticipantId(101);

            CombatEffect[] effects = converter.Convert(
                requestResult.FinalRequest,
                selectedTargetId,
                requestResult.StatusEffectsToApply);

            Assert.That(effects, Has.Length.EqualTo(3));
            Assert.That(effects[1].StatusEffect.Kind, Is.EqualTo(StatusEffectKind.Burn));
            Assert.That(
                effects[1].Target,
                Is.EqualTo(CombatEffectTarget.SelectedEnemy(selectedTargetId)));
            Assert.That(effects[2].StatusEffect.Kind, Is.EqualTo(StatusEffectKind.Thorns));
            Assert.That(effects[2].Target, Is.EqualTo(CombatEffectTarget.Self));
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
        public void SymbolRelic_TriggersOncePerMatchingLine_WhenSameSymbolMatchesMultipleLines()
        {
            var owned = new[] { RelicCatalog.GetById("S-03") }; // 종 치료제: 종 3개 이상 → 회복 +2
            var matches = new[]
            {
                Single(SlotSymbolType.Bell, 3), // 가로
                Single(SlotSymbolType.Bell, 3), // 세로
                Single(SlotSymbolType.Bell, 3), // 대각선
            };

            RelicResolveResult result = _runner.Resolve(matches, owned, FullHp());

            Assert.That(result.HealAmount, Is.EqualTo(6));
            Assert.That(result.Contributions, Has.Count.EqualTo(3));
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
