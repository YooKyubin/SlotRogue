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

            StatusEffectInstance instance = participant.ApplyStatusEffect(incoming, spec.StackMode);

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

            StatusEffectInstance[] instances = participant.GetStatusEffectsSnapshot();
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

        public bool ShouldSkipAction(CombatParticipant participant, BattlePhase phase, List<CombatEvent> events)
        {
            if (participant == null)
            {
                return false;
            }

            StatusEffectInstance[] instances = participant.GetStatusEffectsSnapshot();
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

        internal IReadOnlyList<OutgoingDamageModifierSnapshot> CaptureOutgoingDamageModifiers(
            CombatParticipant source,
            BattlePhase phase,
            List<CombatEvent> events)
        {
            if (source == null)
            {
                return System.Array.Empty<OutgoingDamageModifierSnapshot>();
            }

            var modifiers = new List<OutgoingDamageModifierSnapshot>();
            StatusEffectInstance[] instances = source.GetStatusEffectsSnapshot();
            for (int index = 0; index < instances.Length; index++)
            {
                StatusEffectInstance instance = instances[index];
                StatusEffectContext context = CreateContext(phase, source, instance, events);
                for (int componentIndex = 0; componentIndex < instance.Components.Count; componentIndex++)
                {
                    if (instance.Components[componentIndex] is IOutgoingDamageModifier modifier)
                    {
                        modifiers.Add(new OutgoingDamageModifierSnapshot(
                            modifier,
                            CreateSnapshot(instance),
                            new DamageModifierUsage(context)));
                    }
                }
            }

            return modifiers;
        }

        internal IReadOnlyList<IncomingDamageModifierSnapshot> CaptureIncomingDamageModifiers(
            CombatParticipant target,
            BattlePhase phase,
            List<CombatEvent> events)
        {
            if (target == null)
            {
                return System.Array.Empty<IncomingDamageModifierSnapshot>();
            }

            var modifiers = new List<IncomingDamageModifierSnapshot>();
            StatusEffectInstance[] instances = target.GetStatusEffectsSnapshot();
            for (int index = 0; index < instances.Length; index++)
            {
                StatusEffectInstance instance = instances[index];
                StatusEffectContext context = CreateContext(phase, target, instance, events);
                for (int componentIndex = 0; componentIndex < instance.Components.Count; componentIndex++)
                {
                    if (instance.Components[componentIndex] is IIncomingDamageModifier modifier)
                    {
                        modifiers.Add(new IncomingDamageModifierSnapshot(
                            modifier,
                            CreateSnapshot(instance),
                            new DamageModifierUsage(context)));
                    }
                }
            }

            return modifiers;
        }

        internal IReadOnlyList<AfterHealthDamageReactionSnapshot> CaptureAfterHealthDamageReactions(
            CombatParticipant source,
            BattlePhase phase,
            List<CombatEvent> events)
        {
            if (source == null)
            {
                return System.Array.Empty<AfterHealthDamageReactionSnapshot>();
            }

            var reactions = new List<AfterHealthDamageReactionSnapshot>();
            StatusEffectInstance[] instances = source.GetStatusEffectsSnapshot();
            for (int index = 0; index < instances.Length; index++)
            {
                StatusEffectInstance instance = instances[index];
                StatusEffectContext context = CreateContext(phase, source, instance, events);
                for (int componentIndex = 0; componentIndex < instance.Components.Count; componentIndex++)
                {
                    if (instance.Components[componentIndex] is IAfterHealthDamageDealt reaction)
                    {
                        reactions.Add(new AfterHealthDamageReactionSnapshot(
                            reaction,
                            CreateSnapshot(instance),
                            new AfterHealthDamageUsage(context, events)));
                    }
                }
            }

            return reactions;
        }

        internal int ModifyOutgoingDamage(
            IReadOnlyList<OutgoingDamageModifierSnapshot> modifiers,
            CombatParticipant target,
            int damage,
            DamageOrigin origin,
            List<DamageModifierUsage> usedModifiers)
        {
            if (modifiers == null || damage <= 0 || origin != DamageOrigin.DirectAction)
            {
                return ClampDamage(damage);
            }

            int modifiedDamage = damage;
            for (int index = 0; index < modifiers.Count; index++)
            {
                OutgoingDamageModifierSnapshot snapshot = modifiers[index];
                var context = new DamageModifierContext(
                    origin,
                    snapshot.Usage.ParticipantId,
                    target.Id,
                    modifiedDamage,
                    snapshot.StatusSnapshot);
                modifiedDamage = ClampDamage(
                    snapshot.Modifier.ModifyDamage(modifiedDamage, in context));
                AddUsedModifier(usedModifiers, snapshot.Usage);
            }

            return modifiedDamage;
        }

        internal int ModifyIncomingDamage(
            IReadOnlyList<IncomingDamageModifierSnapshot> modifiers,
            CombatParticipant source,
            int damage,
            DamageOrigin origin,
            List<DamageModifierUsage> usedModifiers)
        {
            if (modifiers == null || damage <= 0 || origin != DamageOrigin.DirectAction)
            {
                return ClampDamage(damage);
            }

            int modifiedDamage = damage;
            for (int index = 0; index < modifiers.Count; index++)
            {
                IncomingDamageModifierSnapshot snapshot = modifiers[index];
                var context = new DamageModifierContext(
                    origin,
                    source.Id,
                    snapshot.Usage.ParticipantId,
                    modifiedDamage,
                    snapshot.StatusSnapshot);
                modifiedDamage = ClampDamage(snapshot.Modifier.ModifyDamage(modifiedDamage, in context));
                AddUsedModifier(usedModifiers, snapshot.Usage);
            }

            return modifiedDamage;
        }

        internal void ConsumeUsedDamageModifiers(IReadOnlyList<DamageModifierUsage> usedModifiers)
        {
            if (usedModifiers == null)
            {
                return;
            }

            for (int index = 0; index < usedModifiers.Count; index++)
            {
                DamageModifierUsage usage = usedModifiers[index];
                if (!usage.IsCurrent)
                {
                    continue;
                }

                IReadOnlyList<IStatusEffectComponent> components = usage.Context.Instance.Components;
                for (int componentIndex = 0; componentIndex < components.Count; componentIndex++)
                {
                    if (components[componentIndex] is IDamageModifierUsageHandler handler)
                    {
                        handler.OnDamageModifierUsed(usage.Context);
                    }
                }
            }
        }

        internal void ReactAfterHealthDamageDealt(
            IReadOnlyList<AfterHealthDamageReactionSnapshot> reactions,
            CombatParticipant target,
            int healthDamage,
            DamageOrigin damageOrigin,
            List<AfterHealthDamageUsage> usedReactions)
        {
            if (reactions == null ||
                healthDamage <= 0 ||
                damageOrigin != DamageOrigin.DirectAction)
            {
                return;
            }

            for (int index = 0; index < reactions.Count; index++)
            {
                AfterHealthDamageReactionSnapshot reaction = reactions[index];
                var context = new AfterHealthDamageContext(
                    reaction.Usage.Context.Phase,
                    reaction.Usage.Context.Participant,
                    target,
                    healthDamage,
                    damageOrigin,
                    reaction.StatusSnapshot,
                    _effectApplicator,
                    reaction.Usage.Events);

                if (reaction.Reaction.OnAfterHealthDamageDealt(context))
                {
                    AddUsedAfterHealthDamageReaction(usedReactions, reaction.Usage);
                }
            }
        }

        internal void ConsumeUsedAfterHealthDamageReactions(
            IReadOnlyList<AfterHealthDamageUsage> usedReactions)
        {
            if (usedReactions == null)
            {
                return;
            }

            for (int index = 0; index < usedReactions.Count; index++)
            {
                AfterHealthDamageUsage usage = usedReactions[index];
                if (!usage.IsCurrent)
                {
                    continue;
                }

                IReadOnlyList<IStatusEffectComponent> components = usage.Context.Instance.Components;
                for (int componentIndex = 0; componentIndex < components.Count; componentIndex++)
                {
                    if (components[componentIndex] is IAfterHealthDamageUsageHandler handler)
                    {
                        handler.OnAfterHealthDamageUsed(usage.Context);
                    }
                }
            }
        }

        public void TickTurnEnd(CombatParticipant participant, BattlePhase phase, List<CombatEvent> events)
        {
            if (participant == null)
            {
                return;
            }

            StatusEffectInstance[] instances = participant.GetStatusEffectsSnapshot();
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

        private static void Expire(CombatParticipant participant, StatusEffectInstance instance, StatusEffectContext context)
        {
            for (int index = 0; index < instance.Components.Count; index++)
            {
                instance.Components[index].OnExpired(context);
            }

            participant.RemoveStatusEffect(instance);
            context.EmitExpired();
        }

        private static int ClampDamage(int damage) => damage < 0 ? 0 : damage;

        private static StatusEffectSnapshot CreateSnapshot(StatusEffectInstance instance)
        {
            return new StatusEffectSnapshot(
                instance.Kind,
                instance.RemainingTurns,
                instance.Magnitude,
                instance.StackCount);
        }

        private static void AddUsedModifier(List<DamageModifierUsage> usedModifiers, DamageModifierUsage usage)
        {
            if (usedModifiers == null)
            {
                return;
            }

            for (int index = 0; index < usedModifiers.Count; index++)
            {
                if (ReferenceEquals(usedModifiers[index].Context.Instance, usage.Context.Instance))
                {
                    return;
                }
            }

            usedModifiers.Add(usage);
        }

        private static void AddUsedAfterHealthDamageReaction(
            List<AfterHealthDamageUsage> usedReactions,
            AfterHealthDamageUsage usage)
        {
            if (usedReactions == null)
            {
                return;
            }

            for (int index = 0; index < usedReactions.Count; index++)
            {
                if (ReferenceEquals(usedReactions[index].Context.Instance, usage.Context.Instance))
                {
                    return;
                }
            }

            usedReactions.Add(usage);
        }

        internal readonly struct OutgoingDamageModifierSnapshot
        {
            internal OutgoingDamageModifierSnapshot(
                IOutgoingDamageModifier modifier,
                StatusEffectSnapshot statusSnapshot,
                DamageModifierUsage usage)
            {
                Modifier = modifier;
                StatusSnapshot = statusSnapshot;
                Usage = usage;
            }

            internal IOutgoingDamageModifier Modifier { get; }

            internal StatusEffectSnapshot StatusSnapshot { get; }

            internal DamageModifierUsage Usage { get; }
        }

        internal readonly struct IncomingDamageModifierSnapshot
        {
            internal IncomingDamageModifierSnapshot(
                IIncomingDamageModifier modifier,
                StatusEffectSnapshot statusSnapshot,
                DamageModifierUsage usage)
            {
                Modifier = modifier;
                StatusSnapshot = statusSnapshot;
                Usage = usage;
            }

            internal IIncomingDamageModifier Modifier { get; }

            internal StatusEffectSnapshot StatusSnapshot { get; }

            internal DamageModifierUsage Usage { get; }
        }

        internal readonly struct AfterHealthDamageReactionSnapshot
        {
            internal AfterHealthDamageReactionSnapshot(
                IAfterHealthDamageDealt reaction,
                StatusEffectSnapshot statusSnapshot,
                AfterHealthDamageUsage usage)
            {
                Reaction = reaction;
                StatusSnapshot = statusSnapshot;
                Usage = usage;
            }

            internal IAfterHealthDamageDealt Reaction { get; }

            internal StatusEffectSnapshot StatusSnapshot { get; }

            internal AfterHealthDamageUsage Usage { get; }
        }

        internal readonly struct DamageModifierUsage
        {
            internal DamageModifierUsage(StatusEffectContext context)
            {
                Context = context;
                ParticipantId = context.Participant.Id;
                Revision = context.Instance.Revision;
            }

            internal StatusEffectContext Context { get; }

            internal CombatParticipantId ParticipantId { get; }

            internal int Revision { get; }

            internal bool IsCurrent
            {
                get
                {
                    if (Context.Instance.Revision != Revision)
                    {
                        return false;
                    }

                    StatusEffectInstance[] instances = Context.Participant.GetStatusEffectsSnapshot();
                    for (int index = 0; index < instances.Length; index++)
                    {
                        if (ReferenceEquals(instances[index], Context.Instance))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }

        internal readonly struct AfterHealthDamageUsage
        {
            internal AfterHealthDamageUsage(StatusEffectContext context, List<CombatEvent> events)
            {
                Context = context;
                Events = events;
                Revision = context.Instance.Revision;
            }

            internal StatusEffectContext Context { get; }

            internal List<CombatEvent> Events { get; }

            internal int Revision { get; }

            internal bool IsCurrent
            {
                get
                {
                    if (Context.Instance.Revision != Revision)
                    {
                        return false;
                    }

                    StatusEffectInstance[] instances = Context.Participant.GetStatusEffectsSnapshot();
                    for (int index = 0; index < instances.Length; index++)
                    {
                        if (ReferenceEquals(instances[index], Context.Instance))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }
    }
}
