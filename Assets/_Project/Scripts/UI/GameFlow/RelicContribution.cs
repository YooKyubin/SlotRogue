using System;
using System.Collections.Generic;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    public readonly struct RelicContributionDelta
    {
        public RelicContributionDelta(
            string relicId,
            string relicName,
            int damagePerHit,
            int block,
            int heal,
            int triggerPatternIndex = -1)
        {
            RelicId = relicId ?? string.Empty;
            RelicName = relicName ?? string.Empty;
            DamagePerHit = Math.Max(0, damagePerHit);
            Block = Math.Max(0, block);
            Heal = Math.Max(0, heal);
            TriggerPatternIndex = triggerPatternIndex;
        }

        public string RelicId { get; }

        public string RelicName { get; }

        public int DamagePerHit { get; }

        public int Block { get; }

        public int Heal { get; }

        public int TriggerPatternIndex { get; }
    }

    public readonly struct RelicContributionSnapshot
    {
        public RelicContributionSnapshot(
            string relicId,
            string relicName,
            int triggerCount,
            int damage,
            int block,
            int heal)
        {
            RelicId = relicId ?? string.Empty;
            RelicName = relicName ?? string.Empty;
            TriggerCount = Math.Max(0, triggerCount);
            Damage = Math.Max(0, damage);
            Block = Math.Max(0, block);
            Heal = Math.Max(0, heal);
        }

        public string RelicId { get; }

        public string RelicName { get; }

        public int TriggerCount { get; }

        public int Damage { get; }

        public int Block { get; }

        public int Heal { get; }
    }

    public readonly struct SlotSymbolContributionSnapshot
    {
        public SlotSymbolContributionSnapshot(
            SlotSymbolType symbol,
            int patternCount,
            int baseAttackPower,
            int relicAttackPower,
            int defensePower = 0)
        {
            Symbol = symbol;
            PatternCount = Math.Max(0, patternCount);
            BaseAttackPower = Math.Max(0, baseAttackPower);
            RelicAttackPower = Math.Max(0, relicAttackPower);
            DefensePower = Math.Max(0, defensePower);
        }

        public SlotSymbolType Symbol { get; }

        public int PatternCount { get; }

        public int BaseAttackPower { get; }

        public int RelicAttackPower { get; }

        public int DefensePower { get; }

        public int TotalAttackPower => BaseAttackPower + RelicAttackPower;
    }

    internal sealed class RelicContributionAccumulator
    {
        private readonly Dictionary<string, MutableContribution> _entries =
            new(StringComparer.Ordinal);

        internal void Clear()
        {
            _entries.Clear();
        }

        internal void RecordTurn(
            IReadOnlyList<RelicContributionDelta> deltas,
            int attackCount)
        {
            if (deltas == null)
            {
                return;
            }

            int normalizedAttackCount = Math.Max(1, attackCount);
            for (int index = 0; index < deltas.Count; index++)
            {
                RelicContributionDelta delta = deltas[index];
                if (string.IsNullOrWhiteSpace(delta.RelicId))
                {
                    continue;
                }

                MutableContribution entry = GetOrCreate(delta.RelicId, delta.RelicName);
                entry.TriggerCount++;
                entry.Damage += delta.DamagePerHit * normalizedAttackCount;
                entry.Block += delta.Block;
                entry.Heal += delta.Heal;
            }
        }

        internal void Add(IReadOnlyList<RelicContributionSnapshot> snapshots)
        {
            if (snapshots == null)
            {
                return;
            }

            for (int index = 0; index < snapshots.Count; index++)
            {
                RelicContributionSnapshot snapshot = snapshots[index];
                if (string.IsNullOrWhiteSpace(snapshot.RelicId))
                {
                    continue;
                }

                MutableContribution entry = GetOrCreate(
                    snapshot.RelicId,
                    snapshot.RelicName);
                entry.TriggerCount += snapshot.TriggerCount;
                entry.Damage += snapshot.Damage;
                entry.Block += snapshot.Block;
                entry.Heal += snapshot.Heal;
            }
        }

        internal IReadOnlyList<RelicContributionSnapshot> Snapshot()
        {
            var snapshots = new List<RelicContributionSnapshot>(_entries.Count);
            foreach (MutableContribution entry in _entries.Values)
            {
                snapshots.Add(entry.ToSnapshot());
            }

            return snapshots;
        }

        internal IReadOnlyList<RelicContributionSnapshot> SnapshotForRelics(
            IReadOnlyList<RelicDefinition> relics)
        {
            if (relics == null || relics.Count == 0)
            {
                return Array.Empty<RelicContributionSnapshot>();
            }

            var snapshots = new List<RelicContributionSnapshot>(relics.Count);
            var addedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < relics.Count; index++)
            {
                RelicDefinition relic = relics[index];
                if (relic == null ||
                    string.IsNullOrWhiteSpace(relic.Id) ||
                    !addedIds.Add(relic.Id))
                {
                    continue;
                }

                if (_entries.TryGetValue(relic.Id, out MutableContribution entry))
                {
                    snapshots.Add(entry.ToSnapshot());
                }
                else
                {
                    snapshots.Add(new RelicContributionSnapshot(
                        relic.Id,
                        relic.Name,
                        0,
                        0,
                        0,
                        0));
                }
            }

            return snapshots;
        }

        private MutableContribution GetOrCreate(string relicId, string relicName)
        {
            if (_entries.TryGetValue(relicId, out MutableContribution entry))
            {
                return entry;
            }

            entry = new MutableContribution(relicId, relicName);
            _entries.Add(relicId, entry);
            return entry;
        }

        private sealed class MutableContribution
        {
            internal MutableContribution(string relicId, string relicName)
            {
                RelicId = relicId;
                RelicName = relicName;
            }

            internal string RelicId { get; }

            internal string RelicName { get; }

            internal int TriggerCount { get; set; }

            internal int Damage { get; set; }

            internal int Block { get; set; }

            internal int Heal { get; set; }

            internal RelicContributionSnapshot ToSnapshot()
            {
                return new RelicContributionSnapshot(
                    RelicId,
                    RelicName,
                    TriggerCount,
                    Damage,
                    Block,
                    Heal);
            }
        }
    }

    internal sealed class SlotSymbolContributionAccumulator
    {
        private readonly Dictionary<SlotSymbolType, MutableSymbolContribution> _entries =
            new();

        internal void Clear()
        {
            _entries.Clear();
        }

        internal void RecordTurn(
            IReadOnlyList<SlotPatternMatch> matches,
            SlotCombatRequest baseRequest,
            IReadOnlyList<RelicContributionDelta> relicDeltas,
            int attackCount)
        {
            if (matches == null || matches.Count == 0)
            {
                return;
            }

            RecordBaseAttack(matches, baseRequest);
            RecordBaseDefense(matches, baseRequest);
            RecordRelicContributions(matches, relicDeltas, attackCount);
        }

        private void RecordBaseAttack(
            IReadOnlyList<SlotPatternMatch> matches,
            SlotCombatRequest baseRequest)
        {
            int remainingPatternValue = CalculateTotalPatternValue(matches);
            int remainingBaseAttackPower = CalculateAttackPower(baseRequest);

            for (int index = 0; index < matches.Count; index++)
            {
                SlotPatternMatch match = matches[index];
                if (match == null)
                {
                    continue;
                }

                int patternValue = Math.Max(0, match.CalculatedValue);
                int attackPower = 0;

                if (patternValue > 0 && remainingPatternValue > 0)
                {
                    attackPower = remainingPatternValue == patternValue
                        ? remainingBaseAttackPower
                        : (int)((long)remainingBaseAttackPower * patternValue / remainingPatternValue);
                    remainingPatternValue -= patternValue;
                    remainingBaseAttackPower -= attackPower;
                }

                MutableSymbolContribution entry = GetOrCreate(match.Symbol);
                entry.PatternCount++;
                entry.BaseAttackPower += attackPower;
            }
        }

        private void RecordBaseDefense(
            IReadOnlyList<SlotPatternMatch> matches,
            SlotCombatRequest baseRequest)
        {
            int remainingPatternValue = CalculateTotalPatternValue(matches);
            int remainingDefensePower = Math.Max(0, baseRequest?.Defense ?? 0);

            for (int index = 0; index < matches.Count; index++)
            {
                SlotPatternMatch match = matches[index];
                if (match == null)
                {
                    continue;
                }

                int patternValue = Math.Max(0, match.CalculatedValue);
                int defensePower = 0;

                if (patternValue > 0 && remainingPatternValue > 0)
                {
                    defensePower = remainingPatternValue == patternValue
                        ? remainingDefensePower
                        : (int)((long)remainingDefensePower * patternValue / remainingPatternValue);
                    remainingPatternValue -= patternValue;
                    remainingDefensePower -= defensePower;
                }

                MutableSymbolContribution entry = GetOrCreate(match.Symbol);
                entry.DefensePower += defensePower;
            }
        }

        private void RecordRelicContributions(
            IReadOnlyList<SlotPatternMatch> matches,
            IReadOnlyList<RelicContributionDelta> relicDeltas,
            int attackCount)
        {
            if (relicDeltas == null || relicDeltas.Count == 0)
            {
                return;
            }

            int normalizedAttackCount = Math.Max(1, attackCount);
            for (int index = 0; index < relicDeltas.Count; index++)
            {
                RelicContributionDelta delta = relicDeltas[index];
                if ((delta.DamagePerHit <= 0 && delta.Block <= 0) ||
                    delta.TriggerPatternIndex < 0 ||
                    delta.TriggerPatternIndex >= matches.Count)
                {
                    continue;
                }

                SlotPatternMatch match = matches[delta.TriggerPatternIndex];
                if (match == null)
                {
                    continue;
                }

                MutableSymbolContribution entry = GetOrCreate(match.Symbol);
                if (delta.DamagePerHit > 0)
                {
                    entry.RelicAttackPower += delta.DamagePerHit * normalizedAttackCount;
                }

                if (delta.Block > 0)
                {
                    entry.DefensePower += delta.Block;
                }
            }
        }

        internal void Add(IReadOnlyList<SlotSymbolContributionSnapshot> snapshots)
        {
            if (snapshots == null)
            {
                return;
            }

            for (int index = 0; index < snapshots.Count; index++)
            {
                SlotSymbolContributionSnapshot snapshot = snapshots[index];
                MutableSymbolContribution entry = GetOrCreate(snapshot.Symbol);
                entry.PatternCount += snapshot.PatternCount;
                entry.BaseAttackPower += snapshot.BaseAttackPower;
                entry.RelicAttackPower += snapshot.RelicAttackPower;
                entry.DefensePower += snapshot.DefensePower;
            }
        }

        internal IReadOnlyList<SlotSymbolContributionSnapshot> Snapshot()
        {
            var snapshots = new List<SlotSymbolContributionSnapshot>(_entries.Count);
            foreach (MutableSymbolContribution entry in _entries.Values)
            {
                snapshots.Add(entry.ToSnapshot());
            }

            return snapshots;
        }

        internal IReadOnlyList<SlotSymbolContributionSnapshot> SnapshotForSymbols(
            IReadOnlyList<SlotSymbolType> symbols)
        {
            if (symbols == null || symbols.Count == 0)
            {
                return Array.Empty<SlotSymbolContributionSnapshot>();
            }

            var snapshots = new List<SlotSymbolContributionSnapshot>(symbols.Count);
            var addedSymbols = new HashSet<SlotSymbolType>();
            for (int index = 0; index < symbols.Count; index++)
            {
                SlotSymbolType symbol = symbols[index];
                if (!addedSymbols.Add(symbol))
                {
                    continue;
                }

                snapshots.Add(_entries.TryGetValue(symbol, out MutableSymbolContribution entry)
                    ? entry.ToSnapshot()
                    : new SlotSymbolContributionSnapshot(symbol, 0, 0, 0));
            }

            return snapshots;
        }

        private MutableSymbolContribution GetOrCreate(SlotSymbolType symbol)
        {
            if (_entries.TryGetValue(symbol, out MutableSymbolContribution entry))
            {
                return entry;
            }

            entry = new MutableSymbolContribution(symbol);
            _entries.Add(symbol, entry);
            return entry;
        }

        private static int CalculateTotalPatternValue(IReadOnlyList<SlotPatternMatch> matches)
        {
            int total = 0;
            for (int index = 0; index < matches.Count; index++)
            {
                SlotPatternMatch match = matches[index];
                if (match != null)
                {
                    total += Math.Max(0, match.CalculatedValue);
                }
            }

            return total;
        }

        private static int CalculateAttackPower(SlotCombatRequest request)
        {
            if (request == null || request.Damage <= 0)
            {
                return 0;
            }

            return request.Damage * Math.Max(1, request.AttackCount);
        }

        private sealed class MutableSymbolContribution
        {
            internal MutableSymbolContribution(SlotSymbolType symbol)
            {
                Symbol = symbol;
            }

            internal SlotSymbolType Symbol { get; }

            internal int PatternCount { get; set; }

            internal int BaseAttackPower { get; set; }

            internal int RelicAttackPower { get; set; }

            internal int DefensePower { get; set; }

            internal SlotSymbolContributionSnapshot ToSnapshot()
            {
                return new SlotSymbolContributionSnapshot(
                    Symbol,
                    PatternCount,
                    BaseAttackPower,
                    RelicAttackPower,
                    DefensePower);
            }
        }
    }
}
