using System;
using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;

// 이 테스트는 의도적으로 레거시 StarterArtifactCatalog 경로(보존된 dead code)를 검증한다.
#pragma warning disable CS0618

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class RunCombatRequestResolverTests
    {
        private RunCombatRequestResolver _resolver = null!;

        [SetUp]
        public void SetUp()
        {
            _resolver = new RunCombatRequestResolver();
        }

        [Test]
        public void Resolve_BlankRequest_AddsBaseAttack()
        {
            RunCombatRequestResult result = _resolver.Resolve(
                NoMatches,
                SlotCombatRequest.Empty,
                StarterArtifactCatalog.Get(StarterArtifactId.None),
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.FinalRequest.Damage, Is.EqualTo(SlotCombatRequest.BaseAttackDamage));
            Assert.That(result.FinalRequest.AttackCount, Is.EqualTo(SlotCombatRequest.BaseAttackCount));
            Assert.That(result.FinalRequest.PatternName, Is.EqualTo(SlotCombatRequest.BaseAttackName));
        }

        [Test]
        public void Resolve_MatchingStarterArtifact_AppliesArtifactBonus()
        {
            var request = new SlotCombatRequest(18, 0, 1, 0, false, "Cherry x3");

            RunCombatRequestResult result = _resolver.Resolve(
                Matches(SlotSymbolType.Cherry, 3),
                request,
                StarterArtifactCatalog.Get(StarterArtifactId.Cherry),
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.StarterArtifactActivation.Activated, Is.True);
            Assert.That(result.FinalRequest.Damage, Is.EqualTo(23));
        }

        [Test]
        public void Resolve_NonMatchingStarterArtifact_DoesNotApplyArtifactBonus()
        {
            var request = new SlotCombatRequest(15, 0, 1, 0, false, "Clover x3");

            RunCombatRequestResult result = _resolver.Resolve(
                Matches(SlotSymbolType.Clover, 3),
                request,
                StarterArtifactCatalog.Get(StarterArtifactId.Cherry),
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.StarterArtifactActivation.Activated, Is.False);
            Assert.That(result.FinalRequest.Damage, Is.EqualTo(15));
        }

        [Test]
        public void Resolve_ArtifactSymbolInAnyPattern_Activates()
        {
            // 대표 패턴이 아니더라도 전체 목록 중 하나가 조건을 만족하면 발동해야 한다.
            var request = new SlotCombatRequest(15, 0, 1, 0, false, "Mixed");
            var matches = new List<SlotPatternMatch>
            {
                Single(SlotSymbolType.Clover, 5), // 대표(가장 큰) 패턴
                Single(SlotSymbolType.Cherry, 3), // 유물 조건을 만족하는 다른 패턴
            };

            RunCombatRequestResult result = _resolver.Resolve(
                matches,
                request,
                StarterArtifactCatalog.Get(StarterArtifactId.Cherry),
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.StarterArtifactActivation.Activated, Is.True);
            Assert.That(result.FinalRequest.Damage, Is.EqualTo(20));
        }

        [Test]
        public void Resolve_RunBonuses_AdjustFinalRequest()
        {
            var request = new SlotCombatRequest(6, 0, 1, 12, false, "Diamond x3");

            RunCombatRequestResult result = _resolver.Resolve(
                Matches(SlotSymbolType.Diamond, 3),
                request,
                StarterArtifactCatalog.Get(StarterArtifactId.Grape),
                runDamageBonus: 2,
                runDefenseBonus: 2);

            Assert.That(result.FinalRequest.Damage, Is.EqualTo(8));
            Assert.That(result.FinalRequest.Defense, Is.EqualTo(2));
            Assert.That(result.FinalRequest.HealAmount, Is.EqualTo(16));
            Assert.That(result.RunBonusSummary, Is.EqualTo("피해 +2, 방어 +2"));
        }

        [Test]
        public void Resolve_MatchingAttributeArtifact_ReturnsStatusEffectToApply()
        {
            var request = new SlotCombatRequest(6, 0, 1, 0, false, "Lemon x3");

            RunCombatRequestResult result = _resolver.Resolve(
                Matches(SlotSymbolType.Lemon, 3),
                request,
                StarterArtifactCatalog.Get(StarterArtifactId.Lemon),
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.StarterArtifactActivation.Activated, Is.True);
            Assert.That(result.StatusEffectToApply.Kind, Is.EqualTo(StatusEffectKind.Burn));
            Assert.That(result.StatusEffectToApply.Duration, Is.EqualTo(3));
            Assert.That(result.StatusEffectToApply.Magnitude, Is.EqualTo(2));
            Assert.That(result.StatusEffectToApply.StackMode, Is.EqualTo(StatusStackMode.Refresh));
        }

        [Test]
        public void Resolve_MultiHitRequest_CalculatesTotalAttackPower()
        {
            var request = new SlotCombatRequest(6, 0, 3, 0, false, "Multi Hit");

            RunCombatRequestResult result = _resolver.Resolve(
                NoMatches,
                request,
                StarterArtifactCatalog.Get(StarterArtifactId.None),
                runDamageBonus: 2,
                runDefenseBonus: 0);

            Assert.That(result.FinalRequest.Damage, Is.EqualTo(8));
            Assert.That(result.FinalRequest.AttackCount, Is.EqualTo(3));
            Assert.That(result.AttackPower, Is.EqualTo(24));
        }

        private static readonly IReadOnlyList<SlotPatternMatch> NoMatches = Array.Empty<SlotPatternMatch>();

        private static IReadOnlyList<SlotPatternMatch> Matches(SlotSymbolType symbol, int cellCount)
        {
            return new[] { Single(symbol, cellCount) };
        }

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
