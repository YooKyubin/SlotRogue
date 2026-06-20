using System;
using System.Collections.Generic;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;
using UnityEngine;

namespace SlotRogue.UI.Leaderboard
{
    public static class LeaderboardConstants
    {
        public const string Id = "Slot_Rogue_Leaderboard";
        public const int MetadataSchemaVersion = 3;
        public const int DefaultPageSize = 10;
    }

    [Serializable]
    public sealed class LeaderboardSymbolCount
    {
        public string Symbol;
        public int Count;

        public LeaderboardSymbolCount()
        {
            Symbol = string.Empty;
            Count = 0;
        }

        public LeaderboardSymbolCount(string symbol, int count)
        {
            Symbol = symbol ?? string.Empty;
            Count = Math.Max(0, count);
        }

        public bool TryGetSymbol(out SlotSymbolType symbol)
        {
            return Enum.TryParse(Symbol, ignoreCase: true, out symbol);
        }
    }

    [Serializable]
    public sealed class LeaderboardMetadataPayload
    {
        public int SchemaVersion;
        public int Wave;
        public string[] RelicIds;
        public LeaderboardSymbolCount[] SymbolCounts;
        public string ProfileIconId;
        public string Message;

        public LeaderboardMetadataPayload()
        {
            SchemaVersion = LeaderboardConstants.MetadataSchemaVersion;
            Wave = 1;
            RelicIds = Array.Empty<string>();
            SymbolCounts = Array.Empty<LeaderboardSymbolCount>();
            ProfileIconId = string.Empty;
            Message = LeaderboardPlayerCosmeticStore.Message;
        }

        public LeaderboardMetadataPayload(
            int wave,
            IReadOnlyList<string> relicIds,
            IReadOnlyList<LeaderboardSymbolCount> symbolCounts,
            string profileIconId,
            string message)
        {
            SchemaVersion = LeaderboardConstants.MetadataSchemaVersion;
            Wave = Math.Max(1, wave);
            RelicIds = CopyRelicIds(relicIds);
            SymbolCounts = CopySymbolCounts(symbolCounts);
            ProfileIconId = profileIconId ?? string.Empty;
            Message = string.IsNullOrWhiteSpace(message)
                ? LeaderboardPlayerCosmeticStore.Message
                : message.Trim();
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

        private static LeaderboardSymbolCount[] CopySymbolCounts(
            IReadOnlyList<LeaderboardSymbolCount> symbolCounts)
        {
            if (symbolCounts == null || symbolCounts.Count == 0)
            {
                return Array.Empty<LeaderboardSymbolCount>();
            }

            var copiedCounts = new List<LeaderboardSymbolCount>(symbolCounts.Count);
            for (int index = 0; index < symbolCounts.Count; index++)
            {
                LeaderboardSymbolCount count = symbolCounts[index];
                if (count == null || string.IsNullOrWhiteSpace(count.Symbol))
                {
                    continue;
                }

                copiedCounts.Add(new LeaderboardSymbolCount(
                    count.Symbol.Trim(),
                    count.Count));
            }

            return copiedCounts.ToArray();
        }
    }

    public readonly struct LeaderboardRunSnapshot
    {
        public LeaderboardRunSnapshot(
            int score,
            int wave,
            IReadOnlyList<string> relicIds,
            IReadOnlyList<LeaderboardSymbolCount> symbolCounts,
            string profileIconId,
            string message)
        {
            Score = Math.Max(0, score);
            Metadata = new LeaderboardMetadataPayload(
                wave,
                relicIds,
                symbolCounts,
                profileIconId,
                message);
        }

        public int Score { get; }

        public LeaderboardMetadataPayload Metadata { get; }

        public static LeaderboardRunSnapshot Capture(
            int victories,
            int currentBattleNumber,
            IReadOnlyList<RelicDefinition> ownedRelics,
            SlotSymbolPool slotPool)
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

            IReadOnlyList<LeaderboardSymbolCount> symbolCounts =
                CaptureSymbolCounts(slotPool);
            int wave = Math.Max(1, currentBattleNumber);
            return new LeaderboardRunSnapshot(
                wave,
                wave,
                relicIds,
                symbolCounts,
                LeaderboardPlayerCosmeticStore.ProfileIconId,
                LeaderboardPlayerCosmeticStore.Message);
        }

        private static IReadOnlyList<LeaderboardSymbolCount> CaptureSymbolCounts(
            SlotSymbolPool slotPool)
        {
            if (slotPool == null)
            {
                return Array.Empty<LeaderboardSymbolCount>();
            }

            IReadOnlyList<SlotSymbolType> symbols = SlotSymbolPool.Symbols;
            var counts = new List<LeaderboardSymbolCount>(symbols.Count);
            for (int index = 0; index < symbols.Count; index++)
            {
                SlotSymbolType symbol = symbols[index];
                counts.Add(new LeaderboardSymbolCount(
                    symbol.ToString(),
                    slotPool.GetCount(symbol)));
            }

            return counts;
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
            IReadOnlyList<LeaderboardSymbolCount> symbolCounts,
            string profileIconId,
            string message,
            bool isCurrentPlayer)
        {
            Rank = Math.Max(1, rank);
            PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Unknown" : playerName;
            Score = Math.Max(0d, score);
            Wave = Math.Max(1, wave);
            RelicIds = relicIds ?? Array.Empty<string>();
            SymbolCounts = symbolCounts ?? Array.Empty<LeaderboardSymbolCount>();
            ProfileIconId = profileIconId ?? string.Empty;
            Message = string.IsNullOrWhiteSpace(message)
                ? LeaderboardPlayerCosmeticStore.Message
                : message.Trim();
            IsCurrentPlayer = isCurrentPlayer;
        }

        public int Rank { get; }

        public string PlayerName { get; }

        public double Score { get; }

        public int Wave { get; }

        public IReadOnlyList<string> RelicIds { get; }

        public IReadOnlyList<LeaderboardSymbolCount> SymbolCounts { get; }

        public string ProfileIconId { get; }

        public string Message { get; }

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
                payload.SymbolCounts ??= Array.Empty<LeaderboardSymbolCount>();
                payload.ProfileIconId ??= string.Empty;
                payload.Message = string.IsNullOrWhiteSpace(payload.Message)
                    ? LeaderboardPlayerCosmeticStore.Message
                    : payload.Message.Trim();
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
                Array.Empty<string>(),
                Array.Empty<LeaderboardSymbolCount>(),
                string.Empty,
                LeaderboardPlayerCosmeticStore.Message);
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
