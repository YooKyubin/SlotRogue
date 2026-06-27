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
            string valueText,
            int triggerPatternIndex = -1,
            int damagePerHit = 0,
            int block = 0,
            int heal = 0)
        {
            RelicId = relicId ?? string.Empty;
            RelicName = relicName ?? string.Empty;
            Icon = icon;
            Description = description ?? string.Empty;
            ValueText = valueText ?? string.Empty;
            TriggerPatternIndex = triggerPatternIndex;
            DamagePerHit = Mathf.Max(0, damagePerHit);
            Block = Mathf.Max(0, block);
            Heal = Mathf.Max(0, heal);
        }

        public string RelicId { get; }

        public string RelicName { get; }

        public Sprite Icon { get; }

        public string Description { get; }

        public string ValueText { get; }

        public int TriggerPatternIndex { get; }

        public int DamagePerHit { get; }

        public int Block { get; }

        public int Heal { get; }
    }
}
