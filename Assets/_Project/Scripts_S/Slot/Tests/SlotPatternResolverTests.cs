using NUnit.Framework;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Tests
{
    public sealed class SlotPatternResolverTests
    {
        [Test]
        public void Resolve_WhenHorizontalRunExists_ReturnsBestPattern()
        {
            var spinResult = new SlotSpinResult(new[]
            {
                SlotSymbolType.Sword, SlotSymbolType.Sword, SlotSymbolType.Sword, SlotSymbolType.Coin, SlotSymbolType.Gem,
                SlotSymbolType.Gem, SlotSymbolType.Gem, SlotSymbolType.Gem, SlotSymbolType.Gem, SlotSymbolType.Coin,
                SlotSymbolType.Heart, SlotSymbolType.Coin, SlotSymbolType.Skull, SlotSymbolType.Shield, SlotSymbolType.Heart
            });
            var resolver = new SlotPatternResolver();

            SlotPatternResult result = resolver.Resolve(spinResult);

            Assert.That(result.HasMatch, Is.True);
            Assert.That(result.Symbol, Is.EqualTo(SlotSymbolType.Gem));
            Assert.That(result.Row, Is.EqualTo(1));
            Assert.That(result.StartColumn, Is.EqualTo(0));
            Assert.That(result.MatchLength, Is.EqualTo(4));
            Assert.That(result.PatternName, Is.EqualTo("Gem Line x4"));
        }

        [Test]
        public void Resolve_WhenNoRunExists_ReturnsNoMatch()
        {
            var spinResult = new SlotSpinResult(new[]
            {
                SlotSymbolType.Sword, SlotSymbolType.Shield, SlotSymbolType.Heart, SlotSymbolType.Coin, SlotSymbolType.Gem,
                SlotSymbolType.Gem, SlotSymbolType.Skull, SlotSymbolType.Sword, SlotSymbolType.Shield, SlotSymbolType.Heart,
                SlotSymbolType.Heart, SlotSymbolType.Coin, SlotSymbolType.Gem, SlotSymbolType.Skull, SlotSymbolType.Sword
            });
            var resolver = new SlotPatternResolver();

            SlotPatternResult result = resolver.Resolve(spinResult);

            Assert.That(result.HasMatch, Is.False);
            Assert.That(result.PatternName, Is.EqualTo("No Match"));
        }
    }
}
