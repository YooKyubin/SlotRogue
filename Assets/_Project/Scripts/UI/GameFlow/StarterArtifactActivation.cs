namespace SlotRogue.UI.GameFlow
{
    public sealed class StarterArtifactActivation
    {
        public static readonly StarterArtifactActivation None = new(false, "None", string.Empty);

        public StarterArtifactActivation(bool activated, string artifactName, string description)
        {
            Activated = activated;
            ArtifactName = artifactName;
            Description = description;
        }

        public bool Activated { get; }

        public string ArtifactName { get; }

        public string Description { get; }
    }
}
