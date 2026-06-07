using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class StatusEffectEngine
    {
        private readonly EffectApplicator _effectApplicator;
        private readonly StatusEffectFactory _factory;

        public StatusEffectEngine(EffectApplicator effectApplicator)
            : this(effectApplicator, new StatusEffectFactory())
        {
        }

        public StatusEffectEngine(EffectApplicator effectApplicator, StatusEffectFactory factory)
        {
            _effectApplicator = effectApplicator ?? new EffectApplicator();
            _factory = factory ?? new StatusEffectFactory();
        }

        public void ApplyStatus(
            StatusEffectSpec spec,
            CombatParticipant participant,
            BattlePhase phase,
            List<CombatEvent> events)
        {
            if (participant == null || !spec.IsValid)
            {
                return;
            }

            StatusEffectInstance incoming = _factory.Create(spec);
            if (incoming == null)
            {
                return;
            }

            StatusEffectInstance instance = Find(participant, spec.Kind);
            if (instance == null)
            {
                participant.MutableStatusEffects.Add(incoming);
                instance = incoming;
            }
            else if (spec.StackMode == StatusStackMode.Stack)
            {
                instance.StackCount += incoming.StackCount;
                instance.Magnitude = incoming.Magnitude;
            }
            else
            {
                instance.RemainingTurns = incoming.RemainingTurns;
                instance.Magnitude = incoming.Magnitude;
            }

            StatusEffectContext context = CreateContext(phase, participant, instance, events);
            InvokeApplied(instance, context);
            context.EmitApplied();
        }

        public void TickTurnStart(CombatParticipant participant, BattlePhase phase, List<CombatEvent> events)
        {
            if (participant == null)
            {
                return;
            }

            StatusEffectInstance[] instances = participant.MutableStatusEffects.ToArray();
            for (int index = 0; index < instances.Length; index++)
            {
                StatusEffectInstance instance = instances[index];
                StatusEffectContext context = CreateContext(phase, participant, instance, events);
                for (int componentIndex = 0; componentIndex < instance.Components.Count; componentIndex++)
                {
                    instance.Components[componentIndex].OnTurnStart(context);
                }
            }
        }

        public bool ShouldSkipAction(
            CombatParticipant participant,
            BattlePhase phase,
            List<CombatEvent> events)
        {
            if (participant == null)
            {
                return false;
            }

            StatusEffectInstance[] instances = participant.MutableStatusEffects.ToArray();
            for (int index = 0; index < instances.Length; index++)
            {
                StatusEffectInstance instance = instances[index];
                StatusEffectContext context = CreateContext(phase, participant, instance, events);
                for (int componentIndex = 0; componentIndex < instance.Components.Count; componentIndex++)
                {
                    if (instance.Components[componentIndex].ShouldSkipAction(context))
                    {
                        context.EmitActionSkipped();
                        return true;
                    }
                }
            }

            return false;
        }

        public void TickTurnEnd(CombatParticipant participant, BattlePhase phase, List<CombatEvent> events)
        {
            if (participant == null)
            {
                return;
            }

            StatusEffectInstance[] instances = participant.MutableStatusEffects.ToArray();
            for (int index = 0; index < instances.Length; index++)
            {
                StatusEffectInstance instance = instances[index];
                StatusEffectContext context = CreateContext(phase, participant, instance, events);
                for (int componentIndex = 0; componentIndex < instance.Components.Count; componentIndex++)
                {
                    instance.Components[componentIndex].OnTurnEnd(context);
                }

                if (instance.RemainingTurns == 0 && HasDurationComponent(instance))
                {
                    Expire(participant, instance, context);
                }
            }
        }

        private StatusEffectContext CreateContext(
            BattlePhase phase,
            CombatParticipant participant,
            StatusEffectInstance instance,
            List<CombatEvent> events)
        {
            return new StatusEffectContext(phase, participant, instance, _effectApplicator, events);
        }

        private static void InvokeApplied(StatusEffectInstance instance, StatusEffectContext context)
        {
            for (int index = 0; index < instance.Components.Count; index++)
            {
                instance.Components[index].OnApplied(context);
            }
        }

        private static StatusEffectInstance Find(CombatParticipant participant, StatusEffectKind kind)
        {
            for (int index = 0; index < participant.StatusEffects.Count; index++)
            {
                StatusEffectInstance instance = participant.StatusEffects[index];
                if (instance.Kind == kind)
                {
                    return instance;
                }
            }

            return null;
        }

        private static bool HasDurationComponent(StatusEffectInstance instance)
        {
            for (int index = 0; index < instance.Components.Count; index++)
            {
                if (instance.Components[index] is DurationComponent)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Expire(
            CombatParticipant participant,
            StatusEffectInstance instance,
            StatusEffectContext context)
        {
            for (int index = 0; index < instance.Components.Count; index++)
            {
                instance.Components[index].OnExpired(context);
            }

            participant.MutableStatusEffects.Remove(instance);
            context.EmitExpired();
        }
    }
}
