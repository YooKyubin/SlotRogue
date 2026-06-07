using System;
using System.Collections.Generic;
using System.Linq;

namespace SlotRogue.Core.Combat
{
    public sealed class BattleSystem
    {
        private readonly EffectApplicator _effectApplicator;
        private readonly StatusEffectEngine _statusEffectEngine;
        private readonly List<CombatEvent> _events = new();
        private readonly List<CombatParticipant> _enemies = new();
        private readonly List<EnemyTurnState> _enemyTurnStates = new();
        private readonly Dictionary<int, CombatParticipant> _participantsById = new();
        private CombatParticipant _player = null!;

        public BattleSystem()
            : this(new EffectApplicator())
        {
        }

        public BattleSystem(EffectApplicator effectApplicator)
        {
            _effectApplicator = effectApplicator ?? new EffectApplicator();
            _statusEffectEngine = new StatusEffectEngine(_effectApplicator);
        }

        public BattlePhase CurrentPhase { get; private set; } = BattlePhase.NotInBattle;

        public BattleEndReason EndReason { get; private set; } = BattleEndReason.None;

        public CombatParticipant Player => _player;

        [Obsolete("Use Enemies for multi-enemy combat. Use Enemies[0] only for legacy single-enemy callers.")]
        public CombatParticipant Monster => _enemies.Count > 0 ? _enemies[0] : null;

        public IReadOnlyList<CombatParticipant> Enemies => _enemies;

        public IReadOnlyList<CombatEvent> Events => _events;

        public IReadOnlyList<CombatEffect> UpcomingEnemyActions =>
            _enemyTurnStates.FirstOrDefault(state => !state.Participant.IsDead)?.Schedule.UpcomingActions
            ?? Array.Empty<CombatEffect>();

        public int UpcomingMonsterTurnIndex =>
            _enemyTurnStates.FirstOrDefault(state => !state.Participant.IsDead)?.Schedule.UpcomingTurnIndex ?? 0;

        public bool CanApplyPlayerTurn => CurrentPhase == BattlePhase.PlayerTurn;

        public void StartBattle(
            CombatParticipant player,
            CombatParticipant monster,
            IReadOnlyList<CombatEffect> monsterTurnActions)
        {
            StartBattle(player, monster, new MonsterTurnSchedule(monsterTurnActions));
        }

        public void StartBattle(CombatParticipant player, CombatParticipant monster, MonsterTurnSchedule monsterTurnSchedule)
        {
            StartBattle(
                player,
                new[] { monster },
                new[] { monsterTurnSchedule ?? throw new ArgumentNullException(nameof(monsterTurnSchedule)) });
        }

        public void StartBattle(
            CombatParticipant player,
            IReadOnlyList<CombatParticipant> enemies,
            IReadOnlyList<MonsterTurnSchedule> enemyTurnSchedules)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (enemies == null || enemies.Count == 0)
            {
                throw new ArgumentException("At least one enemy is required.", nameof(enemies));
            }

            if (enemyTurnSchedules == null || enemyTurnSchedules.Count != enemies.Count)
            {
                throw new ArgumentException("Enemy schedules must match enemy count.", nameof(enemyTurnSchedules));
            }

            _player = EnsureParticipantMeta(player, fallbackId: 1, fallbackTeam: CombatTeam.Player);
            _enemies.Clear();
            _enemyTurnStates.Clear();
            _participantsById.Clear();
            RegisterParticipant(_player);

            for (int index = 0; index < enemies.Count; index++)
            {
                CombatParticipant enemy = EnsureParticipantMeta(
                    enemies[index] ?? throw new ArgumentNullException(nameof(enemies)),
                    fallbackId: 100 + index,
                    fallbackTeam: CombatTeam.Enemy);
                MonsterTurnSchedule schedule =
                    enemyTurnSchedules[index] ?? throw new ArgumentNullException(nameof(enemyTurnSchedules));
                schedule.Reset();
                _enemies.Add(enemy);
                _enemyTurnStates.Add(new EnemyTurnState(enemy, schedule));
                RegisterParticipant(enemy);
            }

            EndReason = BattleEndReason.None;
            _events.Clear();

            SetPhase(BattlePhase.PlayerTurn);
        }

