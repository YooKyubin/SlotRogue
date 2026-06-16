namespace SlotRogue.Core.Combat
{
    using System;
    using System.Collections.Generic;

    public sealed class CombatParticipant
    {
        private readonly List<StatusEffectInstance> _statusEffects = new();

        public CombatParticipant(int maxHp, int currentHp, int shield, CombatParticipantId id, CombatTeam team)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Participant id must be valid.", nameof(id));
            }

            if (team == default)
            {
                throw new ArgumentException("Participant team must be valid.", nameof(team));
            }

            MaxHp = maxHp;
            CurrentHp = currentHp < 0 ? maxHp : currentHp;
            Shield = shield;
            Id = id;
            Team = team;
        }

        public CombatParticipantId Id { get; }

        public CombatTeam Team { get; }

        public int MaxHp { get; }

        public int CurrentHp { get; private set; }

        public int Shield { get; private set; }

        public bool IsDead => CurrentHp <= 0;

        public IReadOnlyList<StatusEffectInstance> StatusEffects => _statusEffects;

        internal EffectApplyResult ApplyDamage(int amount)
        {
            int shieldConsumed = Math.Min(Shield, amount);
            int damageDealt = amount - shieldConsumed;

            Shield -= shieldConsumed;
            CurrentHp = Math.Max(0, CurrentHp - damageDealt);

            return new EffectApplyResult(damageDealt, shieldConsumed, 0, 0);
        }

        internal EffectApplyResult GainShield(int amount)
        {
            Shield += amount;
            return new EffectApplyResult(0, 0, amount, 0);
        }

        internal EffectApplyResult Heal(int amount)
        {
            int healApplied = Math.Min(amount, MaxHp - CurrentHp);
            CurrentHp += healApplied;

            return new EffectApplyResult(0, 0, 0, healApplied);
        }

        internal bool TryRevive(int currentHp)
        {
            if (!IsDead || currentHp <= 0)
            {
                return false;
            }

            CurrentHp = Math.Min(currentHp, MaxHp);
            return true;
        }

        internal void ResetShield()
        {
            Shield = 0;
        }

        internal CombatParticipantSnapshot CaptureSnapshot()
        {
            return new CombatParticipantSnapshot(CurrentHp, Shield);
        }

        internal StatusEffectInstance[] GetStatusEffectsSnapshot()
        {
            return _statusEffects.ToArray();
        }

        internal StatusEffectInstance ApplyStatusEffect(StatusEffectInstance incoming, StatusStackMode stackMode)
        {
            if (!TryGetStatusEffect(incoming.Kind, out StatusEffectInstance instance))
            {
                _statusEffects.Add(incoming);
                return incoming;
            }

            if (stackMode == StatusStackMode.Stack)
            {
                instance.StackCount += incoming.StackCount;
                instance.Magnitude = incoming.Magnitude;
                return instance;
            }

            instance.RemainingTurns = incoming.RemainingTurns;
            instance.Magnitude = incoming.Magnitude;
            return instance;
        }

        internal void RemoveStatusEffect(StatusEffectInstance instance)
        {
            _statusEffects.Remove(instance);
        }

        internal bool TryGetStatusEffect(StatusEffectKind kind, out StatusEffectInstance instance)
        {
            for (int index = 0; index < _statusEffects.Count; index++)
            {
                StatusEffectInstance candidate = _statusEffects[index];
                if (candidate.Kind == kind)
                {
                    instance = candidate;
                    return true;
                }
            }

            instance = null;
            return false;
        }
    }
}
