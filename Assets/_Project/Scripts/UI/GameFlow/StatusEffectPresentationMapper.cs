using System;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public enum StatusEffectDisplayGroup
    {
        Buff = 0,
        Debuff = 1,
    }

    public static class StatusEffectPresentationMapper
    {
        public static StatusEffectViewData Map(StatusEffectInstance status)
        {
            if (status == null)
            {
                throw new ArgumentNullException(nameof(status));
            }

            return Map(
                status.Kind,
                status.Magnitude,
                status.StackCount,
                status.RemainingTurns);
        }

        public static StatusEffectViewData Map(
            StatusEffectKind kind,
            int magnitude,
            int stackCount,
            int remainingTurns)
        {
            int displayValue;
            switch (kind)
            {
                case StatusEffectKind.Burn:
                case StatusEffectKind.Thorns:
                    displayValue = magnitude;
                    break;
                case StatusEffectKind.Infection:
                case StatusEffectKind.Vulnerable:
                case StatusEffectKind.Weaken:
                case StatusEffectKind.Lifesteal:
                    displayValue = stackCount;
                    break;
                case StatusEffectKind.Freeze:
                    displayValue = remainingTurns;
                    break;
                case StatusEffectKind.None:
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        "Status effect kind does not have a presentation mapping.");
            }

            return new StatusEffectViewData(
                kind,
                displayValue,
                showValue: displayValue > 0);
        }

        public static StatusEffectDisplayGroup GetDisplayGroup(StatusEffectKind kind)
        {
            switch (kind)
            {
                case StatusEffectKind.Lifesteal:
                case StatusEffectKind.Thorns:
                    return StatusEffectDisplayGroup.Buff;
                case StatusEffectKind.Burn:
                case StatusEffectKind.Freeze:
                case StatusEffectKind.Infection:
                case StatusEffectKind.Vulnerable:
                case StatusEffectKind.Weaken:
                    return StatusEffectDisplayGroup.Debuff;
                case StatusEffectKind.None:
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        "Status effect kind does not have a display group.");
            }
        }
    }
}