        public BattleApplyResult ApplyPlayerTurn(
            IReadOnlyList<CombatEffect> playerEffects,
            CombatParticipantId selectedTargetId = default)
        {
            if (CurrentPhase != BattlePhase.PlayerTurn)
            {
                return BattleApplyResult.Rejected(CurrentPhase);
            }

            SetPhase(BattlePhase.Resolving);

            RunParticipantTurnStart(_player);
            if (TryEndBattle())
            {
                return AcceptedResult();
            }

            if (TrySkipParticipantAction(_player))
            {
                RunParticipantTurnEnd(_player);
                if (TryEndBattle())
                {
                    return AcceptedResult();
                }

                ResetShieldByTeam(CombatTeam.Enemy);
                RunEnemyTurn();
                return AcceptedResult();
            }

            ApplyEffects(playerEffects ?? Array.Empty<CombatEffect>(), _player, selectedTargetId);

            if (TryEndBattleAfterPlayerTurn())
            {
                return AcceptedResult();
            }

            RunParticipantTurnEnd(_player);

            if (TryEndBattleAfterPlayerTurn())
            {
                return AcceptedResult();
            }

            ResetShieldByTeam(CombatTeam.Enemy);

            if (TryEndBattleAfterPlayerTurn())
            {
                return AcceptedResult();
            }

            RunEnemyTurn();

            return AcceptedResult();
        }

        private void RunEnemyTurn()
        {
            SetPhase(BattlePhase.EnemyTurn);

            for (int index = 0; index < _enemyTurnStates.Count; index++)
            {
                EnemyTurnState enemyState = _enemyTurnStates[index];
                if (enemyState.Participant.IsDead)
                {
                    continue;
                }

                RunParticipantTurnStart(enemyState.Participant);
                if (TryEndBattle())
                {
                    return;
                }

                if (TrySkipParticipantAction(enemyState.Participant))
                {
                    enemyState.Schedule.ConsumeUpcomingTurn();
                    RunParticipantTurnEnd(enemyState.Participant);
                    if (TryEndBattle())
                    {
                        return;
                    }

                    continue;
                }

                IReadOnlyList<CombatEffect> enemyTurnActions = enemyState.Schedule.ConsumeUpcomingTurn();
                ApplyEffects(enemyTurnActions, enemyState.Participant, default);

                if (TryEndBattle())
                {
                    return;
                }

                RunParticipantTurnEnd(enemyState.Participant);

                if (TryEndBattle())
                {
                    return;
                }
            }

            if (TryEndBattle())
            {
                return;
            }

            ResetShieldByTeam(CombatTeam.Player);

            if (TryEndBattle())
            {
                return;
            }

            SetPhase(BattlePhase.PlayerTurn);
        }

