using System.Collections.Generic;
using SlotRogue.Relics.Pool;
using SlotRogue.Data.GameFlow;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 제안(처치 보상) v29 기획표(slot_roguelite_proposals_spec_v29) 46종. 몬스터 처치 후 3택1로 제시하며 모두 영구다.
    /// 실행되는 효과는 <see cref="RunProposalEffectKind"/> 4종(심볼 가중치·심볼 기본피해·별조각·유물슬롯)뿐 —
    /// 그 외(족보·재발동·속성·스왑·5턴·저주 곱연산 등)는 아직 실행 엔진이 없어 <see cref="RunProposalEffectKind.None"/>(설명만)이며,
    /// 개발용 수치는 각 항목 끝 주석에 보존한다(P1/P2에서 배선). v29에 없는 항목은 전부 제거했다.
    /// </summary>
    public static class RunRewardCatalog
    {
        public const int MinSymbolWeightAfterDecrease = 1;
        public const int MinSymbolCountAfterRemove = MinSymbolWeightAfterDecrease;

        private static readonly RunRewardDefinition[] Proposals = BuildProposals();

        public static IReadOnlyList<RunRewardDefinition> ForTier(EncounterTier tier)
        {
            return Proposals;
        }

        private static RunRewardDefinition[] BuildProposals()
        {
            return new[]
            {
                // ── 일반(common) ────────────────────────────────────────
                P("P-01", "prob", RewardRarity.Common, "체리 풍년", "<sprite index=0>체리가 영구히 더 자주 나온다!", RunProposalEffectKind.SymbolWeight, 8, SlotSymbolType.Cherry),
                P("P-02", "prob", RewardRarity.Common, "레몬 풍년", "<sprite index=5>레몬이 영구히 더 자주 나온다!", RunProposalEffectKind.SymbolWeight, 8, SlotSymbolType.Lemon),
                P("P-03", "prob", RewardRarity.Common, "종의 울림", "<sprite index=3>종이 더 자주 나온다.", RunProposalEffectKind.SymbolWeight, 8, SlotSymbolType.Bell),
                P("P-04", "prob", RewardRarity.Common, "클로버의 기운", "<sprite index=4>클로버가 더 자주 나온다.", RunProposalEffectKind.SymbolWeight, 8, SlotSymbolType.Clover),
                P("P-05", "prob", RewardRarity.Common, "다이아 광택", "<sprite index=2>다이아가 더 자주 나온다.", RunProposalEffectKind.SymbolWeight, 8, SlotSymbolType.Diamond),
                P("P-06", "prob", RewardRarity.Common, "세븐의 징조", "귀한 <sprite index=1>세븐이 조금 더 자주 나온다.", RunProposalEffectKind.SymbolWeight, 8, SlotSymbolType.Seven),
                P("P-07", "pat", RewardRarity.Common, "3연격 훈련", "3개를 맞춘 족보가 더 세진다.", RunProposalEffectKind.EngineEffect), // 3일치 족보 피해 +1 (P2: 족보 피해 엔진)
                P("P-08", "upg", RewardRarity.Common, "체리 숙련", "<sprite index=0>체리로 맞추면 더 세게 때린다.", RunProposalEffectKind.SymbolBaseDamage, 1, SlotSymbolType.Cherry),
                P("P-09", "upg", RewardRarity.Common, "레몬 숙련", "<sprite index=5>레몬으로 맞추면 더 세게 때린다.", RunProposalEffectKind.SymbolBaseDamage, 1, SlotSymbolType.Lemon),
                P("P-10", "upg", RewardRarity.Common, "종 숙련", "<sprite index=3>종으로 맞추면 더 세게 때린다.", RunProposalEffectKind.SymbolBaseDamage, 1, SlotSymbolType.Bell),
                P("P-11", "upg", RewardRarity.Common, "클로버 숙련", "<sprite index=4>클로버로 맞추면 더 세게 때린다.", RunProposalEffectKind.SymbolBaseDamage, 1, SlotSymbolType.Clover),
                P("P-12", "combat", RewardRarity.Common, "첫 수의 감각", "매 전투 첫 스핀이 더 강하게 터진다.", RunProposalEffectKind.EngineEffect), // 첫 스핀 합산 피해 +2 (P2)
                P("P-13", "swap", RewardRarity.Common, "절제 보상", "자리 바꾸기를 아낀 스핀은 피해가 조금 오른다.", RunProposalEffectKind.EngineEffect), // no-swap 스핀 합산 피해 +1 (P2)
                P("P-14", "status", RewardRarity.Common, "붉은 성냥", "<sprite index=0>체리·<sprite index=5>레몬을 잔뜩 맞추면 적이 불에 탄다.", RunProposalEffectKind.EngineEffect), // 체리/레몬 4개↑ → 화상 2 (P2)
                P("P-15", "status", RewardRarity.Common, "푸른 감염가루", "<sprite index=4>클로버·<sprite index=2>다이아를 잔뜩 맞추면 적이 감염된다.", RunProposalEffectKind.EngineEffect), // 클로버/다이아 4개↑ → 감염 3 (P2)
                P("P-16", "status", RewardRarity.Common, "균열음", "<sprite index=3>종을 맞추면 적이 취약해져 다음 피해를 더 받는다.", RunProposalEffectKind.EngineEffect), // 종 3개↑ → 취약 1 (P2)

                // ── 고급(uncommon) ──────────────────────────────────────
                P("P-17", "prob", RewardRarity.Uncommon, "대세 강화", "지금 가장 잘 나오는 심볼이 더 자주 나온다. 대신 가장 안 나오는 심볼이 조금 줄어든다."), // 최고 가중 +3, 최저 -1 (자동 대상 P2)
                P("P-18", "prob", RewardRarity.Uncommon, "행운 상승", "행운이 올라 귀한 심볼들이 더 잘 나온다."), // 행운 +2 (종/클로버/다이아 +1, 7 +2) (P2 luck)
                P("P-19", "upg", RewardRarity.Uncommon, "다이아 세공", "<sprite index=2>다이아가 세지고, 크게 맞출수록 더 세진다.", RunProposalEffectKind.SymbolBaseDamage, 1, SlotSymbolType.Diamond), // +1, 4개↑면 추가 +1 (조건부는 P2)
                P("P-20", "upg", RewardRarity.Uncommon, "저가치 증폭", "<sprite index=0>체리와 <sprite index=5>레몬이 함께 더 세게 때린다.", RunProposalEffectKind.SymbolBaseDamage, 1, SlotSymbolType.Cherry, SlotSymbolType.Lemon),
                P("P-21", "pat", RewardRarity.Uncommon, "4연격 훈련", "4개를 맞춘 족보가 크게 세진다.", RunProposalEffectKind.EngineEffect), // 4일치 족보 피해 +3 (P2)
                P("P-22", "pat", RewardRarity.Uncommon, "연쇄 계산", "한 번에 여러 족보가 터지면, 뒤엣것마다 추가 피해!", RunProposalEffectKind.EngineEffect), // 2번째 이후 족보마다 피해 +2 (P2)
                P("P-23", "retrig", RewardRarity.Uncommon, "반향 훈련", "5개를 맞추면 3개 효과까지 함께 터진다."), // 5일치 발동 시 3일치도 발동(규칙) (P2)
                P("P-24", "swap", RewardRarity.Uncommon, "교환 타격", "자리 바꾸기를 쓴 스핀은 피해가 오른다.", RunProposalEffectKind.EngineEffect), // swap 쓴 스핀 합산 피해 +2 (P2)
                P("P-25", "econ", RewardRarity.Uncommon, "별 수집가", "자리 바꾸기 없이 이기면 별조각을 하나 더 받는다."), // no-swap 승리 시 별조각 +1 (P2)
                P("P-26", "combat", RewardRarity.Uncommon, "마무리 본능", "5턴째 스핀이 강하게 터진다.", RunProposalEffectKind.EngineEffect), // 5턴째 합산 피해 +5 (P2)
                P("P-27", "clean", RewardRarity.Uncommon, "가지치기", "지금 가장 약한 심볼이 훨씬 덜 나오게 해서 슬롯을 정리한다."), // 최저 기본값 심볼 가중 -3 (자동 대상 P2)
                P("P-28", "status", RewardRarity.Uncommon, "가시 잎사귀", "<sprite index=4>클로버를 맞추면 가시가 돋아, 맞을 때 반격한다.", RunProposalEffectKind.EngineEffect), // 클로버 3개↑ → 가시 3 (P2)
                P("P-29", "status", RewardRarity.Uncommon, "흡혈 문장", "<sprite index=5>레몬·<sprite index=1>세븐으로 준 피해만큼 체력을 조금 회복한다."), // 레몬/7 피해 10% 회복(턴당 5) (P2)
                P("P-30", "status", RewardRarity.Uncommon, "약화 분말", "<sprite index=3>종·<sprite index=4>클로버를 맞추면 적의 공격이 약해진다.", RunProposalEffectKind.EngineEffect), // 종/클로버 3개↑ → 약화 1 (P2)

                // ── 희귀(rare) ──────────────────────────────────────────
                P("P-31", "upg", RewardRarity.Rare, "세븐 각인", "<sprite index=1>세븐이 훨씬 아프게 때린다.", RunProposalEffectKind.SymbolBaseDamage, 2, SlotSymbolType.Seven),
                P("P-32", "pat", RewardRarity.Rare, "완성된 문양", "5개를 맞춘 족보가 대박으로 터진다.", RunProposalEffectKind.EngineEffect), // 5일치 족보 피해 +8 (P2)
                P("P-33", "pat", RewardRarity.Rare, "문양 공명", "한 스핀에 족보 3개 이상이면 큰 추가 피해!", RunProposalEffectKind.EngineEffect), // 족보 3개↑면 합산 피해 +5 (P2)
                P("P-34", "retrig", RewardRarity.Rare, "다시 친화", "지금 가장 센 심볼에 [다시] 표식이 붙는다 — 그 심볼이 든 족보는 한 번 더 터진다!"), // 최고 기본값 심볼 등장 시 확률 [다시] (자동 대상 P2)
                P("P-35", "retrig", RewardRarity.Rare, "막타 정산", "5턴째 첫 족보가 한 번 더 터진다.", RunProposalEffectKind.EngineEffect), // 5턴째 첫 족보 재발동(전투당 1회) (P2)
                P("P-36", "econ", RewardRarity.Rare, "저축 습관", "별조각을 5개 넘게 모아두면 계속 더 세게 때린다.", RunProposalEffectKind.EngineEffect), // 보유 별조각 5+면 합산 피해 +2 (P2)
                P("P-37", "combat", RewardRarity.Rare, "최후의 별빛", "5턴째 첫 족보의 피해가 두 배!", RunProposalEffectKind.EngineEffect), // 5턴째 첫 족보 2배(전투당 1회) (P2)
                P("P-38", "clean", RewardRarity.Rare, "과밀 정리", "가장 많은 심볼을 줄이고, 가장 적은 심볼을 키운다."), // 최고 가중 -3, 최저 +3 (자동 대상 P2)

                // ── 전설(legendary) ─────────────────────────────────────
                P("P-39", "prob", RewardRarity.Legendary, "운명의 별자리", "행운이 크게 올라 귀한 심볼이 쏟아진다! (세븐이 너무 흔해지진 않음)"), // 행운 +6 (7 확률 20% 상한) (P2 luck)
                P("P-40", "retrig", RewardRarity.Legendary, "완전한 배열", "5개를 맞출 때마다 3개 효과까지 항상 함께 터진다!"), // 5일치 발동 시 3일치 항상 발동(규칙) (P2)
                P("P-41", "pat", RewardRarity.Legendary, "별의 축복", "모든 3개 족보가 더 세진다.", RunProposalEffectKind.EngineEffect), // 모든 3일치 족보 피해 +3 (P2)
                P("P-42", "retrig", RewardRarity.Legendary, "황금 손", "매 전투 첫 스핀의 모든 족보가 한 번 더 터진다!", RunProposalEffectKind.EngineEffect), // 첫 스핀 전체 재발동(전투당 1회) (P2)

                // ── 저주(curse) — 전부 1회성 계약 ───────────────────────
                P("P-43", "risk", RewardRarity.Curse, "악마의 제안", "별조각을 더 얻는다. 대신 다음 몬스터가 더 강해진다."), // 별조각 +1 / 다음 몬스터 HP +15% (P2)
                P("P-44", "risk", RewardRarity.Curse, "대박 집착", "<sprite index=1>세븐이 확 자주 나온다! 대신 <sprite index=0>체리·<sprite index=5>레몬이 줄어든다."), // 7 +3 / 체리·레몬 각 -3 (혼합 부호라 P2)
                P("P-45", "risk", RewardRarity.Curse, "굶주린 계약", "피해가 크게 오른다. 대신 회복이 크게 줄어든다."), // 피해 ×1.35 / 회복 ×0.65 (P2)
                P("P-46", "risk", RewardRarity.Curse, "피의 서약", "별조각을 훨씬 많이 번다. 대신 최대 체력이 줄어든다."), // 최대 HP -20% / 별조각 획득 +50% (P2)
            };
        }

        private static RunRewardDefinition P(
            string id,
            string category,
            RewardRarity rarity,
            string displayName,
            string description,
            RunProposalEffectKind proposalEffect = RunProposalEffectKind.None,
            int amount = 0,
            params SlotSymbolType[] symbols)
        {
            return new RunRewardDefinition(
                id,
                category,
                rarity,
                proposalEffect,
                symbols,
                amount,
                displayName,
                description);
        }
    }
}
