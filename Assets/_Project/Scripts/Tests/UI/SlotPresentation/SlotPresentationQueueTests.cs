using NUnit.Framework;
using SlotRogue.Slot.Data;
using SlotRogue.UI.SlotPresentation;

namespace SlotRogue.UI.Tests.SlotPresentation
{
    public sealed class SlotPresentationQueueTests
    {
        [Test]
        public void Queue_PatternsOnly_InPlaybackOrder()
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
            var result = new SlotPresentationResult(
                null,
                new[] { firstPattern, secondPattern },
                new[]
                {
                    new SlotRelicTriggerPresentationResult(
                        "cherry",
                        "Cherry",
                        null,
                        "Cherry bonus",
                        "+5 damage",
                        triggerPatternIndex: 0),
                },
                new SlotFinalPresentationResult(
                    47,
                    0,
                    1,
                    0,
                    "DMG 47 / DEF 0"));

            var queue = new SlotPresentationQueue(result);

            Assert.That(queue.Steps, Has.Count.EqualTo(2));
            Assert.That(queue.Steps[0].Kind, Is.EqualTo(SlotPresentationStepKind.Pattern));
            Assert.That(queue.Steps[0].Pattern, Is.SameAs(firstPattern));
            Assert.That(queue.Steps[1].Kind, Is.EqualTo(SlotPresentationStepKind.Pattern));
            Assert.That(queue.Steps[1].Pattern, Is.SameAs(secondPattern));
        }

        [Test]
        public void Queue_FinalePattern_PlaysAfterNormalPatterns()
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
            var result = new SlotPresentationResult(
                null,
                new[] { finalePattern, normalPattern },
                null,
                null);

            var queue = new SlotPresentationQueue(result);

            Assert.That(queue.Steps, Has.Count.EqualTo(2));
            Assert.That(queue.Steps[0].Pattern, Is.SameAs(normalPattern));
            Assert.That(queue.Steps[1].Pattern, Is.SameAs(finalePattern));
        }
    }
}
