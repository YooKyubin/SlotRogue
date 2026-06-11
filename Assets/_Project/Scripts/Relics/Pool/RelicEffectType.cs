namespace SlotRogue.Relics.Pool
{
    /// <summary>
    /// v20.3 유물 풀이 사용하는 효과 종류. (기존 <see cref="SlotRogue.Relics.Data.RelicEffectType"/>와는
    /// 별개 — 네임스페이스로 구분한다.) 효과 실행은 문자열이 아니라 이 enum으로 분기한다.
    ///
    /// Phase 1 실행 대상: AddDamage / AddBlock / Heal / ApplyBurn / ApplyCorrosion / ApplyShock.
    /// 그 외는 카탈로그에 등록만 하고 Phase 1 보상풀에서는 제외한다(미구현).
    /// </summary>
    public enum RelicEffectType
    {
        // ── Phase 1 (실제 실행) ─────────────────────────────────────────
        AddDamage = 0,
        AddBlock = 1,
        Heal = 2,
        ApplyBurn = 3,
        ApplyCorrosion = 4,
        ApplyShock = 5,

        // ── Phase 2 (등록만, 미실행) ────────────────────────────────────
        ModifyDamageMultiplier = 100, // 배율 증폭(패시브/조건부)
        AmplifyStatus = 101,          // 상태이상 부여량/최대 스택 증가
        AddRewardChoice = 102,        // 보상 선택지 +1
        AddRewardReroll = 103,        // 보상 리롤
        Lifesteal = 104,              // 입힌 피해 비율만큼 회복
        BlockToHeal = 105,            // 획득 방어도 비율만큼 회복
        ReviveOnce = 106,             // 1회 부활
        Special = 999,                // 그 외 복합 효과(저주/전설). customId 보유.
    }
}
