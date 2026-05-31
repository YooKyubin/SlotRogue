using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    public static class StarterArtifactCatalog
    {
        private static readonly StarterArtifactDefinition NoneDefinition = new(
            StarterArtifactId.None,
            "None",
            "No starter artifact selected.",
            SlotSymbolType.Sword,
            int.MaxValue,
            0,
            0,
            0);

        private static readonly StarterArtifactDefinition[] AllDefinitions =
        {
            new(
                StarterArtifactId.BeginnerBlade,
                "Beginner Blade",
                "Sword matches of 3 or more gain +5 damage.",
                SlotSymbolType.Sword,
                3,
                5,
                0,
                0),
            new(
                StarterArtifactId.FirstAidCharm,
                "First Aid Charm",
                "Heart matches of 3 or more gain +4 heal.",
                SlotSymbolType.Heart,
                3,
                0,
                0,
                4),
            new(
                StarterArtifactId.GuardMedal,
                "Guard Medal",
                "Shield matches of 3 or more gain +6 defense.",
                SlotSymbolType.Shield,
                3,
                0,
                6,
                0),
        };

        public static IReadOnlyList<StarterArtifactDefinition> All => AllDefinitions;

        public static StarterArtifactDefinition Get(StarterArtifactId id)
        {
            for (int index = 0; index < AllDefinitions.Length; index++)
            {
                if (AllDefinitions[index].Id == id)
                {
                    return AllDefinitions[index];
                }
            }

            return NoneDefinition;
        }
    }
}
