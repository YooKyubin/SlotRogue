namespace SlotRogue.Relics.Data
{
    /// <summary>
    /// 유물이 "적에게 이 상태이상을 걸어달라"고 전투에 넘기는 요청 데이터.
    /// 유물 시스템은 상태이상을 직접 처리하지 않고 이 요청만 누적하며,
    /// 슬롯/전투 연결 계층이 기존 상태이상 시스템(StatusEffectSpec 등)으로 변환해 적용한다.
    /// </summary>
    public readonly struct RelicStatusApplication
    {
        public RelicStatusApplication(RelicStatusType type, int duration, int magnitude, bool stack)
        {
            Type = type;
            Duration = duration;
            Magnitude = magnitude;
            Stack = stack;
        }

        public RelicStatusType Type { get; }

        /// <summary>지속 턴 수(화상 3, 빙결 1 등). 독은 0(스택 기반).</summary>
        public int Duration { get; }

        /// <summary>화상: 턴당 피해(2). 독: 추가 스택 수. 빙결: 피해 감소율(%) 50.</summary>
        public int Magnitude { get; }

        /// <summary>true면 누적(독), false면 지속시간 갱신(화상/빙결).</summary>
        public bool Stack { get; }

        // 스펙 표준값. 화상 3턴/턴당 2, 독 스택, 빙결 다음 공격 50% 감소.
        public static RelicStatusApplication Burn(int duration = 3, int magnitude = 2) =>
            new RelicStatusApplication(RelicStatusType.Burn, duration, magnitude, stack: false);

        public static RelicStatusApplication Poison(int stacks = 1) =>
            new RelicStatusApplication(RelicStatusType.Poison, duration: 0, magnitude: stacks, stack: true);

        public static RelicStatusApplication Freeze(int reductionPercent = 50) =>
            new RelicStatusApplication(RelicStatusType.Freeze, duration: 1, magnitude: reductionPercent, stack: false);
    }
}
