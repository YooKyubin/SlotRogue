namespace SlotRogue.Relics.Pool
{
    /// <summary>
    /// v23 유물 풀이 사용하는 효과 종류. 효과 실행은 문자열이 아니라 이 enum으로 분기한다.
    ///
    /// Phase 1 실행 대상: AddDamage / AddBlock / Heal과 지원 상태 효과.
    /// 그 외는 카탈로그에 등록만 하고 Phase 1 보상풀에서는 제외한다(미구현).
    /// </summary>
    public enum RelicEffectType
    {
        // ── 기본 수치 효과 ──────────────────────────────────────────────
        AddDamage = 0,
        AddBlock = 1,
        Heal = 2,
        ApplyBurn = 3,

        /// <summary>감염 부여.</summary>
        ApplyInfect = 4,

        // ── Phase 2 (등록만, 미실행) ────────────────────────────────────
        ModifyDamageMultiplier = 100, // 배율 증폭(패시브/조건부)
        AmplifyStatus = 101,          // 상태이상 부여량/상한 증가
        AddRewardChoice = 102,        // 보상 선택지 +1
        AddRewardReroll = 103,        // 보상 리롤
        Lifesteal = 104,              // 입힌 피해 비율만큼 회복(value=%, value2=턴당 상한)
        BlockToHeal = 105,            // 획득 방어도 비율만큼 회복
        ReviveOnce = 106,             // 1회 부활
        ApplyVulnerable = 107,        // 취약 부여(다음 피격 행동 N회 직접 피해 +20%)
        ApplyWeak = 108,              // 약화 부여(다음 공격 행동 N회 직접 피해 -20%)
        GainThorns = 109,             // 가시 획득(직접 공격 피격 시 확률 반격)
        Special = 999,                // 그 외 복합 효과(저주/전설).
    }
}
