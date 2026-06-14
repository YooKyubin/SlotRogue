using System;
using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class BattleSystem
    {
        private readonly EffectApplicator _effectApplicator;
        private readonly StatusEffectEngine _statusEffectEngine;
        private readonly List<CombatEvent> _events = new();
        private readonly List<CombatParticipant> _enemies = new();
        private readonly List<EnemyRuntime> _enemyRuntimes = new();
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

        public IReadOnlyList<CombatParticipant> Enemies => _enemies;

        public IReadOnlyList<CombatEvent> Events => _events;

        public bool CanApplyPlayerTurn => CurrentPhase == BattlePhase.PlayerTurn;

        public bool TryGetUpcomingEnemyTurn(
            CombatParticipantId participantId,
            out EnemyUpcomingTurn upcomingTurn)
        {
            if (!participantId.IsValid)
            {
                upcomingTurn = default;
                return false;
            }

            for (int index = 0; index < _enemyRuntimes.Count; index++)
            {
                EnemyRuntime runtime = _enemyRuntimes[index];
                CombatParticipant participant = runtime.Participant;

                if (participant.Id.Value != participantId.Value)
                {
                    continue;
                }

                if (participant.IsDead)
                {
                    upcomingTurn = default;
                    return false;
                }

                upcomingTurn = new EnemyUpcomingTurn(participant.Id, runtime.UpcomingPlan);
                return true;
            }

            upcomingTurn = default;
            return false;
        }

        public void StartBattle(CombatParticipant player, CombatParticipant monster, IReadOnlyList<CombatEffect> monsterTurnActions)
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

            _player = player;
            _enemies.Clear();
            _enemyRuntimes.Clear();
            _participantsById.Clear();
            RegisterParticipant(_player);

            for (int index = 0; index < enemies.Count; index++)
            {
                CombatParticipant enemy = enemies[index] ?? throw new ArgumentNullException(nameof(enemies));
                MonsterTurnSchedule schedule = enemyTurnSchedules[index] ?? throw new ArgumentNullException(nameof(enemyTurnSchedules));
                EnemyRuntime runtime = CreateRuntimeFromLegacySchedule(enemy, schedule);
                _enemies.Add(enemy);
                _enemyRuntimes.Add(runtime);
                RegisterParticipant(enemy);
            }

            EndReason = BattleEndReason.None;
            _events.Clear();

            PlanNextActionsForAllEnemies();
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

            if (RunBattleStep(() => RunParticipantTurnStart(_player)))
            {
                return AcceptedResult();
            }

            if (!TrySkipParticipantAction(_player) &&
                ApplyEffects(playerEffects ?? Array.Empty<CombatEffect>(), _player, selectedTargetId))
            {
                return AcceptedResult();
            }

            if (RunBattleStep(() => RunParticipantTurnEnd(_player)))
            {
                return AcceptedResult();
            }

            if (RunBattleStep(() => ResetShieldByTeam(CombatTeam.Enemy)))
            {
                return AcceptedResult();
            }

            RunEnemyTurn();

            return AcceptedResult();
        }

        private void RunEnemyTurn()
        {
            SetPhase(BattlePhase.EnemyTurn);

            for (int index = 0; index < _enemyRuntimes.Count; index++)
            {
                EnemyRuntime enemyRuntime = _enemyRuntimes[index];
                CombatParticipant enemy = enemyRuntime.Participant;
                if (enemy.IsDead)
                {
                    continue;
                }

                if (RunBattleStep(() => RunParticipantTurnStart(enemy)))
                {
                    return;
                }

                bool shouldSkipAction = TrySkipParticipantAction(enemy);
                IReadOnlyList<CombatEffect> enemyTurnActions = enemyRuntime.UpcomingPlan.Effects;
                enemyRuntime.PlanNextAction(CreateEnemyActionContext(enemy));
                if (!shouldSkipAction &&
                    ApplyEffects(enemyTurnActions, enemy, default))
                {
                    return;
                }

                if (RunBattleStep(() => RunParticipantTurnEnd(enemy)))
                {
                    return;
                }
            }

            if (RunBattleStep(() => ResetShieldByTeam(CombatTeam.Player)))
            {
                return;
            }

            SetPhase(BattlePhase.PlayerTurn);
        }

        private bool RunBattleStep(Action step)
        {
            if (CurrentPhase == BattlePhase.Ended)
            {
                return true;
            }

            step();

            return TryEndBattle();
        }

        private bool ApplyEffects(
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
                    CombatParticipantSnapshot targetBefore = target.CaptureSnapshot();
                    EffectApplyResult applyResult = ApplyEffectToTarget(effect, target);
                    CombatParticipantSnapshot targetAfter = target.CaptureSnapshot();
                    bool isPlayerTarget = target.Team == CombatTeam.Player;

                    _events.Add(new CombatEvent(
                        CombatEventKind.EffectApplied,
                        CurrentPhase,
                        effect,
                        applyResult,
                        isPlayerParticipant: isPlayerTarget,
                        targetParticipantId: target.Id,
                        targetBefore: targetBefore,
                        targetAfter: targetAfter,
                        sourceParticipantId: source.Id));

                    if (TryEndBattle())
                    {
                        return true;
                    }
                }

                _events.Add(new CombatEvent(
                    CombatEventKind.ActionCompleted,
                    CurrentPhase,
                    effect,
                    sourceParticipantId: source.Id));
            }

            return false;
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

        private BattleApplyResult AcceptedResult()
        {
            return new(accepted: true, CurrentPhase, EndReason);
        }

        private void ResetShield(CombatParticipant participant)
        {
            CombatParticipantSnapshot targetBefore = participant.CaptureSnapshot();
            participant.ResetShield();
            CombatParticipantSnapshot targetAfter = participant.CaptureSnapshot();
            _events.Add(new CombatEvent(
                CombatEventKind.ShieldReset,
                CurrentPhase,
                isPlayerParticipant: participant.Team == CombatTeam.Player,
                targetParticipantId: participant.Id,
                targetBefore: targetBefore,
                targetAfter: targetAfter));
        }

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

        private void PlanNextActionsForAllEnemies()
        {
            for (int index = 0; index < _enemyRuntimes.Count; index++)
            {
                EnemyRuntime runtime = _enemyRuntimes[index];
                runtime.PlanNextAction(CreateEnemyActionContext(runtime.Participant));
            }
        }

        private EnemyActionContext CreateEnemyActionContext(CombatParticipant enemy)
        {
            return new(enemy, _player, _enemies, turnNumber: 0);
        }

        private static EnemyRuntime CreateRuntimeFromLegacySchedule(
            CombatParticipant enemy,
            MonsterTurnSchedule schedule)
        {
            // Temporary migration adapter.
            // Converts the legacy MonsterTurnSchedule-based input into EnemyRuntime.
            // Remove this after Data/GameFlow creates EnemyRuntime directly.
            schedule.Reset();

            var plans = new EnemyActionPlan[schedule.TurnCount];
            for (int index = 0; index < schedule.TurnCount; index++)
            {
                plans[index] = new EnemyActionPlan(schedule.UpcomingActions);
                schedule.ConsumeUpcomingTurn();
            }

            schedule.Reset();

            return new EnemyRuntime(
                enemy,
                new FixedSequenceEnemyActionPlanner(plans));
        }
    }
}
