using System;

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

        public static StatusEffectSpec FromAmount(StatusEffectKind kind, int amount)
        {
            switch (kind)
            {
                case StatusEffectKind.Burn:
                    return new StatusEffectSpec(
                        kind,
                        duration: 1,
                        magnitude: amount,
                        StatusStackMode.Refresh);
                case StatusEffectKind.Freeze:
                    return new StatusEffectSpec(
                        kind,
                        duration: amount,
                        magnitude: 0,
                        StatusStackMode.Refresh);
                case StatusEffectKind.Infection:
                case StatusEffectKind.Vulnerable:
                case StatusEffectKind.Weaken:
                case StatusEffectKind.Lifesteal:
                    return new StatusEffectSpec(
                        kind,
                        duration: 0,
                        magnitude: amount,
                        StatusStackMode.Stack);
                case StatusEffectKind.Thorns:
                    return new StatusEffectSpec(
                        kind,
                        duration: 0,
                        magnitude: amount,
                        StatusStackMode.Refresh);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        "Unsupported status effect kind.");
            }
        }
    }
}
