using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class SlotSymbolContributionAccumulatorTests
    {
        [Test]
        public void RecordTurn_DistributesBaseAttackPowerByPatternValue()
        {
            var accumulator = new SlotSymbolContributionAccumulator();
            var matches = new[]
            {
                MakeMatch(SlotSymbolType.Cherry, multiplier: 1f, baseValue: 3),
                MakeMatch(SlotSymbolType.Seven, multiplier: 2f, baseValue: 4),
            };
            var request = new SlotCombatRequest(
                damage: 11,
                defense: 0,
                attackCount: 2,
                healAmount: 0,
                isCritical: false,
                patternName: "Mixed");

            accumulator.RecordTurn(
                matches,
                request,
                relicDeltas: null,
                attackCount: request.AttackCount);

            IReadOnlyList<SlotSymbolContributionSnapshot> snapshots =
                accumulator.SnapshotForSymbols(new[]
                {
                    SlotSymbolType.Cherry,
                    SlotSymbolType.Seven,
                    SlotSymbolType.Lemon,
                });

            Assert.That(snapshots[0].Symbol, Is.EqualTo(SlotSymbolType.Cherry));
            Assert.That(snapshots[0].PatternCount, Is.EqualTo(1));
            Assert.That(snapshots[0].BaseAttackPower, Is.EqualTo(6));
            Assert.That(snapshots[0].RelicAttackPower, Is.EqualTo(0));
            Assert.That(snapshots[0].TotalAttackPower, Is.EqualTo(6));
            Assert.That(snapshots[1].Symbol, Is.EqualTo(SlotSymbolType.Seven));
            Assert.That(snapshots[1].PatternCount, Is.EqualTo(1));
            Assert.That(snapshots[1].BaseAttackPower, Is.EqualTo(16));
            Assert.That(snapshots[1].RelicAttackPower, Is.EqualTo(0));
            Assert.That(snapshots[1].TotalAttackPower, Is.EqualTo(16));
            Assert.That(snapshots[2].Symbol, Is.EqualTo(SlotSymbolType.Lemon));
            Assert.That(snapshots[2].PatternCount, Is.EqualTo(0));
            Assert.That(snapshots[2].BaseAttackPower, Is.EqualTo(0));
            Assert.That(snapshots[2].RelicAttackPower, Is.EqualTo(0));
            Assert.That(snapshots[2].TotalAttackPower, Is.EqualTo(0));
        }

        [Test]
        public void RecordTurn_AddsRelicAttackPowerToTriggerSymbol()
        {
            var accumulator = new SlotSymbolContributionAccumulator();
            var matches = new[]
            {
                MakeMatch(SlotSymbolType.Cherry, multiplier: 1f, baseValue: 3),
                MakeMatch(SlotSymbolType.Lemon, multiplier: 1f, baseValue: 3),
            };
            var request = new SlotCombatRequest(
                damage: 12,
                defense: 0,
                attackCount: 2,
                healAmount: 0,
                isCritical: false,
                patternName: "Mixed");
            var relicDeltas = new[]
            {
                new RelicContributionDelta(
                    "R-01",
                    "Cherry Bonus",
                    damagePerHit: 3,
                    block: 0,
                    heal: 0,
                    triggerPatternIndex: 0),
            };

            accumulator.RecordTurn(
                matches,
                request,
                relicDeltas,
                attackCount: request.AttackCount);

            IReadOnlyList<SlotSymbolContributionSnapshot> snapshots =
                accumulator.SnapshotForSymbols(new[]
                {
                    SlotSymbolType.Cherry,
                    SlotSymbolType.Lemon,
                });

            Assert.That(snapshots[0].BaseAttackPower, Is.EqualTo(12));
            Assert.That(snapshots[0].RelicAttackPower, Is.EqualTo(6));
            Assert.That(snapshots[0].TotalAttackPower, Is.EqualTo(18));
            Assert.That(snapshots[1].BaseAttackPower, Is.EqualTo(12));
            Assert.That(snapshots[1].RelicAttackPower, Is.EqualTo(0));
            Assert.That(snapshots[1].TotalAttackPower, Is.EqualTo(12));
        }

        [Test]
        public void Add_MergesBattleSnapshots()
        {
            var accumulator = new SlotSymbolContributionAccumulator();

            accumulator.Add(new[]
            {
                new SlotSymbolContributionSnapshot(SlotSymbolType.Bell, 2, 18, 5),
                new SlotSymbolContributionSnapshot(SlotSymbolType.Bell, 1, 10, 7),
            });

            IReadOnlyList<SlotSymbolContributionSnapshot> snapshots =
                accumulator.SnapshotForSymbols(new[] { SlotSymbolType.Bell });

            Assert.That(snapshots[0].PatternCount, Is.EqualTo(3));
            Assert.That(snapshots[0].BaseAttackPower, Is.EqualTo(28));
            Assert.That(snapshots[0].RelicAttackPower, Is.EqualTo(12));
            Assert.That(snapshots[0].TotalAttackPower, Is.EqualTo(40));
        }

        private static SlotPatternMatch MakeMatch(
            SlotSymbolType symbol,
            float multiplier,
            int baseValue)
        {
            var cells = new List<SlotCell>(baseValue);
            for (int index = 0; index < baseValue; index++)
            {
                cells.Add(new SlotCell(index, 0));
            }

            var definition = new SlotPatternDefinition(
                "test",
                "Test",
                0,
                multiplier,
                SlotPatternRank.HorizontalSm,
                false,
                cells);
            return new SlotPatternMatch(definition, symbol, cells, baseValue, 0);
        }
    }
}
