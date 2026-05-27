using SlotRogue.Data.Combat;

namespace SlotRogue.Core.Combat
{
    public readonly struct CombatEvent
    {
        private CombatEvent(
            CombatEventKind kind,
            int amount,
            MonsterAction monsterAction,
            BattleEndReason endReason)
        {
            Kind = kind;
            Amount = amount;
            MonsterAction = monsterAction;
            EndReason = endReason;
        }

        public CombatEventKind Kind { get; }

        public int Amount { get; }

        public MonsterAction MonsterAction { get; }

        public BattleEndReason EndReason { get; }

        public static CombatEvent PlayerDamageToMonster(int damageDealt)
        {
            return new CombatEvent(CombatEventKind.PlayerDamageToMonster, damageDealt, default, BattleEndReason.None);
        }

        public static CombatEvent MonsterDamageToPlayer(int damageDealt)
        {
            return new CombatEvent(CombatEventKind.MonsterDamageToPlayer, damageDealt, default, BattleEndReason.None);
        }

        public static CombatEvent MonsterActionExecuted(MonsterAction action)
        {
            return new CombatEvent(CombatEventKind.MonsterActionExecuted, 0, action, BattleEndReason.None);
        }

        public static CombatEvent PlayerHealed(int healAmount)
        {
            return new CombatEvent(CombatEventKind.PlayerHealed, healAmount, default, BattleEndReason.None);
        }

        public static CombatEvent MonsterHealed(int healAmount)
        {
            return new CombatEvent(CombatEventKind.MonsterHealed, healAmount, default, BattleEndReason.None);
        }

        public static CombatEvent BattleEnded(BattleEndReason reason)
        {
            return new CombatEvent(CombatEventKind.BattleEnded, 0, default, reason);
        }
    }
}
