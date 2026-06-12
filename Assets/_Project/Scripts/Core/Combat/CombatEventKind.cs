namespace SlotRogue.Core.Combat
{
    public enum CombatEventKind
    {
        PhaseChanged = 0,
        EffectApplied = 1,
        ShieldReset = 2,
        BattleEnded = 3,
        StatusApplied = 4,
        StatusTicked = 5,
        StatusExpired = 6,
        ActionSkipped = 7,
        ActionCompleted = 8,
    }
}
