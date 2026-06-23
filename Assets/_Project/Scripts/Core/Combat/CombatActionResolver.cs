using System;
using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class CombatActionResolver
    {
        private readonly EffectApplicator _effectApplicator;
        private readonly StatusEffectEngine _statusEffectEngine;

        public CombatActionResolver(
            EffectApplicator effectApplicator,
            StatusEffectEngine statusEffectEngine)
        {
            _effectApplicator = effectApplicator ?? throw new ArgumentNullException(nameof(effectApplicator));
            _statusEffectEngine = statusEffectEngine ?? throw new ArgumentNullException(nameof(statusEffectEngine));
        }

        public bool ResolvePlayerEffects(
            IReadOnlyList<CombatEffect> effects,
            CombatParticipant source,
            CombatParticipant player,
            IReadOnlyList<CombatParticipant> enemies,
            IReadOnlyDictionary<int, CombatParticipant> participantsById,
            CombatParticipantId selectedTargetId,
            BattlePhase phase,
            List<CombatEvent> events,
            Func<bool> shouldEndBattle)
        {
            effects ??= Array.Empty<CombatEffect>();

            for (int index = 0; index < effects.Count; index++)
            {
                CombatEffect effect = effects[index];
                IReadOnlyList<CombatParticipant> targets = ResolveTargets(
                    effect,
                    source,
                    player,
                    enemies,
                    participantsById,
                    selectedTargetId);

                for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    ApplyEffectAndRecordEvent(effect, source, targets[targetIndex], phase, events);

                    if (shouldEndBattle())
                    {
                        return true;
                    }
                }

                events.Add(new CombatEvent(
                    CombatEventKind.ActionCompleted,
                    phase,
                    effect,
                    sourceParticipantId: source.Id));
            }

            return false;
        }

        public bool ResolveEnemyPlannedActions(
            IReadOnlyList<EnemyPlannedAction> actions,
            CombatParticipant source,
            CombatParticipant player,
            IReadOnlyList<CombatParticipant> enemies,
            IReadOnlyDictionary<int, CombatParticipant> participantsById,
            CombatParticipantId selectedTargetId,
            BattlePhase phase,
            List<CombatEvent> events,
            Func<bool> shouldEndBattle)
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

                events.Add(new CombatEvent(
                    CombatEventKind.ActionStarted,
                    phase,
                    sourceParticipantId: source.Id,
                    actionName: action.ActionName));

                CombatEffect completedEffect = default;
                bool shouldCompleteAndEndBattle = false;
                IReadOnlyList<EnemyActionEffect> actionEffects = action.Effects;
                for (int effectIndex = 0; effectIndex < actionEffects.Count; effectIndex++)
                {
                    EnemyActionEffect actionEffect = actionEffects[effectIndex];
                    if (actionEffect.Kind == EnemyActionEffectKind.LockSlot)
                    {
                        continue;
                    }

                    CombatEffect effect = actionEffect.CombatEffect;
                    completedEffect = effect;
                    IReadOnlyList<CombatParticipant> targets = ResolveTargets(
                        effect,
                        source,
                        player,
                        enemies,
                        participantsById,
                        selectedTargetId);

                    for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                    {
                        ApplyEffectAndRecordEvent(effect, source, targets[targetIndex], phase, events);

                        if (shouldEndBattle())
                        {
                            shouldCompleteAndEndBattle = true;
                            break;
                        }
                    }

                    if (shouldCompleteAndEndBattle)
                    {
                        break;
                    }
                }

                events.Add(new CombatEvent(
                    CombatEventKind.ActionCompleted,
                    phase,
                    completedEffect,
                    sourceParticipantId: source.Id,
                    actionName: action.ActionName));

                if (shouldCompleteAndEndBattle)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyEffectAndRecordEvent(
            CombatEffect effect,
            CombatParticipant source,
            CombatParticipant target,
            BattlePhase phase,
            List<CombatEvent> events)
        {
            CombatParticipantSnapshot targetBefore = target.CaptureSnapshot();
            EffectApplyResult applyResult = ApplyEffectToTarget(effect, target, phase, events);
            CombatParticipantSnapshot targetAfter = target.CaptureSnapshot();

            events.Add(new CombatEvent(
                CombatEventKind.EffectApplied,
                phase,
                effect,
                applyResult,
                isPlayerParticipant: target.Team == CombatTeam.Player,
                targetParticipantId: target.Id,
                targetBefore: targetBefore,
                targetAfter: targetAfter,
                sourceParticipantId: source.Id));
        }

        private EffectApplyResult ApplyEffectToTarget(
            CombatEffect effect,
            CombatParticipant target,
            BattlePhase phase,
            List<CombatEvent> events)
        {
            if (effect.Kind == CombatEffectKind.ApplyStatus)
            {
                _statusEffectEngine.ApplyStatus(effect.StatusEffect, target, phase, events);
                return EffectApplyResult.None;
            }

            return _effectApplicator.ApplyToParticipant(effect, target);
        }

        private static IReadOnlyList<CombatParticipant> ResolveTargets(
            CombatEffect effect,
            CombatParticipant source,
            CombatParticipant player,
            IReadOnlyList<CombatParticipant> enemies,
            IReadOnlyDictionary<int, CombatParticipant> participantsById,
            CombatParticipantId selectedTargetId)
        {
            if (effect.Target.Mode == CombatTargetMode.Self)
            {
                return new[] { source };
            }

            CombatTeam targetTeam = source.Team == CombatTeam.Player ? CombatTeam.Enemy : CombatTeam.Player;
            List<CombatParticipant> aliveTargets = GetAliveParticipantsByTeam(player, enemies, targetTeam);
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
                participantsById.TryGetValue(requestedId.Value, out CombatParticipant explicitTarget) &&
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

        private static List<CombatParticipant> GetAliveParticipantsByTeam(
            CombatParticipant player,
            IReadOnlyList<CombatParticipant> enemies,
            CombatTeam team)
        {
            if (team == CombatTeam.Player)
            {
                return player.IsDead ? new List<CombatParticipant>() : new List<CombatParticipant> { player };
            }

            var aliveEnemies = new List<CombatParticipant>();
            for (int index = 0; index < enemies.Count; index++)
            {
                if (!enemies[index].IsDead)
                {
                    aliveEnemies.Add(enemies[index]);
                }
            }

            return aliveEnemies;
        }

    }
}
