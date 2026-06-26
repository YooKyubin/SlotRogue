using SlotRogue.Core.Combat;
using System;
using System.Collections.Generic;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class CombatViewModel
    {
        private readonly Dictionary<int, CombatParticipantSnapshot> _participants = new();
        private readonly CombatStatusPresentationState _statuses = new();

        public event Action Changed;

        public int PlayerHp { get; private set; }

        public int PlayerShield { get; private set; }

        public StatusEffectViewData[] GetStatuses(CombatParticipantId participantId)
        {
            return _statuses.GetAll(participantId);
        }

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
            _statuses.Clear();
            _participants[player.Id.Value] = new CombatParticipantSnapshot(player.CurrentHp, player.Shield);
            SyncStatuses(player);

            for (int index = 0; index < battle.Enemies.Count; index++)
            {
                CombatParticipant enemy = battle.Enemies[index];
                _participants[enemy.Id.Value] = new CombatParticipantSnapshot(enemy.CurrentHp, enemy.Shield);
                SyncStatuses(enemy);
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

        public void AddOrReplaceStatus(
            CombatParticipantId participantId,
            StatusEffectViewData status)
        {
            _statuses.AddOrReplace(participantId, status);
        }

        public void RemoveStatus(
            CombatParticipantId participantId,
            StatusEffectKind kind)
        {
            _statuses.Remove(participantId, kind);
        }

        public void PublishStatusChanged()
        {
            PublishChanged();
        }

        private void SyncStatuses(CombatParticipant participant)
        {
            for (int index = 0; index < participant.StatusEffects.Count; index++)
            {
                _statuses.AddOrReplace(
                    participant.Id,
                    StatusEffectPresentationMapper.Map(participant.StatusEffects[index]));
            }
        }

        private void PublishChanged()
        {
            Changed?.Invoke();
        }
    }

    public sealed class CombatStatusPresentationState
    {
        private readonly Dictionary<int, Dictionary<StatusEffectKind, StatusEffectViewData>> _statusesByParticipantId =
            new();

        public bool TryGet(
            CombatParticipantId participantId,
            StatusEffectKind kind,
            out StatusEffectViewData status)
        {
            if (participantId.IsValid &&
                _statusesByParticipantId.TryGetValue(
                    participantId.Value,
                    out Dictionary<StatusEffectKind, StatusEffectViewData> statuses))
            {
                return statuses.TryGetValue(kind, out status);
            }

            status = default;
            return false;
        }

        public void AddOrReplace(CombatParticipantId participantId, StatusEffectViewData status)
        {
            if (!participantId.IsValid)
            {
                throw new ArgumentException("Participant ID must be valid.", nameof(participantId));
            }

            if (!_statusesByParticipantId.TryGetValue(
                    participantId.Value,
                    out Dictionary<StatusEffectKind, StatusEffectViewData> statuses))
            {
                statuses = new Dictionary<StatusEffectKind, StatusEffectViewData>();
                _statusesByParticipantId.Add(participantId.Value, statuses);
            }

            statuses[status.Kind] = status;
        }

        public bool Remove(CombatParticipantId participantId, StatusEffectKind kind)
        {
            if (!participantId.IsValid ||
                !_statusesByParticipantId.TryGetValue(
                    participantId.Value,
                    out Dictionary<StatusEffectKind, StatusEffectViewData> statuses) ||
                !statuses.Remove(kind))
            {
                return false;
            }

            if (statuses.Count == 0)
            {
                _statusesByParticipantId.Remove(participantId.Value);
            }

            return true;
        }

        public StatusEffectViewData[] GetAll(CombatParticipantId participantId)
        {
            if (!participantId.IsValid ||
                !_statusesByParticipantId.TryGetValue(
                    participantId.Value,
                    out Dictionary<StatusEffectKind, StatusEffectViewData> statuses))
            {
                return Array.Empty<StatusEffectViewData>();
            }

            var result = new List<StatusEffectViewData>(statuses.Values);
            result.Sort((left, right) => left.Kind.CompareTo(right.Kind));
            return result.ToArray();
        }

        public void Clear()
        {
            _statusesByParticipantId.Clear();
        }
    }
}
