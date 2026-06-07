using System;
using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Core
{
    public sealed class SlotPatternResolver
    {
        private const int MinimumMatchLength = 3;

        public SlotPatternResult Resolve(SlotSpinResult spinResult)
        {
            if (spinResult == null)
            {
                return SlotPatternResult.NoMatch;
            }

            SlotPatternResult bestResult = SlotPatternResult.NoMatch;

            for (int row = 0; row < SlotSpinResult.Rows; row++)
            {
                int startColumn = 0;

                while (startColumn < SlotSpinResult.Columns)
                {
                    SlotSymbolType symbol = spinResult.GetSymbol(startColumn, row);
                    int matchLength = CountRunLength(spinResult, symbol, startColumn, row);

                    if (matchLength >= MinimumMatchLength)
                    {
                        SlotPatternResult candidate = CreateResult(symbol, row, startColumn, matchLength);

                        if (candidate.Score > bestResult.Score)
                        {
                            bestResult = candidate;
                        }
                    }

                    startColumn += matchLength;
                }
            }

            return bestResult;
        }

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
            return new SlotPatternMatch(
                candidate.Definition,
                candidate.Symbol,
                candidate.Cells,
                GetSymbolBaseScore(candidate.Symbol),
                repeatIndex);
        }

        private static int CountRunLength(
            SlotSpinResult spinResult,
            SlotSymbolType symbol,
            int startColumn,
            int row)
        {
            int matchLength = 0;

            for (int column = startColumn; column < SlotSpinResult.Columns; column++)
            {
                if (spinResult.GetSymbol(column, row) != symbol)
                {
                    break;
                }

                matchLength++;
            }

            return matchLength;
        }

        private static SlotPatternResult CreateResult(
            SlotSymbolType symbol,
            int row,
            int startColumn,
            int matchLength)
        {
            int score = GetSymbolScore(symbol) * matchLength;

            return new SlotPatternResult(
                true,
                $"{GetKoreanSymbolName(symbol)} {matchLength}칸",
                symbol,
                row,
                startColumn,
                matchLength,
                score);
        }

        private static string GetKoreanSymbolName(SlotSymbolType symbol) => symbol switch
        {
            SlotSymbolType.Cherry => "체리",
            SlotSymbolType.Seven => "세븐",
            SlotSymbolType.Grape => "포도",
            SlotSymbolType.Bell => "종",
            SlotSymbolType.Clover => "네잎클로버",
            SlotSymbolType.Lemon => "레몬",
            _ => symbol.ToString()
        };

        private static int GetSymbolScore(SlotSymbolType symbol)
        {
            switch (symbol)
            {
                case SlotSymbolType.Cherry:
                    return 6;
                case SlotSymbolType.Seven:
                    return 5;
                case SlotSymbolType.Grape:
                    return 4;
                case SlotSymbolType.Bell:
                    return 3;
                case SlotSymbolType.Clover:
                    return 8;
                case SlotSymbolType.Lemon:
                    return 7;
                default:
                    return 1;
            }
        }

        public static int GetSymbolBaseScore(SlotSymbolType symbol)
        {
            return GetSymbolScore(symbol);
        }
    }
}
