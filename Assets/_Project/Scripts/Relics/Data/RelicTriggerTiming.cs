namespace SlotRogue.Relics.Data
{
    /// <summary>유물 효과가 실행되는 전투 흐름상의 타이밍.</summary>
    public enum RelicTriggerTiming
    {
        OnBattleStart = 0,
        OnSpinStart = 1,
        OnPatternResolved = 2,
        OnBeforeDamage = 3,
        OnAfterDamage = 4,
        OnEnemyTurnStart = 5,
        OnEnemyTurnEnd = 6,
        OnBattleWin = 7,
        OnPlayerDeath = 8,
    }
}
