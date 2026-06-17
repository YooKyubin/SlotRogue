using NUnit.Framework;
using SlotRogue.Slot.Data;
using SlotRogue.UI.SlotPresentation;

namespace SlotRogue.UI.Tests.SlotPresentation
{
    public sealed class SlotPresentationQueueTests
    {
        [Test]
        public void Queue_PatternsRelicsAndFinalResult_InPlaybackOrder()
        {
            var firstPattern = new SlotPatternPresentationResult(
                "Cherry Line x3",
                SlotSymbolType.Cherry,
                0,
                0,
                3,
                new[] { 0, 1, 2 },
                "Cherry match",
                "+18 damage");
            var secondPattern = new SlotPatternPresentationResult(
                "Clover Line x4",
                SlotSymbolType.Clover,
                1,
                0,
                4,
                new[] { 5, 6, 7, 8 },
                "Clover match",
                "+40 damage");
            var firstRelic = new SlotRelicTriggerPresentationResult(
                "cherry",
                "체리",
                null,
                "Cherry bonus",
                "+5 damage",
                triggerPatternIndex: 0);
            var secondRelic = new SlotRelicTriggerPresentationResult(
                "RunBonus",
                "Run Bonus",
                null,
                "Reward bonus",
                "+2 damage",
                triggerPatternIndex: 1);
            var finalResult = new SlotFinalPresentationResult(
                47,
                0,
                1,
                0,
                "DMG 47 / DEF 0");

            var result = new SlotPresentationResult(
                null,
                new[] { firstPattern, secondPattern },
                new[] { firstRelic, secondRelic },
                finalResult);

            var queue = new SlotPresentationQueue(result);

            Assert.That(queue.Steps, Has.Count.EqualTo(5));
            Assert.That(queue.Steps[0].Kind, Is.EqualTo(SlotPresentationStepKind.Pattern));
            Assert.That(queue.Steps[0].Pattern, Is.SameAs(firstPattern));
            Assert.That(queue.Steps[1].Kind, Is.EqualTo(SlotPresentationStepKind.Relic));
            Assert.That(queue.Steps[1].Relic, Is.SameAs(firstRelic));
            Assert.That(queue.Steps[2].Kind, Is.EqualTo(SlotPresentationStepKind.Pattern));
            Assert.That(queue.Steps[2].Pattern, Is.SameAs(secondPattern));
            Assert.That(queue.Steps[3].Kind, Is.EqualTo(SlotPresentationStepKind.Relic));
            Assert.That(queue.Steps[3].Relic, Is.SameAs(secondRelic));
            Assert.That(queue.Steps[4].Kind, Is.EqualTo(SlotPresentationStepKind.Final));
            Assert.That(queue.Steps[4].FinalResult, Is.SameAs(finalResult));
        }

        [Test]
        public void Queue_UnassignedRelic_PlaysAfterFinalePattern()
        {
            var finalePattern = new SlotPatternPresentationResult(
                "Perfect Spin x15",
                SlotSymbolType.Cherry,
                0,
                0,
                15,
                new[] { 0, 1, 2 },
                "Full match",
                "+75 damage",
                true,
                7);
            var normalPattern = new SlotPatternPresentationResult(
                "Small Match x3",
                SlotSymbolType.Cherry,
                0,
                0,
                3,
                new[] { 0, 1, 2 },
                "Small match",
                "+18 damage",
                false,
                0);
            var relic = new SlotRelicTriggerPresentationResult(
                "cherry",
                "체리",
                null,
                "Cherry bonus",
                "+5 damage");

            var result = new SlotPresentationResult(
                null,
                new[] { finalePattern, normalPattern },
                new[] { relic },
                null);

            var queue = new SlotPresentationQueue(result);

            Assert.That(queue.Steps, Has.Count.EqualTo(3));
            Assert.That(queue.Steps[0].Pattern, Is.SameAs(normalPattern));
            Assert.That(queue.Steps[1].Pattern, Is.SameAs(finalePattern));
            Assert.That(queue.Steps[2].Relic, Is.SameAs(relic));
        }
    }
}
