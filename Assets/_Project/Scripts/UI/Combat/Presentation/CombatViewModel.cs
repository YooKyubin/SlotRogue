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

            ApplyParticipantSnapshot(combatEvent.IsPlayerParticipant, combatEvent.TargetAfter);
        }

        public void ApplyParticipantSnapshot(bool isPlayerParticipant, CombatParticipantSnapshot snapshot)
        {
            if (isPlayerParticipant)
            {
                PlayerHp = snapshot.Hp;
                PlayerShield = snapshot.Shield;
            }
            else
            {
                MonsterHp = snapshot.Hp;
                MonsterShield = snapshot.Shield;
            }
        }

        public void SetPlayerHp(int hp) => PlayerHp = hp;

        public void SetPlayerShield(int shield) => PlayerShield = shield;

        public void SetMonsterHp(int hp) => MonsterHp = hp;

        public void SetMonsterShield(int shield) => MonsterShield = shield;
    }
}
