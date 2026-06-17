using System;
using System.Collections.Generic;
using SlotRogue.Relics.Pool;
using UnityEngine;

namespace SlotRogue.UI.Leaderboard
{
    public static class LeaderboardConstants
    {
        public const string Id = "Slot_Rogue_Leaderboard";
        public const int MetadataSchemaVersion = 2;
        public const int DefaultPageSize = 10;
    }

    [Serializable]
    public sealed class LeaderboardMetadataPayload
    {
        public int SchemaVersion;
        public int Wave;
        public string[] RelicIds;

        public LeaderboardMetadataPayload()
        {
            SchemaVersion = LeaderboardConstants.MetadataSchemaVersion;
            Wave = 1;
            RelicIds = Array.Empty<string>();
        }

        public LeaderboardMetadataPayload(
            int wave,
            IReadOnlyList<string> relicIds)
        {
            SchemaVersion = LeaderboardConstants.MetadataSchemaVersion;
            Wave = Math.Max(1, wave);
            RelicIds = CopyRelicIds(relicIds);
        }

        private static string[] CopyRelicIds(IReadOnlyList<string> relicIds)
        {
            if (relicIds == null || relicIds.Count == 0)
            {
                return Array.Empty<string>();
            }

            var copiedIds = new List<string>(relicIds.Count);
            for (int index = 0; index < relicIds.Count; index++)
            {
                string relicId = relicIds[index];
                if (!string.IsNullOrWhiteSpace(relicId))
                {
                    copiedIds.Add(relicId.Trim());
                }
            }

            return copiedIds.ToArray();
        }
    }

    public readonly struct LeaderboardRunSnapshot
    {
        public LeaderboardRunSnapshot(
            int score,
            int wave,
            IReadOnlyList<string> relicIds)
        {
            Score = Math.Max(0, score);
            Metadata = new LeaderboardMetadataPayload(wave, relicIds);
        }

        public int Score { get; }

        public LeaderboardMetadataPayload Metadata { get; }

        public static LeaderboardRunSnapshot Capture(
            int victories,
            int currentBattleNumber,
            IReadOnlyList<RelicDefinition> ownedRelics)
        {
            var relicIds = new List<string>(ownedRelics?.Count ?? 0);
            if (ownedRelics != null)
            {
                for (int index = 0; index < ownedRelics.Count; index++)
                {
                    RelicDefinition relic = ownedRelics[index];
                    if (relic != null && !string.IsNullOrWhiteSpace(relic.Id))
                    {
                        relicIds.Add(relic.Id);
                    }
                }
            }

            return new LeaderboardRunSnapshot(
                victories,
                currentBattleNumber,
                relicIds);
        }
    }

    public readonly struct LeaderboardEntryData
    {
        public LeaderboardEntryData(
            int rank,
            string playerName,
            double score,
            int wave,
            IReadOnlyList<string> relicIds,
            bool isCurrentPlayer)
        {
            Rank = Math.Max(1, rank);
            PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Unknown" : playerName;
            Score = Math.Max(0d, score);
            Wave = Math.Max(1, wave);
            RelicIds = relicIds ?? Array.Empty<string>();
            IsCurrentPlayer = isCurrentPlayer;
        }

        public int Rank { get; }

        public string PlayerName { get; }

        public double Score { get; }

        public int Wave { get; }

        public IReadOnlyList<string> RelicIds { get; }

        public bool IsCurrentPlayer { get; }
    }

    public static class LeaderboardMetadataCodec
    {
        public static LeaderboardMetadataPayload Parse(string json, int fallbackWave)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateFallback(fallbackWave);
            }

            try
            {
                LeaderboardMetadataPayload payload =
                    JsonUtility.FromJson<LeaderboardMetadataPayload>(json);
                if (payload == null)
                {
                    return CreateFallback(fallbackWave);
                }

                payload.SchemaVersion = payload.SchemaVersion <= 0
                    ? LeaderboardConstants.MetadataSchemaVersion
                    : payload.SchemaVersion;
                payload.Wave = payload.Wave <= 0 ? Math.Max(1, fallbackWave) : payload.Wave;
                payload.RelicIds ??= Array.Empty<string>();
                return payload;
            }
            catch (ArgumentException)
            {
                return CreateFallback(fallbackWave);
            }
        }

        private static LeaderboardMetadataPayload CreateFallback(int fallbackWave)
        {
            return new LeaderboardMetadataPayload(
                Math.Max(1, fallbackWave),
                Array.Empty<string>());
        }
    }

    internal readonly struct LeaderboardServiceResult
    {
        private LeaderboardServiceResult(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        internal bool Success { get; }

        internal string ErrorMessage { get; }

        internal static LeaderboardServiceResult Succeeded() => new(true, string.Empty);

        internal static LeaderboardServiceResult Failed(string errorMessage) =>
            new(false, errorMessage);
    }

    internal readonly struct LeaderboardServiceResult<T>
    {
        private LeaderboardServiceResult(bool success, T value, string errorMessage)
        {
            Success = success;
            Value = value;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        internal bool Success { get; }

        internal T Value { get; }

        internal string ErrorMessage { get; }

        internal static LeaderboardServiceResult<T> Succeeded(T value) =>
            new(true, value, string.Empty);

        internal static LeaderboardServiceResult<T> Failed(string errorMessage) =>
            new(false, default, errorMessage);
    }
}
