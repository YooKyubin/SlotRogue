using System;
using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class BattleSystem
    {
        private readonly CombatActionResolver _actionResolver;
        private readonly StatusEffectEngine _statusEffectEngine;
        private readonly List<CombatEvent> _events = new();
        private readonly List<CombatParticipant> _enemies = new();
        private readonly List<EnemyCombatant> _enemyCombatants = new();
        private readonly Dictionary<int, CombatParticipant> _participantsById = new();
        private CombatParticipant _player = null!;

        public BattleSystem()
            : this(new EffectApplicator(), new SystemCombatRandom())
        {
        }

        public BattleSystem(EffectApplicator effectApplicator)
            : this(effectApplicator, new SystemCombatRandom())
        {
        }

        public BattleSystem(
            EffectApplicator effectApplicator,
            ICombatRandom combatRandom)
        {
            EffectApplicator applicator = effectApplicator ?? new EffectApplicator();
            _statusEffectEngine = new StatusEffectEngine(applicator);
            _actionResolver = new CombatActionResolver(
                applicator,
                _statusEffectEngine,
                combatRandom ?? throw new ArgumentNullException(nameof(combatRandom)));
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

            for (int index = 0; index < _enemyCombatants.Count; index++)
            {
                EnemyCombatant combatant = _enemyCombatants[index];
                CombatParticipant participant = combatant.Participant;

                if (participant.Id.Value != participantId.Value)
                {
                    continue;
                }

                if (participant.IsDead)
                {
                    upcomingTurn = default;
                    return false;
                }

                upcomingTurn = new EnemyUpcomingTurn(participant.Id, combatant.UpcomingPlan);
                return true;
            }

            upcomingTurn = default;
            return false;
        }

        public void StartBattle(CombatParticipant player, IReadOnlyList<EnemyCombatant> enemyCombatants)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (enemyCombatants == null || enemyCombatants.Count == 0)
            {
                throw new ArgumentException("At least one enemy combatant is required.", nameof(enemyCombatants));
            }

            _player = player;
            _enemies.Clear();
            _enemyCombatants.Clear();
            _participantsById.Clear();
            RegisterParticipant(_player);

            for (int index = 0; index < enemyCombatants.Count; index++)
            {
                EnemyCombatant combatant = enemyCombatants[index] ?? throw new ArgumentNullException(nameof(enemyCombatants));
                CombatParticipant enemy = combatant.Participant;
                _enemies.Add(enemy);
                _enemyCombatants.Add(combatant);
                RegisterParticipant(enemy);
            }

            EndReason = BattleEndReason.None;
            _events.Clear();

            PlanNextActionsForAllEnemies();
            SetPhase(BattlePhase.PlayerTurn);
        }

        public bool TryRevivePlayer(int currentHp)
        {
            if (CurrentPhase != BattlePhase.Ended ||
                EndReason != BattleEndReason.Defeat ||
                _player == null ||
                AreAllEnemiesDefeated() ||
                !_player.TryRevive(currentHp))
            {
                return false;
            }

            EndReason = BattleEndReason.None;
            _events.Clear();
            SetPhase(BattlePhase.PlayerTurn);
            return true;
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
                ApplyPlayerEffects(playerEffects ?? Array.Empty<CombatEffect>(), selectedTargetId))
            {
                return AcceptedResult();
            }

            if (RunBattleStep(() => RunParticipantTurnEnd(_player)))
            {
                return AcceptedResult();
            }

            if (RunBattleStep(() => NotifyTeamTurnEnded(CombatTeam.Player)))
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

            for (int index = 0; index < _enemyCombatants.Count; index++)
            {
                EnemyCombatant enemyCombatant = _enemyCombatants[index];
                CombatParticipant enemy = enemyCombatant.Participant;
                if (enemy.IsDead)
                {
                    continue;
                }

                if (RunBattleStep(() => RunParticipantTurnStart(enemy)))
                {
                    return;
                }

                bool shouldSkipAction = TrySkipParticipantAction(enemy);
                IReadOnlyList<EnemyPlannedAction> enemyTurnActions = enemyCombatant.UpcomingPlan.Actions;
                enemyCombatant.PlanNextAction(CreateEnemyActionContext(enemy));
                if (!shouldSkipAction &&
                    ApplyEnemyPlannedActions(enemyTurnActions, enemy, default))
                {
                    return;
                }

                if (RunBattleStep(() => RunParticipantTurnEnd(enemy)))
                {
                    return;
                }
            }

            if (RunBattleStep(() => NotifyTeamTurnEnded(CombatTeam.Enemy)))
            {
                return;
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

        private bool ApplyPlayerEffects(
            IReadOnlyList<CombatEffect> effects,
            CombatParticipantId selectedTargetId)
        {
            bool actionReachedBattleEnd = _actionResolver.ResolvePlayerEffects(
                effects,
                _player,
                _player,
                _enemies,
                _participantsById,
                selectedTargetId,
                CurrentPhase,
                _events,
                IsBattleEndConditionMet);

            if (!actionReachedBattleEnd)
            {
                return false;
            }

            TryEndBattle();
            return true;
        }

        private bool ApplyEnemyPlannedActions(
            IReadOnlyList<EnemyPlannedAction> actions,
            CombatParticipant source,
            CombatParticipantId selectedTargetId)
        {
            bool actionReachedBattleEnd = _actionResolver.ResolveEnemyPlannedActions(
                actions,
                source,
                _player,
                _enemies,
                _participantsById,
                selectedTargetId,
                CurrentPhase,
                _events,
                IsBattleEndConditionMet);

            if (!actionReachedBattleEnd)
            {
                return false;
            }

            TryEndBattle();
            return true;
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

        private void NotifyTeamTurnEnded(CombatTeam endedTeam)
        {
            _statusEffectEngine.NotifyTeamTurnEnded(
                endedTeam,
                _player,
                CurrentPhase,
                _events);

            for (int index = 0; index < _enemies.Count; index++)
            {
                _statusEffectEngine.NotifyTeamTurnEnded(
                    endedTeam,
                    _enemies[index],
                    CurrentPhase,
                    _events);
            }
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

        private bool IsBattleEndConditionMet() => IsPlayerTeamDefeated() || AreAllEnemiesDefeated();

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
            for (int index = 0; index < _enemyCombatants.Count; index++)
            {
                EnemyCombatant combatant = _enemyCombatants[index];
                combatant.PlanNextAction(CreateEnemyActionContext(combatant.Participant));
            }
        }

        private EnemyActionContext CreateEnemyActionContext(CombatParticipant enemy)
        {
            return new(enemy, _player, _enemies, turnNumber: 0);
        }
    }
}
