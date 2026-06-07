namespace SlotRogue.Relics.Data
{
    /// <summary>유물이 발동할지 판정하는 조건의 종류.</summary>
    public enum RelicConditionType
    {
        /// <summary>항상 발동.</summary>
        Always = 0,

        /// <summary>아무 족보나 1개 이상 발동.</summary>
        AnyPattern = 1,

        /// <summary>특정 심볼 족보 발동.</summary>
        SpecificSymbol = 2,

        /// <summary>특정 심볼 그룹(과일/행운/보물) 족보 발동.</summary>
        SymbolGroup = 3,

        /// <summary>특정 패턴(가로/세로/대각 등) 족보 발동.</summary>
        SpecificPattern = 4,

        /// <summary>한 턴에 발동한 족보 수 기준.</summary>
        PatternCount = 5,

        /// <summary>같은 턴에 지정한 여러 심볼 족보가 모두 발동.</summary>
        MultipleSpecificSymbolsInSameTurn = 6,

        /// <summary>한 턴에 족보가 하나도 발동하지 않음.</summary>
        NoPattern = 7,

        /// <summary>플레이어 HP가 특정 비율 이하.</summary>
        HpBelowPercent = 8,

        /// <summary>적이 특정 상태이상을 가지고 있고 족보가 발동.</summary>
        EnemyHasStatus = 9,

        /// <summary>슬롯 풀의 심볼 구성 기준(그룹 수/종류 수/총량).</summary>
        SymbolCountInPool = 10,
    }
}
