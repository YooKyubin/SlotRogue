namespace SlotRogue.Core.Combat
{
    public readonly struct StatusEffectSpec
    {
        public static readonly StatusEffectSpec None = new(StatusEffectKind.None, 0, 0, StatusStackMode.Refresh);

        public StatusEffectSpec(
            StatusEffectKind kind,
            int duration,
            int magnitude,
            StatusStackMode stackMode)
        {
            Kind = kind;
            Duration = duration;
            Magnitude = magnitude;
            StackMode = stackMode;
        }

        public StatusEffectKind Kind { get; }

        public int Duration { get; }

        public int Magnitude { get; }

        public StatusStackMode StackMode { get; }

        public bool IsValid => Kind != StatusEffectKind.None;
    }
}
