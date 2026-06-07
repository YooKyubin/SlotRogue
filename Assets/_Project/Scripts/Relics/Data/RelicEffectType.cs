namespace SlotRogue.Relics.Data
{
    /// <summary>유물이 적용하는 효과의 종류. 효과별 클래스를 만들지 않고 이 종류로 분기한다.</summary>
    public enum RelicEffectType
    {
        AddFlatDamage = 0,
        MultiplyDamage = 1,
        HealPlayer = 2,
        HealByDamagePercent = 3,
        AddShield = 4,
        AddGold = 5,
        ApplyBurn = 6,
        ApplyPoison = 7,
        ApplyFreeze = 8,
        AddCritChance = 9,
        AddCritDamage = 10,
        ChanceAddDamage = 11,
        ReduceEnemyAttack = 12,
        GainPotion = 13,
        GainRespin = 14,
        AddNextTurnBaseDamage = 15,
        CopyHighestPatternDamage = 16,
        ReviveOnce = 17,
        PlayerTakeDamage = 18,
        AddCurseSymbol = 19,
        RemoveRandomSymbol = 20,
        Custom = 21,
    }
}
