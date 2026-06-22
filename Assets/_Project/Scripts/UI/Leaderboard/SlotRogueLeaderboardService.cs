using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

namespace SlotRogue.UI.Leaderboard
{
    public static class SlotRogueLeaderboardService
    {
        private static Task<LeaderboardServiceResult> _initializationTask;
        private static Task<LeaderboardServiceResult> _lastSubmissionTask;

        public static string PlayerName =>
            LeaderboardPlayerProfileStore.LoadOrDefault().Nickname;

        public static bool IsReady =>
            UnityServices.State == ServicesInitializationState.Initialized &&
            AuthenticationService.Instance.IsSignedIn;

        internal static async UniTask<LeaderboardServiceResult> InitializeAsync()
        {
            if (IsReady)
            {
                return LeaderboardServiceResult.Succeeded();
            }

            Task<LeaderboardServiceResult> activeTask = _initializationTask;
            if (activeTask == null || activeTask.IsCompleted)
            {
                activeTask = InitializeCoreAsync().AsTask();
                _initializationTask = activeTask;
            }

            LeaderboardServiceResult result = await activeTask;
            if (!result.Success && ReferenceEquals(_initializationTask, activeTask))
            {
                _initializationTask = null;
            }

            return result;
        }

        internal static async UniTask<LeaderboardServiceResult> SubmitRunAsync(
            LeaderboardRunSnapshot snapshot)
        {
            LeaderboardServiceResult initialization = await InitializeAsync();
            if (!initialization.Success)
            {
                return initialization;
            }

            try
            {
                var options = new AddPlayerScoreOptions
                {
                    Metadata = snapshot.Metadata,
                };

                await LeaderboardsService.Instance.AddPlayerScoreAsync(
                    LeaderboardConstants.Id,
                    snapshot.Score,
                    options);
                return LeaderboardServiceResult.Succeeded();
            }
            catch (Exception exception)
            {
                return LeaderboardServiceResult.Failed(ToUserMessage(exception));
            }
        }

        internal static void QueueRunSubmission(LeaderboardRunSnapshot snapshot)
        {
            if (!LeaderboardBestRunStore.ShouldSubmit(snapshot))
            {
                return;
            }

            _lastSubmissionTask = SubmitBestRunAsync(snapshot).AsTask();
        }

        private static async UniTask<LeaderboardServiceResult> SubmitBestRunAsync(
            LeaderboardRunSnapshot snapshot)
        {
            LeaderboardServiceResult result = await SubmitRunAsync(snapshot);
            if (result.Success)
            {
                LeaderboardBestRunStore.Save(snapshot);
            }

            return result;
        }

        internal static async UniTask<LeaderboardServiceResult<IReadOnlyList<LeaderboardEntryData>>>
            GetTopEntriesAsync(int limit = LeaderboardConstants.DefaultPageSize)
        {
            Task<LeaderboardServiceResult> pendingSubmission = _lastSubmissionTask;
            if (pendingSubmission != null && !pendingSubmission.IsCompleted)
            {
                await pendingSubmission;
            }

            LeaderboardServiceResult initialization = await InitializeAsync();
            if (!initialization.Success)
            {
                return LeaderboardServiceResult<IReadOnlyList<LeaderboardEntryData>>.Failed(
                    initialization.ErrorMessage);
            }

            try
            {
                var options = new GetScoresOptions
                {
                    Offset = 0,
                    Limit = Math.Max(1, Math.Min(100, limit)),
                    IncludeMetadata = true,
                };

                LeaderboardScoresPage page = await LeaderboardsService.Instance.GetScoresAsync(
                    LeaderboardConstants.Id,
                    options);
                var entries = new List<LeaderboardEntryData>(page.Results?.Count ?? 0);
                string currentPlayerId = AuthenticationService.Instance.PlayerId;

                if (page.Results != null)
                {
                    for (int index = 0; index < page.Results.Count; index++)
                    {
                        LeaderboardEntry entry = page.Results[index];
                        int fallbackWave = Math.Max(1, (int)Math.Floor(entry.Score));
                        LeaderboardMetadataPayload metadata =
                            LeaderboardMetadataCodec.Parse(entry.Metadata, fallbackWave);

                        entries.Add(new LeaderboardEntryData(
                            entry.Rank + 1,
                            entry.PlayerName,
                            entry.Score,
                            metadata.Wave,
                            metadata.RelicIds,
                            metadata.SymbolCounts,
                            metadata.ProfileIconId,
                            metadata.Message,
                            string.Equals(
                                entry.PlayerId,
                                currentPlayerId,
                                StringComparison.Ordinal)));
                    }
                }

                return LeaderboardServiceResult<IReadOnlyList<LeaderboardEntryData>>.Succeeded(
                    entries);
            }
            catch (Exception exception)
            {
                return LeaderboardServiceResult<IReadOnlyList<LeaderboardEntryData>>.Failed(
                    ToUserMessage(exception));
            }
        }

        internal static async UniTask<LeaderboardServiceResult<LeaderboardPlayerProfile>>
            SaveProfileAsync(string requestedName)
        {
            var profile = new LeaderboardPlayerProfile(requestedName);
            if (string.IsNullOrWhiteSpace(profile.Nickname))
            {
                return LeaderboardServiceResult<LeaderboardPlayerProfile>.Failed(
                    "Enter a nickname.");
            }

            if (profile.Nickname.Length > 50)
            {
                return LeaderboardServiceResult<LeaderboardPlayerProfile>.Failed(
                    "Nickname must be 50 characters or fewer.");
            }

            LeaderboardServiceResult initialization = await InitializeAsync();
            if (!initialization.Success)
            {
                return LeaderboardServiceResult<LeaderboardPlayerProfile>.Failed(
                    initialization.ErrorMessage);
            }

            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(profile.Nickname);
                LeaderboardPlayerProfileStore.Save(profile);
                return LeaderboardServiceResult<LeaderboardPlayerProfile>.Succeeded(profile);
            }
            catch (Exception exception)
            {
                return LeaderboardServiceResult<LeaderboardPlayerProfile>.Failed(
                    ToUserMessage(exception));
            }
        }

        private static async UniTask<LeaderboardServiceResult> InitializeCoreAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                return LeaderboardServiceResult.Succeeded();
            }
            catch (Exception exception)
            {
                return LeaderboardServiceResult.Failed(ToUserMessage(exception));
            }
        }

        private static string ToUserMessage(Exception exception)
        {
            if (exception == null)
            {
                return "Leaderboard service is unavailable.";
            }

            string message = exception.Message;
            return string.IsNullOrWhiteSpace(message)
                ? "Leaderboard service is unavailable."
                : message;
        }
    }

    internal static class LeaderboardBestRunStore
    {
        private const string BestWaveKey = "SlotRogue.Leaderboard.BestWave";

        internal static bool ShouldSubmit(LeaderboardRunSnapshot snapshot)
        {
            return snapshot.Score >= PlayerPrefs.GetInt(BestWaveKey, 0);
        }

        internal static void Save(LeaderboardRunSnapshot snapshot)
        {
            int bestWave = PlayerPrefs.GetInt(BestWaveKey, 0);
            if (snapshot.Score < bestWave)
            {
                return;
            }

            PlayerPrefs.SetInt(BestWaveKey, snapshot.Score);
            PlayerPrefs.Save();
        }
    }
}
