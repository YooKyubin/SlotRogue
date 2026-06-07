namespace SlotRogue.Core.Combat
{
    using System.Collections.Generic;

    public sealed class CombatParticipant
    {
        private readonly List<StatusEffectInstance> _statusEffects = new();

        public CombatParticipant(int maxHp, int currentHp, int shield, CombatParticipantId id, CombatTeam team)
        {
            MaxHp = maxHp;
            CurrentHp = currentHp < 0 ? maxHp : currentHp;
            Shield = shield;
            Id = id;
            Team = team;
        }

        public CombatParticipantId Id { get; }

        public CombatTeam Team { get; }

        public int MaxHp { get; }

        public int CurrentHp { get; internal set; }

        public int Shield { get; internal set; }

        public bool IsDead => CurrentHp <= 0;

        public IReadOnlyList<StatusEffectInstance> StatusEffects => _statusEffects;

        internal List<StatusEffectInstance> MutableStatusEffects => _statusEffects;
    }
}
