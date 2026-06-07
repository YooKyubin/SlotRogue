namespace SlotRogue.Relics.Data
{
    /// <summary><see cref="RelicConditionType.PatternCount"/> 조건의 세부 집계 방식.</summary>
    public enum PatternCountMode
    {
        /// <summary>발동한 족보 총 개수.</summary>
        Total = 0,

        /// <summary>서로 다른 심볼 족보의 종류 수.</summary>
        DistinctSymbols = 1,

        /// <summary>같은 심볼 족보가 반복 발동한 최대 개수.</summary>
        SameSymbolRepeat = 2,
    }
}
