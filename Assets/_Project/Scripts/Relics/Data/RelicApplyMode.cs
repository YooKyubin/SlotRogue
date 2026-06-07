namespace SlotRogue.Relics.Data
{
    /// <summary>유물 효과를 한 번의 트리거에서 몇 번 반복 적용할지 결정한다.</summary>
    public enum RelicApplyMode
    {
        /// <summary>조건을 만족하면 1회만 적용.</summary>
        Once = 0,

        /// <summary>조건에 일치한 족보 개수만큼 반복 적용.</summary>
        PerMatchedPattern = 1,

        /// <summary>이번 턴에 발동한 모든 족보 개수만큼 반복 적용.</summary>
        PerAllPattern = 2,

        /// <summary>관련 상태이상 스택 수만큼 반복 적용.</summary>
        PerStack = 3,
    }
}
