using System;
using System.Collections.Generic;
using SlotRogue.Relics.Pool;

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
}
