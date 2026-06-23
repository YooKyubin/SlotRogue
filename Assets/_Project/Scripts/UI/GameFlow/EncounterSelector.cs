using System;
using System.Collections.Generic;
using SlotRogue.Data.Combat;
using SlotRogue.Data.GameFlow;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EncounterSelector
    {
        public EncounterSelection Select(EncounterSelectionRequest request)
        {
            IReadOnlyList<EncounterDefinition> candidates = FilterCandidates(request);
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No encounter candidates found. Table='{request.Table.name}', " +
                    $"Tier={request.Tier}, ThemeGroupIndex={request.ThemeGroupIndex}, " +
                    $"BattleNumber={request.BattleNumber}.");
            }

            int totalWeight = CalculateTotalWeight(candidates);
            int roll = RollWeight(request.RunSeed, request.BattleNumber, totalWeight);
            EncounterDefinition selected = SelectWeightedCandidate(candidates, roll);

            IReadOnlyList<MonsterDefinition> monsters = selected.Monsters;
            IReadOnlyList<int> slots = EnemyFormationLayout.ResolveSlots(monsters.Count);
            var selectedMonsters = new SelectedEncounterMonster[monsters.Count];
            for (int i = 0; i < monsters.Count; i++)
            {
                selectedMonsters[i] = new SelectedEncounterMonster(monsters[i], slots[i]);
            }

            return new EncounterSelection(selectedMonsters);
        }

        private static IReadOnlyList<EncounterDefinition> FilterCandidates(EncounterSelectionRequest request)
        {
            var candidates = new List<EncounterDefinition>();
            IReadOnlyList<EncounterDefinition> encounters = request.Table.GetEncounters(request.ThemeGroupIndex);
            for (int index = 0; index < encounters.Count; index++)
            {
                EncounterDefinition encounter = encounters[index];
                if (encounter == null)
                {
                    continue;
                }

                if (encounter.Tier != request.Tier)
                {
                    continue;
                }

                candidates.Add(encounter);
            }

            return candidates;
        }

        private static int CalculateTotalWeight(IReadOnlyList<EncounterDefinition> candidates)
        {
            int totalWeight = 0;
            for (int index = 0; index < candidates.Count; index++)
            {
                totalWeight += candidates[index].Weight;
            }

            if (totalWeight <= 0)
            {
                throw new InvalidOperationException("Encounter candidate total weight must be greater than zero.");
            }

            return totalWeight;
        }

        private static int RollWeight(int runSeed, int battleNumber, int totalWeight)
        {
            if (totalWeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalWeight), totalWeight, "Total weight must be positive.");
            }

            uint seed = CombineSeed(runSeed, battleNumber);
            return (int)(seed % (uint)totalWeight);
        }

        private static EncounterDefinition SelectWeightedCandidate(
            IReadOnlyList<EncounterDefinition> candidates,
            int roll)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            int totalWeight = CalculateTotalWeight(candidates);
            if (roll < 0 || roll >= totalWeight)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(roll),
                    roll,
                    "Roll must be greater than or equal to zero and less than total weight.");
            }

            int cumulative = 0;
            for (int index = 0; index < candidates.Count; index++)
            {
                EncounterDefinition candidate = candidates[index];
                cumulative += candidate.Weight;
                if (roll < cumulative)
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Weighted encounter selection failed.");
        }

        private static uint CombineSeed(int runSeed, int battleNumber)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = Mix(hash, (uint)runSeed);
                hash = Mix(hash, (uint)battleNumber);
                hash ^= hash >> 16;
                hash *= 2246822519u;
                hash ^= hash >> 13;
                hash *= 3266489917u;
                hash ^= hash >> 16;
                return hash;
            }
        }

        private static uint Mix(uint hash, uint value)
        {
            unchecked
            {
                hash ^= value;
                hash *= 16777619u;
                return hash;
            }
        }
    }

    public sealed class EncounterThemeIndexSelector
    {
        public int Select(int runSeed, int themeSectionIndex, int themeGroupCount)
        {
            if (themeSectionIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(themeSectionIndex));
            }

            if (themeGroupCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(themeGroupCount));
            }

            if (themeGroupCount == 1)
            {
                return 0;
            }

            int selected = SelectRaw(runSeed, themeSectionIndex, themeGroupCount);
            if (themeSectionIndex == 0)
            {
                return selected;
            }

            int previous = Select(runSeed, themeSectionIndex - 1, themeGroupCount);
            if (selected == previous)
            {
                return (selected + 1) % themeGroupCount;
            }

            return selected;
        }

        private static int SelectRaw(int runSeed, int themeSectionIndex, int themeGroupCount)
        {
            uint seed = CombineSeed(runSeed, themeSectionIndex);
            return (int)(seed % (uint)themeGroupCount);
        }

        private static uint CombineSeed(int runSeed, int themeSectionIndex)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = Mix(hash, (uint)runSeed);
                hash = Mix(hash, (uint)themeSectionIndex);
                hash ^= hash >> 16;
                hash *= 2246822519u;
                hash ^= hash >> 13;
                hash *= 3266489917u;
                hash ^= hash >> 16;
                return hash;
            }
        }

        private static uint Mix(uint hash, uint value)
        {
            unchecked
            {
                hash ^= value;
                hash *= 16777619u;
                return hash;
            }
        }
    }
}
