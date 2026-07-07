using System;
using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Core
{
    public sealed class SlotPatternResolver
    {
        public IReadOnlyList<SlotPatternMatch> ResolveAll(
            SlotSpinResult spinResult,
            int jackpotRepeatIndex = 0)
        {
            if (spinResult == null)
            {
                return Array.Empty<SlotPatternMatch>();
            }

            List<PatternCandidate> candidates = SlotPatternCatalog.GenerateCandidates(spinResult);
            PatternCandidate jackpotCandidate = null;
            var nonJackpotCandidates = new List<PatternCandidate>(candidates.Count);

            foreach (PatternCandidate candidate in candidates)
            {
                if (candidate.Definition.IsJackpot)
                {
                    jackpotCandidate = candidate;
                }
                else
                {
                    nonJackpotCandidates.Add(candidate);
                }
            }

            nonJackpotCandidates.Sort((left, right) =>
            {
                int orderCompare = right.Definition.OrderIndex.CompareTo(left.Definition.OrderIndex);
                return orderCompare != 0 ? orderCompare : left.SortKey.CompareTo(right.SortKey);
            });

            var firedCandidates = new List<PatternCandidate>();

            foreach (PatternCandidate candidate in nonJackpotCandidates)
            {
                if (candidate.Suppressed)
                {
                    continue;
                }

                if (IsSuppressedByFired(candidate, firedCandidates))
                {
                    candidate.Suppressed = true;
                    continue;
                }

                firedCandidates.Add(candidate);
                SuppressLower(candidate, nonJackpotCandidates);
            }

            firedCandidates.Sort((left, right) => left.SortKey.CompareTo(right.SortKey));

            var result = new List<SlotPatternMatch>(firedCandidates.Count + 1);

            foreach (PatternCandidate candidate in firedCandidates)
            {
                result.Add(ToMatch(candidate, 0));
            }

            if (jackpotCandidate != null)
            {
                result.Add(ToMatch(jackpotCandidate, jackpotRepeatIndex));
            }

            return result;
        }

        private static bool IsSuppressedByFired(
            PatternCandidate candidate,
            List<PatternCandidate> firedCandidates)
        {
            foreach (PatternCandidate fired in firedCandidates)
            {
                if (fired.Definition.OrderIndex <= candidate.Definition.OrderIndex)
                {
                    continue;
                }

                if (ContainsAll(fired.Cells, candidate.Cells))
                {
                    return true;
                }
            }

            return false;
        }

        private static void SuppressLower(
            PatternCandidate fired,
            List<PatternCandidate> allCandidates)
        {
            foreach (PatternCandidate candidate in allCandidates)
            {
                if (candidate.Suppressed)
                {
                    continue;
                }

                if (candidate.Definition.OrderIndex >= fired.Definition.OrderIndex)
                {
                    continue;
                }

                if (ContainsAll(fired.Cells, candidate.Cells))
                {
                    candidate.Suppressed = true;
                }
            }
        }

        private static bool ContainsAll(List<SlotCell> container, List<SlotCell> target)
        {
            foreach (SlotCell targetCell in target)
            {
                bool found = false;

                foreach (SlotCell containerCell in container)
                {
                    if (containerCell == targetCell)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static SlotPatternMatch ToMatch(PatternCandidate candidate, int repeatIndex)
        {
            int cellCount = candidate.Cells?.Count ?? 0;
            int baseValue = SlotSymbolAttackValues.DamageFor(candidate.Symbol) * cellCount;

            return new SlotPatternMatch(
                candidate.Definition,
                candidate.Symbol,
                candidate.Cells,
                baseValue,
                repeatIndex);
        }

    }
}
