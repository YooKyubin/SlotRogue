namespace SlotRogue.Data.GameFlow
{
    /// <summary>
    /// 전투 한 판의 등급입니다. 런 모드와 독립적이며,
    /// 무한모드 진행 생성기와 전투 난이도 스케일의 기준이 됩니다.
    /// </summary>
    public enum EncounterTier
    {
        Normal = 0,
        Elite = 1,
        Boss = 2,
    }
}
