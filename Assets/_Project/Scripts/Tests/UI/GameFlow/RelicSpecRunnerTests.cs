using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.Tests.Relics
{
    /// <summary>
    /// v29 유물 실행 엔진(<see cref="RelicSpecRunner"/>)의 OnDamageResolve 슬라이스 검증.
    /// 실제 카탈로그(<see cref="RelicSpecCatalog"/>) 데이터로 조건·다중 발동·배율 누적을 확인한다.
    /// </summary>
    public sealed class RelicSpecRunnerTests
    {
        [Test]
        public void NoRelics_ReturnsEmpty()
        {
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(null, Ctx(), null);

            Assert.That(result.FlatDamage, Is.EqualTo(0));
            Assert.That(result.Heal, Is.EqualTo(0));
            Assert.That(result.SpecialMult, Is.EqualTo(1f));
            Assert.That(result.Contributions, Is.Empty);
        }

        [Test]
        public void NonDamageTrigger_IsIgnored()
        {
            // R-21 별가루 주머니 = OnBattleStart. 피해 정산 슬라이스에선 무시돼야 한다.
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-21"), Ctx(coins: 3), Pats(Pat()));

            Assert.That(result.FlatDamage, Is.EqualTo(0));
            Assert.That(result.Contributions, Is.Empty);
        }

        [Test]
        public void FlatDamage_SwapRelic_AppliesOnlyWhenSwapped()
        {
            // R-16 교환 장갑 = swap 쓴 스핀 합산 피해 +4.
            RelicSpecResolveResult swapped = RelicSpecRunner.ResolveDamageTurn(
                Own("R-16"), Ctx(swappedThisSpin: true), Pats(Pat()));
            RelicSpecResolveResult noSwap = RelicSpecRunner.ResolveDamageTurn(
                Own("R-16"), Ctx(swappedThisSpin: false), Pats(Pat()));

            Assert.That(swapped.FlatDamage, Is.EqualTo(4));
            Assert.That(noSwap.FlatDamage, Is.EqualTo(0));
        }

        [Test]
        public void FlatDamage_SavingsRelic_AppliesAtCoinThreshold()
        {
            // R-23 저축가의 반지 = 보유 별조각 5+면 합산 피해 +2.
            RelicSpecResolveResult atThreshold = RelicSpecRunner.ResolveDamageTurn(
                Own("R-23"), Ctx(coins: 5), Pats(Pat()));
            RelicSpecResolveResult below = RelicSpecRunner.ResolveDamageTurn(
                Own("R-23"), Ctx(coins: 4), Pats(Pat()));

            Assert.That(atThreshold.FlatDamage, Is.EqualTo(2));
            Assert.That(below.FlatDamage, Is.EqualTo(0));
        }

        [Test]
        public void FlatDamage_Unconditional_AlwaysApplies()
        {
            // R-39 폭죽 다발 = 조건 없이 모든 족보 피해 +8.
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-39"), Ctx(), Pats(Pat(SlotSymbolType.Lemon, 3)));

            Assert.That(result.FlatDamage, Is.EqualTo(8));
        }

        [Test]
        public void ComboMultAdd_AccumulatesPerMatchingPattern()
        {
            // R-09 체리 증폭기 = 체리 3개↑ 족보마다 Mult +0.3. 두 줄이면 +0.6.
            RelicSpecResolveResult twoLines = RelicSpecRunner.ResolveDamageTurn(
                Own("R-09"), Ctx(),
                Pats(Pat(SlotSymbolType.Cherry, 3), Pat(SlotSymbolType.Cherry, 4)));
            RelicSpecResolveResult noCherry = RelicSpecRunner.ResolveDamageTurn(
                Own("R-09"), Ctx(), Pats(Pat(SlotSymbolType.Lemon, 3)));

            Assert.That(twoLines.ComboMultAdd, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(noCherry.ComboMultAdd, Is.EqualTo(0f));
        }

        [Test]
        public void ComboMultAdd_RespectsMinPatternSize()
        {
            // R-09는 체리 3개↑만. 2개짜리 체리 족보는 발동하지 않는다.
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-09"), Ctx(), Pats(Pat(SlotSymbolType.Cherry, 2)));

            Assert.That(result.ComboMultAdd, Is.EqualTo(0f));
        }

        [Test]
        public void SpecialMult_SevenPattern_Multiplies()
        {
            // R-12 세븐 과충전 = 7 포함 족보 특수배율 ×1.5.
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-12"), Ctx(), Pats(Pat(SlotSymbolType.Seven, 3)));

            Assert.That(result.SpecialMult, Is.EqualTo(1.5f).Within(0.0001f));
        }

        [Test]
        public void FinalMult_WholeLine_Multiplies()
        {
            // R-13 잭팟 코어 = 한 줄 5칸 동일 → ×3.
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-13"), Ctx(),
                Pats(Pat(SlotSymbolType.Seven, 5, wholeLine: true)));

            Assert.That(result.FinalMult, Is.EqualTo(3f).Within(0.0001f));
        }

        [Test]
        public void GlassCannon_AppliesSpecialAndIncomingMult()
        {
            // R-38 유리 대포 = 모든 피해 특수배율 ×1.4 / 받는 피해 ×1.3 (조건 없음).
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-38"), Ctx(), Pats(Pat()));

            Assert.That(result.SpecialMult, Is.EqualTo(1.4f).Within(0.0001f));
            Assert.That(result.IncomingDamageMul, Is.EqualTo(1.3f).Within(0.0001f));
        }

        [Test]
        public void Contribution_RecordedForFlatDamage()
        {
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-16"), Ctx(swappedThisSpin: true), Pats(Pat()));

            Assert.That(result.Contributions, Has.Count.EqualTo(1));
            Assert.That(result.Contributions[0].RelicId, Is.EqualTo("R-16"));
            Assert.That(result.Contributions[0].FlatDamage, Is.EqualTo(4));
        }

        [Test]
        public void MultipleRelics_Accumulate()
        {
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-16", "R-39"), Ctx(swappedThisSpin: true), Pats(Pat()));

            Assert.That(result.FlatDamage, Is.EqualTo(12)); // 4 + 8
            Assert.That(result.Contributions, Has.Count.EqualTo(2));
        }

        [Test]
        public void EventCoins_BattleStart_GrantsFromR21()
        {
            // R-21 별가루 주머니 = 전투 시작 별조각 +1.
            int coins = RelicSpecRunner.ResolveEventCoins(
                Own("R-21"), RelicTrigger.OnBattleStart, Ctx());

            Assert.That(coins, Is.EqualTo(1));
        }

        [Test]
        public void EventCoins_Kill_RespectsNoSwapCondition()
        {
            // R-22 별 수집가 = 스왑 없이 승리 시 별조각 +1.
            int noSwap = RelicSpecRunner.ResolveEventCoins(
                Own("R-22"), RelicTrigger.OnKill, Ctx(swappedThisBattle: false));
            int swapped = RelicSpecRunner.ResolveEventCoins(
                Own("R-22"), RelicTrigger.OnKill, Ctx(swappedThisBattle: true));

            Assert.That(noSwap, Is.EqualTo(1));
            Assert.That(swapped, Is.EqualTo(0));
        }

        [Test]
        public void EventCoins_WrongTrigger_ReturnsZero()
        {
            int coins = RelicSpecRunner.ResolveEventCoins(
                Own("R-21"), RelicTrigger.OnKill, Ctx());

            Assert.That(coins, Is.EqualTo(0));
        }

        [Test]
        public void EventHeal_Kill_GrantsFromR33()
        {
            // R-33 응급 붕대 = 처치 시 HP +2.
            int heal = RelicSpecRunner.ResolveEventHeal(Own("R-33"), RelicTrigger.OnKill, Ctx());

            Assert.That(heal, Is.EqualTo(2));
        }

        [Test]
        public void EventHeal_WrongTrigger_ReturnsZero()
        {
            int heal = RelicSpecRunner.ResolveEventHeal(
                Own("R-33"), RelicTrigger.OnBattleStart, Ctx());

            Assert.That(heal, Is.EqualTo(0));
        }

        [Test]
        public void RuleModifier_SwapCount_SumsAcrossRelics()
        {
            // R-17 스왑 장인, R-19 시간의 여유 = 각 SwapCountDelta +1.
            int one = RelicSpecRunner.ResolveRuleModifier(Own("R-17"), RelicEffectKind.SwapCountDelta);
            int two = RelicSpecRunner.ResolveRuleModifier(
                Own("R-17", "R-19"), RelicEffectKind.SwapCountDelta);

            Assert.That(one, Is.EqualTo(1));
            Assert.That(two, Is.EqualTo(2));
        }

        [Test]
        public void RuleModifier_WrongKind_ReturnsZero()
        {
            // R-30 VIP 상점권 = ShopOfferDelta +2 (스왑 아님).
            int swap = RelicSpecRunner.ResolveRuleModifier(Own("R-30"), RelicEffectKind.SwapCountDelta);
            int offers = RelicSpecRunner.ResolveRuleModifier(Own("R-30"), RelicEffectKind.ShopOfferDelta);

            Assert.That(swap, Is.EqualTo(0));
            Assert.That(offers, Is.EqualTo(2));
        }

        [Test]
        public void RuleModifier_ShopDiscount_SumsFromShopRelics()
        {
            // R-27 낡은 할인권, R-28 단골 카드 = 각 ShopDiscount +1.
            int one = RelicSpecRunner.ResolveRuleModifier(Own("R-28"), RelicEffectKind.ShopDiscount);
            int two = RelicSpecRunner.ResolveRuleModifier(
                Own("R-27", "R-28"), RelicEffectKind.ShopDiscount);

            Assert.That(one, Is.EqualTo(1));
            Assert.That(two, Is.EqualTo(2));
        }

        [Test]
        public void MultipliedBaseDamage_NoMultRelics_EqualsBaseSum()
        {
            // 배율 유물 없음(R-16은 가산): 곱해진 기본 피해 = 족보 기본 피해 합.
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-16"), Ctx(swappedThisSpin: true),
                Pats(Pat(SlotSymbolType.Cherry, 3, baseDamage: 10),
                     Pat(SlotSymbolType.Lemon, 3, baseDamage: 5)));

            Assert.That(result.MultipliedBaseDamage, Is.EqualTo(15));
        }

        [Test]
        public void MultipliedBaseDamage_SevenSpecialMult_OnlyMultipliesMatchingPattern()
        {
            // R-12 세븐 과충전 = 7 포함 족보 ×1.5. 7족보(10)만 ×1.5, 체리족보(10)는 그대로 → 25.
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-12"), Ctx(),
                Pats(Pat(SlotSymbolType.Seven, 3, baseDamage: 10),
                     Pat(SlotSymbolType.Cherry, 3, baseDamage: 10)));

            Assert.That(result.MultipliedBaseDamage, Is.EqualTo(25));
        }

        [Test]
        public void MultipliedBaseDamage_ComboMultAdd_AppliesPerPattern()
        {
            // R-09 체리 증폭기 = 체리 3개↑ Mult +0.3. 체리족보(10)만 ×1.3 → 13 + 10 = 23.
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-09"), Ctx(),
                Pats(Pat(SlotSymbolType.Cherry, 3, baseDamage: 10),
                     Pat(SlotSymbolType.Lemon, 3, baseDamage: 10)));

            Assert.That(result.MultipliedBaseDamage, Is.EqualTo(23));
        }

        [Test]
        public void MultipliedBaseDamage_FinalMult_AppliesToTotal()
        {
            // R-13 잭팟 코어 = 한 줄 5칸 동일 → 최종 ×3. 20 × 3 = 60.
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-13"), Ctx(),
                Pats(Pat(SlotSymbolType.Seven, 5, wholeLine: true, baseDamage: 20)));

            Assert.That(result.MultipliedBaseDamage, Is.EqualTo(60));
        }

        [Test]
        public void MultipliedBaseDamage_GlassCannon_MultipliesAllPatterns()
        {
            // R-38 유리 대포 = 조건 없이 모든 피해 ×1.4. (10 + 10) 각 ×1.4 = 28.
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-38"), Ctx(),
                Pats(Pat(SlotSymbolType.Cherry, 3, baseDamage: 10),
                     Pat(SlotSymbolType.Lemon, 3, baseDamage: 10)));

            Assert.That(result.MultipliedBaseDamage, Is.EqualTo(28));
        }

        [Test]
        public void Retrigger_Highest_OnFirstSpin_AddsHighestAgain()
        {
            // R-01 앙코르 = 전투마다 한 번(첫 스핀), 최고 족보 재발동. 10+20, 최고 20 재발동 → 50.
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-01"), Ctx(firstSpin: true),
                Pats(Pat(SlotSymbolType.Cherry, 3, baseDamage: 10),
                     Pat(SlotSymbolType.Seven, 3, baseDamage: 20)));

            Assert.That(result.MultipliedBaseDamage, Is.EqualTo(50));
        }

        [Test]
        public void Retrigger_OncePerBattle_NotOnLaterSpins()
        {
            // R-01은 첫 스핀에만. 이후 스핀엔 재발동 없음 → 그대로 30.
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-01"), Ctx(firstSpin: false, turn: 5),
                Pats(Pat(SlotSymbolType.Cherry, 3, baseDamage: 10),
                     Pat(SlotSymbolType.Seven, 3, baseDamage: 20)));

            Assert.That(result.MultipliedBaseDamage, Is.EqualTo(30));
        }

        [Test]
        public void Retrigger_All_NoSwapFirstSpin_DoublesTotal()
        {
            // R-05 메아리 부적 = no-swap 첫 스핀, 전 족보 재발동. (10+20)×2 = 60.
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-05"), Ctx(firstSpin: true, swappedThisSpin: false),
                Pats(Pat(SlotSymbolType.Cherry, 3, baseDamage: 10),
                     Pat(SlotSymbolType.Seven, 3, baseDamage: 20)));

            Assert.That(result.MultipliedBaseDamage, Is.EqualTo(60));
        }

        [Test]
        public void Retrigger_All_BlockedBySwap()
        {
            // R-05는 스왑을 쓴 스핀이면 발동하지 않는다 → 그대로 30.
            RelicSpecResolveResult result = RelicSpecRunner.ResolveDamageTurn(
                Own("R-05"), Ctx(firstSpin: true, swappedThisSpin: true),
                Pats(Pat(SlotSymbolType.Cherry, 3, baseDamage: 10),
                     Pat(SlotSymbolType.Seven, 3, baseDamage: 20)));

            Assert.That(result.MultipliedBaseDamage, Is.EqualTo(30));
        }

        [Test]
        public void Retrigger_SwapMade_R18_RetriggersHighest()
        {
            // R-18 연쇄 교환 = 스왑으로 만든 족보가 있으면 최고 족보 재발동.
            RelicSpecResolveResult swapped = RelicSpecRunner.ResolveDamageTurn(
                Own("R-18"), Ctx(),
                Pats(Pat(SlotSymbolType.Cherry, 3, madeBySwap: true, baseDamage: 10),
                     Pat(SlotSymbolType.Seven, 3, madeBySwap: true, baseDamage: 20)));
            RelicSpecResolveResult noSwap = RelicSpecRunner.ResolveDamageTurn(
                Own("R-18"), Ctx(),
                Pats(Pat(SlotSymbolType.Cherry, 3, madeBySwap: false, baseDamage: 10),
                     Pat(SlotSymbolType.Seven, 3, madeBySwap: false, baseDamage: 20)));

            Assert.That(swapped.MultipliedBaseDamage, Is.EqualTo(50)); // 30 + 최고 20
            Assert.That(noSwap.MultipliedBaseDamage, Is.EqualTo(30));  // 조건 미충족
        }

        [Test]
        public void ComboCrown_R42_MultipliesWhenThreePatterns()
        {
            // R-42 콤보 왕관 = 족보 3개↑면 특수배율 ×1.5.
            RelicSpecResolveResult three = RelicSpecRunner.ResolveDamageTurn(
                Own("R-42"), Ctx(patternCount: 3),
                Pats(Pat(baseDamage: 10), Pat(baseDamage: 10), Pat(baseDamage: 10)));
            RelicSpecResolveResult two = RelicSpecRunner.ResolveDamageTurn(
                Own("R-42"), Ctx(patternCount: 2),
                Pats(Pat(baseDamage: 10), Pat(baseDamage: 10)));

            Assert.That(three.MultipliedBaseDamage, Is.EqualTo(45)); // (10+10+10)×1.5
            Assert.That(two.MultipliedBaseDamage, Is.EqualTo(20));   // 조건 미충족
        }

        [Test]
        public void FirstStrike_R49_MultipliesOnFirstSpin()
        {
            RelicSpecResolveResult first = RelicSpecRunner.ResolveDamageTurn(
                Own("R-49"), Ctx(firstSpin: true), Pats(Pat(baseDamage: 10)));
            RelicSpecResolveResult later = RelicSpecRunner.ResolveDamageTurn(
                Own("R-49"), Ctx(firstSpin: false), Pats(Pat(baseDamage: 10)));

            Assert.That(first.MultipliedBaseDamage, Is.EqualTo(15));
            Assert.That(later.MultipliedBaseDamage, Is.EqualTo(10));
        }

        [Test]
        public void ProposalSpec_P07_ThreeMatch_AddsFlatDamage()
        {
            // 제안 엔진 브릿지: P-07(3일치 족보 피해 +1)이 엔진에서 실제로 적용되는지.
            RelicSpec p07 = RelicSpecCatalog.GetProposalById("P-07");
            Assert.That(p07, Is.Not.Null);

            var owned = new List<RelicSpec> { p07 };
            RelicSpecResolveResult three = RelicSpecRunner.ResolveDamageTurn(
                owned, Ctx(), Pats(Pat(SlotSymbolType.Cherry, 3, baseDamage: 10)));
            RelicSpecResolveResult four = RelicSpecRunner.ResolveDamageTurn(
                owned, Ctx(), Pats(Pat(SlotSymbolType.Cherry, 4, baseDamage: 10)));

            Assert.That(three.FlatDamage, Is.EqualTo(1)); // 정확히 3칸 → +1
            Assert.That(four.FlatDamage, Is.EqualTo(0));  // 4칸이면 조건 미충족
        }

        [Test]
        public void ProposalSpec_P14_FruitFourMatch_RequestsBurn()
        {
            // 속성 제안: P-14(체리·레몬 4개↑ → 화상 2)이 엔진에서 상태이상 요청을 뿜는지.
            RelicSpec p14 = RelicSpecCatalog.GetProposalById("P-14");
            Assert.That(p14, Is.Not.Null);

            var owned = new List<RelicSpec> { p14 };
            RelicSpecResolveResult burn = RelicSpecRunner.ResolveDamageTurn(
                owned, Ctx(), Pats(Pat(SlotSymbolType.Cherry, 4, baseDamage: 10)));
            RelicSpecResolveResult noBurn = RelicSpecRunner.ResolveDamageTurn(
                owned, Ctx(), Pats(Pat(SlotSymbolType.Cherry, 3, baseDamage: 10)));

            Assert.That(burn.StatusRequests, Has.Count.EqualTo(1));
            Assert.That(burn.StatusRequests[0].Kind, Is.EqualTo(RelicEffectKind.ApplyBurn));
            Assert.That(burn.StatusRequests[0].Amount, Is.EqualTo(2));
            Assert.That(noBurn.StatusRequests, Is.Empty); // 4칸 미만이면 조건 미충족
        }

        // ── helpers ──────────────────────────────────────────────────────

        private static RelicRuntimeContext Ctx(
            bool swappedThisSpin = false,
            bool swappedThisBattle = false,
            int coins = 0,
            int turn = 1,
            bool firstSpin = false,
            int patternCount = 1)
            => new(swappedThisSpin, swappedThisBattle, coins, turn, firstSpin, patternCount);

        private static RelicPatternView Pat(
            SlotSymbolType symbol = SlotSymbolType.Cherry,
            int size = 3,
            bool madeBySwap = false,
            bool wholeLine = false,
            int baseDamage = 0)
            => new(symbol, size, madeBySwap, wholeLine, baseDamage);

        private static List<RelicPatternView> Pats(params RelicPatternView[] patterns) =>
            new(patterns);

        private static List<RelicSpec> Own(params string[] ids)
        {
            var owned = new List<RelicSpec>(ids.Length);
            foreach (string id in ids)
            {
                owned.Add(RelicSpecCatalog.GetById(id));
            }

            return owned;
        }
    }
}
