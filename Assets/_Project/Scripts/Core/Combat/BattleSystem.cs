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
        private readonly List<EnemyCombatant> _enemyCombatants = new();
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
                    ApplyPlannedActions(enemyTurnActions, enemy, default))
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

        private bool ApplyPlannedActions(
            IReadOnlyList<EnemyPlannedAction> actions,
            CombatParticipant source,
            CombatParticipantId selectedTargetId)
        {
            if (actions == null || actions.Count == 0)
            {
                return false;
            }

            for (int actionIndex = 0; actionIndex < actions.Count; actionIndex++)
            {
                EnemyPlannedAction action = actions[actionIndex];
                if (action == null)
                {
                    continue;
                }

                IReadOnlyList<EnemyActionEffect> effects = action.Effects;
                CombatEffect completedEffect = default;
                for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                {
                    EnemyActionEffect actionEffect = effects[effectIndex];
                    if (actionEffect.Kind == EnemyActionEffectKind.LockSlot)
                    {
                        continue;
                    }

                    CombatEffect effect = actionEffect.CombatEffect;
                    completedEffect = effect;
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
                }

                _events.Add(new CombatEvent(
                    CombatEventKind.ActionCompleted,
                    CurrentPhase,
                    completedEffect,
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
