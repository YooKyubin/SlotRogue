using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class CombatViewModel
    {
        public int PlayerHp { get; private set; }

        public int PlayerShield { get; private set; }

        public int MonsterHp { get; private set; }

        public int MonsterShield { get; private set; }

        public void SyncFrom(BattleSystem battle)
        {
            CombatParticipant player = battle.Player;
            CombatParticipant monster = battle.Monster;
            PlayerHp = player.CurrentHp;
            PlayerShield = player.Shield;
            MonsterHp = monster.CurrentHp;
            MonsterShield = monster.Shield;
        }

        public void ApplySnapshot(CombatEvent combatEvent)
        {
            if (!combatEvent.HasTargetSnapshot)
            {
                return;
            }

            CombatParticipantSnapshot after = combatEvent.TargetAfter;

            if (combatEvent.IsPlayerParticipant)
            {
                PlayerHp = after.Hp;
                PlayerShield = after.Shield;
            }
            else
            {
                MonsterHp = after.Hp;
                MonsterShield = after.Shield;
            }
        }
    }
}
