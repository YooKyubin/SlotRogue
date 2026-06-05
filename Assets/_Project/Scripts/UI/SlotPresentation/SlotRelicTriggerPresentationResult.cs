using UnityEngine;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class SlotRelicTriggerPresentationResult
    {
        public SlotRelicTriggerPresentationResult(
            string relicId,
            string relicName,
            Sprite icon,
            string description,
            string valueText)
        {
            RelicId = relicId ?? string.Empty;
            RelicName = relicName ?? string.Empty;
            Icon = icon;
            Description = description ?? string.Empty;
            ValueText = valueText ?? string.Empty;
        }

        public string RelicId { get; }

        public string RelicName { get; }

        public Sprite Icon { get; }

        public string Description { get; }

        public string ValueText { get; }
    }
}
