namespace SlotRogue.Relics.Data
{
    /// <summary>유물 시스템이 다루는 상태이상 종류. 실제 적용은 기존 전투 상태이상 시스템에 위임한다.</summary>
    public enum RelicStatusType
    {
        None = 0,
        Burn = 1,
        Poison = 2,
        Freeze = 3,
    }
}
