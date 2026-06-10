using SlotRogue.Core.Combat;
using System;
using System.Collections.Generic;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class CombatViewModel
    {
        private readonly Dictionary<int, CombatParticipantSnapshot> _participants = new();

        public event Action Changed;

        public int PlayerHp { get; private set; }

        public int PlayerShield { get; private set; }

        public bool TryGetParticipantSnapshot(
            CombatParticipantId participantId,
            out CombatParticipantSnapshot snapshot)
        {
            if (participantId.IsValid &&
                _participants.TryGetValue(participantId.Value, out snapshot))
            {
                return true;
            }

            snapshot = default;
            return false;
        }

        public void SyncFrom(BattleSystem battle)
        {
            CombatParticipant player = battle.Player;
            PlayerHp = player.CurrentHp;
            PlayerShield = player.Shield;
            _participants.Clear();
            _participants[player.Id.Value] = new CombatParticipantSnapshot(player.CurrentHp, player.Shield);

            for (int index = 0; index < battle.Enemies.Count; index++)
            {
                CombatParticipant enemy = battle.Enemies[index];
                _participants[enemy.Id.Value] = new CombatParticipantSnapshot(enemy.CurrentHp, enemy.Shield);
            }

            PublishChanged();
        }

        public void ApplySnapshot(CombatEvent combatEvent)
        {
            if (!combatEvent.HasTargetSnapshot)
            {
                return;
            }

            ApplyParticipantSnapshot(combatEvent.TargetParticipantId, combatEvent.TargetAfter, combatEvent.IsPlayerParticipant);
        }

        public void ApplyParticipantSnapshot(bool isPlayerParticipant, CombatParticipantSnapshot snapshot)
        {
            ApplyParticipantSnapshot(default, snapshot, isPlayerParticipant);
        }

        public void ApplyParticipantSnapshot(
            CombatParticipantId participantId,
            CombatParticipantSnapshot snapshot,
            bool isPlayerParticipantHint = false)
        {
            if (participantId.IsValid)
            {
                _participants[participantId.Value] = snapshot;
            }

            bool isPlayerParticipant = isPlayerParticipantHint || participantId.Value == 1;
            if (isPlayerParticipant)
            {
                PlayerHp = snapshot.Hp;
                PlayerShield = snapshot.Shield;
            }

            PublishChanged();
        }

        public void SetPlayerHp(int hp)
        {
            PlayerHp = hp;
            PublishChanged();
        }

        public void SetPlayerShield(int shield)
        {
            PlayerShield = shield;
            PublishChanged();
        }

        public void SetParticipantHp(
            CombatParticipantId participantId,
            int hp,
            bool isPlayerParticipantHint = false)
        {
            bool isPlayer = isPlayerParticipantHint || participantId.Value == 1;
            if (isPlayer)
            {
                PlayerHp = hp;
                if (_participants.TryGetValue(1, out CombatParticipantSnapshot playerSnapshot))
                {
                    _participants[1] = new CombatParticipantSnapshot(hp, playerSnapshot.Shield);
                }

                PublishChanged();
                return;
            }

            if (participantId.IsValid && _participants.TryGetValue(participantId.Value, out CombatParticipantSnapshot snapshot))
            {
                _participants[participantId.Value] = new CombatParticipantSnapshot(hp, snapshot.Shield);
            }

            PublishChanged();
        }

        public void SetParticipantShield(
            CombatParticipantId participantId,
            int shield,
            bool isPlayerParticipantHint = false)
        {
            bool isPlayer = isPlayerParticipantHint || participantId.Value == 1;
            if (isPlayer)
            {
                PlayerShield = shield;
                if (_participants.TryGetValue(1, out CombatParticipantSnapshot playerSnapshot))
                {
                    _participants[1] = new CombatParticipantSnapshot(playerSnapshot.Hp, shield);
                }

                PublishChanged();
                return;
            }

            if (participantId.IsValid && _participants.TryGetValue(participantId.Value, out CombatParticipantSnapshot snapshot))
            {
                _participants[participantId.Value] = new CombatParticipantSnapshot(snapshot.Hp, shield);
            }

            PublishChanged();
        }

        private void PublishChanged()
        {
            Changed?.Invoke();
        }
    }
}
