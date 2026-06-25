using System;
using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class CombatActionResolver
    {
        private readonly EffectApplicator _effectApplicator;
        private readonly StatusEffectEngine _statusEffectEngine;
        private readonly ICombatRandom _combatRandom;

        public CombatActionResolver(
            EffectApplicator effectApplicator,
            StatusEffectEngine statusEffectEngine)
            : this(effectApplicator, statusEffectEngine, new SystemCombatRandom())
        {
        }

        public CombatActionResolver(
            EffectApplicator effectApplicator,
            StatusEffectEngine statusEffectEngine,
            ICombatRandom combatRandom)
        {
            _effectApplicator = effectApplicator ?? throw new ArgumentNullException(nameof(effectApplicator));
            _statusEffectEngine = statusEffectEngine ?? throw new ArgumentNullException(nameof(statusEffectEngine));
            _combatRandom = combatRandom ?? throw new ArgumentNullException(nameof(combatRandom));
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
            ActionExecutionState actionState = CreateActionState(source, player, enemies, phase, events);
            bool shouldStopAndEndBattle = false;

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
                    ApplyEffectAndRecordEvent(effect, source, targets[targetIndex], phase, events, actionState);

                    if (shouldEndBattle())
                    {
                        shouldStopAndEndBattle = true;
                        break;
                    }
                }

                if (shouldStopAndEndBattle)
                {
                    break;
                }

                events.Add(new CombatEvent(
                    CombatEventKind.ActionCompleted,
                    phase,
                    effect,
                    sourceParticipantId: source.Id));
            }

            actionState.Complete();
            return shouldStopAndEndBattle;
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

                ActionExecutionState actionState = CreateActionState(source, player, enemies, phase, events);
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
                        ApplyEffectAndRecordEvent(effect, source, targets[targetIndex], phase, events, actionState);

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

                actionState.Complete();
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
            List<CombatEvent> events,
            ActionExecutionState actionState)
        {
            CombatParticipantSnapshot targetBefore = target.CaptureSnapshot();
            EffectApplyResult applyResult = ApplyEffectToTarget(
                effect,
                target,
                phase,
                events,
                actionState,
                out CombatEffect appliedEffect,
                out AppliedStatusModifier[] appliedStatusModifiers);
            CombatParticipantSnapshot targetAfter = target.CaptureSnapshot();

            events.Add(new CombatEvent(
                CombatEventKind.EffectApplied,
                phase,
                appliedEffect,
                applyResult,
                isPlayerParticipant: target.Team == CombatTeam.Player,
                targetParticipantId: target.Id,
                targetBefore: targetBefore,
                targetAfter: targetAfter,
                sourceParticipantId: source.Id,
                appliedStatusModifiers: appliedStatusModifiers));

            if (appliedEffect.Kind == CombatEffectKind.Damage)
            {
                int healthDamage = targetBefore.Hp - targetAfter.Hp;
                actionState.ReactAfterHealthDamageDealt(
                    target,
                    healthDamage,
                    appliedEffect.DamageOrigin);
                actionState.ReactAfterDirectDamageReceived(
                    target,
                    applyResult,
                    appliedEffect.DamageOrigin);
            }
        }

        private EffectApplyResult ApplyEffectToTarget(
            CombatEffect effect,
            CombatParticipant target,
            BattlePhase phase,
            List<CombatEvent> events,
            ActionExecutionState actionState,
            out CombatEffect appliedEffect,
            out AppliedStatusModifier[] appliedStatusModifiers)
        {
            appliedEffect = effect;
            appliedStatusModifiers = Array.Empty<AppliedStatusModifier>();

            if (effect.Kind == CombatEffectKind.ApplyStatus)
            {
                _statusEffectEngine.ApplyStatus(effect.StatusEffect, target, phase, events);
                return EffectApplyResult.None;
            }

            if (effect.Kind == CombatEffectKind.Damage)
            {
                appliedEffect = actionState.ApplyDamageModifiers(
                    effect,
                    target,
                    out appliedStatusModifiers);
            }

            return _effectApplicator.ApplyToParticipant(appliedEffect, target);
        }

        private ActionExecutionState CreateActionState(
            CombatParticipant source,
            CombatParticipant player,
            IReadOnlyList<CombatParticipant> enemies,
            BattlePhase phase,
            List<CombatEvent> events)
        {
            return new ActionExecutionState(
                _statusEffectEngine,
                _combatRandom,
                source,
                player,
                enemies,
                phase,
                events);
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

        private sealed class ActionExecutionState
        {
            private readonly StatusEffectEngine _statusEffectEngine;
            private readonly ICombatRandom _combatRandom;
            private readonly CombatParticipant _source;
            private readonly IReadOnlyList<StatusEffectEngine.OutgoingDamageModifierSnapshot> _outgoingDamageModifiers;
            private readonly IReadOnlyList<StatusEffectEngine.AfterHealthDamageReactionSnapshot> _afterHealthDamageReactions;
            private readonly Dictionary<int, IReadOnlyList<StatusEffectEngine.IncomingDamageModifierSnapshot>> _incomingDamageModifiersByParticipantId = new();
            private readonly Dictionary<int, IReadOnlyList<StatusEffectEngine.DirectDamageReceivedReactionSnapshot>> _directDamageReceivedReactionsByParticipantId = new();
            private readonly List<StatusEffectEngine.DamageModifierUsage> _usedDamageModifiers = new();
            private readonly List<StatusEffectEngine.AfterHealthDamageUsage> _usedAfterHealthDamageReactions = new();
            private bool _isCompleted;

            internal ActionExecutionState(
                StatusEffectEngine statusEffectEngine,
                ICombatRandom combatRandom,
                CombatParticipant source,
                CombatParticipant player,
                IReadOnlyList<CombatParticipant> enemies,
                BattlePhase phase,
                List<CombatEvent> events)
            {
                _statusEffectEngine = statusEffectEngine;
                _combatRandom = combatRandom;
                _source = source;
                _outgoingDamageModifiers = statusEffectEngine.CaptureOutgoingDamageModifiers(source, phase, events);
                _afterHealthDamageReactions = statusEffectEngine.CaptureAfterHealthDamageReactions(source, phase, events);
                CaptureIncomingDamageModifiers(player, phase, events);
                CaptureDirectDamageReceivedReactions(player, phase, events);

                for (int index = 0; index < enemies.Count; index++)
                {
                    CaptureIncomingDamageModifiers(enemies[index], phase, events);
                    CaptureDirectDamageReceivedReactions(enemies[index], phase, events);
                }
            }

            internal CombatEffect ApplyDamageModifiers(
                CombatEffect effect,
                CombatParticipant target,
                out AppliedStatusModifier[] appliedStatusModifiers)
            {
                if (effect.DamageOrigin != DamageOrigin.DirectAction)
                {
                    appliedStatusModifiers = Array.Empty<AppliedStatusModifier>();
                    return effect;
                }

                int usageStartIndex = _usedDamageModifiers.Count;
                int modifiedDamage = _statusEffectEngine.ModifyOutgoingDamage(
                    _outgoingDamageModifiers,
                    target,
                    effect.Amount,
                    effect.DamageOrigin,
                    _usedDamageModifiers);

                IReadOnlyList<StatusEffectEngine.IncomingDamageModifierSnapshot> incomingModifiers =
                    _incomingDamageModifiersByParticipantId.TryGetValue(
                        target.Id.Value,
                        out IReadOnlyList<StatusEffectEngine.IncomingDamageModifierSnapshot> modifiers)
                        ? modifiers
                        : Array.Empty<StatusEffectEngine.IncomingDamageModifierSnapshot>();

                modifiedDamage = _statusEffectEngine.ModifyIncomingDamage(
                    incomingModifiers,
                    _source,
                    modifiedDamage,
                    effect.DamageOrigin,
                    _usedDamageModifiers);

                appliedStatusModifiers = BuildAppliedStatusModifiers(usageStartIndex);
                return new CombatEffect(
                    effect.Kind,
                    modifiedDamage,
                    effect.Target,
                    effect.StatusEffect,
                    effect.DamageOrigin);
            }

            private AppliedStatusModifier[] BuildAppliedStatusModifiers(int startIndex)
            {
                int count = _usedDamageModifiers.Count - startIndex;
                if (count <= 0)
                {
                    return Array.Empty<AppliedStatusModifier>();
                }

                var modifiers = new AppliedStatusModifier[count];
                for (int index = 0; index < count; index++)
                {
                    StatusEffectEngine.DamageModifierUsage usage =
                        _usedDamageModifiers[startIndex + index];
                    modifiers[index] = new AppliedStatusModifier(
                        usage.Context.Participant.Id,
                        usage.Context.Instance.Kind,
                        usage.Context.Participant.Team);
                }

                return modifiers;
            }

            internal void ReactAfterHealthDamageDealt(
                CombatParticipant target,
                int healthDamage,
                DamageOrigin damageOrigin)
            {
                _statusEffectEngine.ReactAfterHealthDamageDealt(
                    _afterHealthDamageReactions,
                    target,
                    healthDamage,
                    damageOrigin,
                    _usedAfterHealthDamageReactions);
            }

            internal void ReactAfterDirectDamageReceived(
                CombatParticipant target,
                EffectApplyResult applyResult,
                DamageOrigin damageOrigin)
            {
                IReadOnlyList<StatusEffectEngine.DirectDamageReceivedReactionSnapshot> reactions =
                    _directDamageReceivedReactionsByParticipantId.TryGetValue(
                        target.Id.Value,
                        out IReadOnlyList<StatusEffectEngine.DirectDamageReceivedReactionSnapshot> snapshots)
                        ? snapshots
                        : Array.Empty<StatusEffectEngine.DirectDamageReceivedReactionSnapshot>();

                _statusEffectEngine.ReactAfterDirectDamageReceived(
                    reactions,
                    _source,
                    target,
                    applyResult,
                    damageOrigin,
                    _combatRandom);
            }

            internal void Complete()
            {
                if (_isCompleted)
                {
                    return;
                }

                _statusEffectEngine.ConsumeUsedDamageModifiers(_usedDamageModifiers);
                _statusEffectEngine.ConsumeUsedAfterHealthDamageReactions(_usedAfterHealthDamageReactions);
                _isCompleted = true;
            }

            private void CaptureIncomingDamageModifiers(
                CombatParticipant participant,
                BattlePhase phase,
                List<CombatEvent> events)
            {
                if (participant == null)
                {
                    return;
                }

                _incomingDamageModifiersByParticipantId[participant.Id.Value] =
                    _statusEffectEngine.CaptureIncomingDamageModifiers(participant, phase, events);
            }

            private void CaptureDirectDamageReceivedReactions(
                CombatParticipant participant,
                BattlePhase phase,
                List<CombatEvent> events)
            {
                if (participant == null)
                {
                    return;
                }

                _directDamageReceivedReactionsByParticipantId[participant.Id.Value] =
                    _statusEffectEngine.CaptureDirectDamageReceivedReactions(
                        participant,
                        phase,
                        events);
            }
        }

    }
}
