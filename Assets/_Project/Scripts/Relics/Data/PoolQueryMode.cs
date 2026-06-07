namespace SlotRogue.Relics.Data
{
    /// <summary><see cref="RelicConditionType.SymbolCountInPool"/> 조건의 세부 질의 방식.</summary>
    public enum PoolQueryMode
    {
        /// <summary>지정 그룹 심볼이 풀에 N개 이상.</summary>
        GroupSymbolCountAtLeast = 0,

        /// <summary>풀의 심볼 종류 수가 N개 이하.</summary>
        DistinctSymbolTypesAtMost = 1,

        /// <summary>풀의 전체 심볼 총량이 N개 이상.</summary>
        TotalSymbolCountAtLeast = 2,
    }
}