        private void ApplyEffects(
            IReadOnlyList<CombatEffect> effects,
            CombatParticipant source,
            CombatParticipantId selectedTargetId)
        {
            for (int index = 0; index < effects.Count; index++)
            {
                CombatEffect effect = effects[index];
                IReadOnlyList<CombatParticipant> targets = ResolveTargets(effect, source, selectedTargetId);

                for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    CombatParticipant target = targets[targetIndex];
                    CombatParticipantSnapshot targetBefore = CaptureSnapshot(target);
                    EffectApplyResult applyResult = ApplyEffectToTarget(effect, target);
                    CombatParticipantSnapshot targetAfter = CaptureSnapshot(target);
                    bool isPlayerTarget = target.Team == CombatTeam.Player;

                    _events.Add(new CombatEvent(
                        CombatEventKind.EffectApplied,
                        CurrentPhase,
                        effect,
                        applyResult,
                        isPlayerParticipant: isPlayerTarget,
                        targetParticipantId: target.Id,
                        targetBefore: targetBefore,
                        targetAfter: targetAfter));

                    if (TryEndBattle())
                    {
                        return;
                    }
                }
            }
        }

        private EffectApplyResult ApplyEffectToTarget(CombatEffect effect, CombatParticipant target)
        {
            if (effect.Kind == CombatEffectKind.ApplyStatus)
            {
                _statusEffectEngine.ApplyStatus(effect.StatusEffect, target, CurrentPhase, _events);
                return EffectApplyResult.None;
            }

            return _effectApplicator.ApplyToParticipant(effect, target);
        }

        private void RunParticipantTurnStart(CombatParticipant participant)
        {
            _statusEffectEngine.TickTurnStart(participant, CurrentPhase, _events);
        }

        private bool TrySkipParticipantAction(CombatParticipant participant)
        {
            return _statusEffectEngine.ShouldSkipAction(participant, CurrentPhase, _events);
        }

        private void RunParticipantTurnEnd(CombatParticipant participant)
        {
            _statusEffectEngine.TickTurnEnd(participant, CurrentPhase, _events);
        }

        private bool TryEndBattleAfterPlayerTurn()
        {
            if (AreAllEnemiesDefeated())
            {
                EndBattle(BattleEndReason.Victory);
                return true;
            }

            if (IsPlayerTeamDefeated())
            {
                EndBattle(BattleEndReason.Defeat);
                return true;
            }

            return false;
        }

        private bool TryEndBattle()
        {
            if (IsPlayerTeamDefeated())
            {
                EndBattle(BattleEndReason.Defeat);
                return true;
            }

            if (AreAllEnemiesDefeated())
            {
                EndBattle(BattleEndReason.Victory);
                return true;
            }

            return false;
        }

        private void EndBattle(BattleEndReason endReason)
        {
            EndReason = endReason;
            SetPhase(BattlePhase.Ended);
            _events.Add(new CombatEvent(
                CombatEventKind.BattleEnded,
                BattlePhase.Ended,
                endReason: endReason));
        }

        private void ResetShieldByTeam(CombatTeam team)
        {
            if (team == CombatTeam.Player)
            {
                ResetShield(_player);
                return;
            }

            for (int index = 0; index < _enemies.Count; index++)
            {
                if (_enemies[index].IsDead)
                {
                    continue;
                }

                ResetShield(_enemies[index]);
            }
        }

        private void SetPhase(BattlePhase phase)
        {
            CurrentPhase = phase;
            _events.Add(new CombatEvent(CombatEventKind.PhaseChanged, phase));
        }

        private BattleApplyResult AcceptedResult() =>
            new(accepted: true, CurrentPhase, EndReason);

        private void ResetShield(CombatParticipant participant)
        {
            participant.Shield = 0;
            _events.Add(new CombatEvent(
                CombatEventKind.ShieldReset,
                CurrentPhase,
                isPlayerParticipant: participant.Team == CombatTeam.Player,
                targetParticipantId: participant.Id));
        }

        private static CombatParticipantSnapshot CaptureSnapshot(CombatParticipant participant) =>
            new(participant.CurrentHp, participant.Shield);

        private IReadOnlyList<CombatParticipant> ResolveTargets(
            CombatEffect effect,
            CombatParticipant source,
            CombatParticipantId selectedTargetId)
        {
            if (effect.Target.Mode == CombatTargetMode.Self)
            {
                return new[] { source };
            }

            CombatTeam targetTeam = source.Team == CombatTeam.Player ? CombatTeam.Enemy : CombatTeam.Player;
            List<CombatParticipant> aliveTargets = GetAliveParticipantsByTeam(targetTeam);
            if (aliveTargets.Count == 0)
            {
                return Array.Empty<CombatParticipant>();
            }

            if (effect.Target.Mode == CombatTargetMode.AllEnemies)
            {
                return aliveTargets;
            }

            CombatParticipantId requestedId = effect.Target.ParticipantId.IsValid
                ? effect.Target.ParticipantId
                : selectedTargetId;

            if (requestedId.IsValid &&
                _participantsById.TryGetValue(requestedId.Value, out CombatParticipant explicitTarget) &&
                explicitTarget.Team == targetTeam &&
                !explicitTarget.IsDead)
            {
                return new[] { explicitTarget };
            }

            if (effect.Target.Mode == CombatTargetMode.RandomEnemy)
            {
                return new[] { aliveTargets[0] };
            }

            return new[] { aliveTargets[0] };
        }

        private List<CombatParticipant> GetAliveParticipantsByTeam(CombatTeam team)
        {
            if (team == CombatTeam.Player)
            {
                return _player.IsDead ? new List<CombatParticipant>() : new List<CombatParticipant> { _player };
            }

            var aliveEnemies = new List<CombatParticipant>();
            for (int index = 0; index < _enemies.Count; index++)
            {
                if (!_enemies[index].IsDead)
                {
                    aliveEnemies.Add(_enemies[index]);
                }
            }

            return aliveEnemies;
        }

        private bool IsPlayerTeamDefeated() => _player == null || _player.IsDead;

        private bool AreAllEnemiesDefeated()
        {
            for (int index = 0; index < _enemies.Count; index++)
            {
                if (!_enemies[index].IsDead)
                {
                    return false;
                }
            }

            return true;
        }

        private void RegisterParticipant(CombatParticipant participant)
        {
            _participantsById[participant.Id.Value] = participant;
        }

        private static CombatParticipant EnsureParticipantMeta(CombatParticipant participant, int fallbackId, CombatTeam fallbackTeam)
        {
            CombatParticipantId resolvedId = participant.Id.IsValid
                ? participant.Id
                : new CombatParticipantId(fallbackId);
            CombatTeam resolvedTeam = participant.Team != CombatTeam.None
                ? participant.Team
                : fallbackTeam;

            return new CombatParticipant(
                participant.MaxHp,
                participant.CurrentHp,
                participant.Shield,
                resolvedId,
                resolvedTeam);
        }

        private sealed class EnemyTurnState
        {
            public EnemyTurnState(CombatParticipant participant, MonsterTurnSchedule schedule)
            {
                Participant = participant;
                Schedule = schedule;
            }

            public CombatParticipant Participant { get; }

            public MonsterTurnSchedule Schedule { get; }
        }
    }
}
