namespace SlotRogue.Relics.Pool
{
    /// <summary>유물이 언제/어떻게 발동하는지(v23).</summary>
    public enum RelicTriggerType
    {
        /// <summary>특정 심볼 족보가 requiredCount개 이상일 때.</summary>
        MatchSymbol = 0,

        /// <summary>특정 태그 심볼이 requiredCount개 이상일 때.</summary>
        MatchTag = 1,

        /// <summary>상시 패시브(보유만으로 적용). Phase 2.</summary>
        Passive = 2,

        /// <summary>전투 시작 시. Phase 2.</summary>
        BattleStart = 3,

        /// <summary>전투 종료 시. Phase 2.</summary>
        BattleEnd = 4,

        /// <summary>보상 단계 개입(선택지/리롤 등). Phase 2.</summary>
        Reward = 5,

        /// <summary>특수 조건부(적 상태/큰 족보/치명 피해 등).</summary>
        Conditional = 6,
    }
}
