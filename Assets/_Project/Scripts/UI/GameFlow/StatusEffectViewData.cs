using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public readonly struct StatusEffectViewData
    {
        public StatusEffectViewData(
            StatusEffectKind kind,
            int displayValue,
            bool showValue)
        {
            Kind = kind;
            DisplayValue = displayValue;
            ShowValue = showValue;
        }

        public StatusEffectKind Kind { get; }

        public int DisplayValue { get; }

        public bool ShowValue { get; }
    }
}
