namespace SlotRogue.UI.Combat.Presentation
{
    public readonly struct PresentationContext
    {
        public PresentationContext(bool isCritical, string patternName)
        {
            IsCritical = isCritical;
            PatternName = patternName ?? string.Empty;
        }

        public bool IsCritical { get; }

        public string PatternName { get; }
    }
}
