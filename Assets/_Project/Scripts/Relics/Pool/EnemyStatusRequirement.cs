namespace SlotRogue.Relics.Pool
{
    /// <summary>
    /// Conditional 트리거 유물이 요구하는 적 상태이상(v23).
    /// </summary>
    public enum EnemyStatusRequirement
    {
        None = 0,

        /// <summary>임의의 상태이상 1개 이상(U-13 상태 추격자).</summary>
        Any = 1,

        /// <summary>화상 상태(U-16 그을린 추격자).</summary>
        Burn = 2,

        /// <summary>감염 상태(U-17 감염 추격자).</summary>
        Infect = 3,
    }
}
