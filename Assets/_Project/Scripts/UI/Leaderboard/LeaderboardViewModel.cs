using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace SlotRogue.UI.Leaderboard
{
    public sealed class LeaderboardViewModel
    {
        private IReadOnlyList<LeaderboardEntryData> _entries =
            Array.Empty<LeaderboardEntryData>();
        private bool _isVisible;
        private bool _isBusy;
        private bool _isProfileRequired;
        private string _playerName = string.Empty;
        private string _statusMessage = string.Empty;

        public LeaderboardViewModel()
        {
            State = LeaderboardViewState.Hidden;
        }

        public event Action<LeaderboardViewState> Changed;

        public LeaderboardViewState State { get; private set; }

        public bool HasCompletedProfile => LeaderboardPlayerProfileStore.HasProfile;

        public void EvaluateProfileRequirement()
        {
            if (LeaderboardPlayerProfileStore.TryLoad(out LeaderboardPlayerProfile profile))
            {
                _playerName = profile.Nickname;
                _isProfileRequired = false;
                Publish();
                return;
            }

            RequireProfile();
        }

        public void RequireProfile()
        {
            _isVisible = true;
            _isProfileRequired = true;
            _statusMessage = "A nickname is required before playing.";
            Publish();
        }

        public async UniTask OpenAsync()
        {
            if (!LeaderboardPlayerProfileStore.TryLoad(out LeaderboardPlayerProfile profile))
            {
                RequireProfile();
                return;
            }

            _playerName = profile.Nickname;
            _isVisible = true;
            _isProfileRequired = false;
            await RefreshAsync();
        }

        public void Close()
        {
            if (_isProfileRequired)
            {
                return;
            }

            _isVisible = false;
            Publish();
        }

        public async UniTask RefreshAsync()
        {
            if (_isBusy)
            {
                return;
            }

            if (!LeaderboardPlayerProfileStore.HasProfile)
            {
                RequireProfile();
                return;
            }

            _isBusy = true;
            _statusMessage = "Loading leaderboard...";
            Publish();

            LeaderboardServiceResult<IReadOnlyList<LeaderboardEntryData>> result =
                await SlotRogueLeaderboardService.GetTopEntriesAsync();
            _isBusy = false;

            if (result.Success)
            {
                _entries = result.Value ?? Array.Empty<LeaderboardEntryData>();
                if (LeaderboardPlayerProfileStore.TryLoad(
                    out LeaderboardPlayerProfile profile))
                {
                    _playerName = profile.Nickname;
                }
                _statusMessage = _entries.Count == 0
                    ? "No scores yet."
                    : string.Empty;
            }
            else
            {
                _statusMessage = result.ErrorMessage;
            }

            Publish();
        }

        public async UniTask SaveProfileAsync(string requestedName)
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            bool wasProfileRequired = _isProfileRequired;
            _statusMessage = "Saving profile...";
            Publish();

            LeaderboardServiceResult<LeaderboardPlayerProfile> result =
                await SlotRogueLeaderboardService.SaveProfileAsync(requestedName);
            _isBusy = false;

            if (!result.Success)
            {
                _statusMessage = result.ErrorMessage;
                Publish();
                return;
            }

            _playerName = result.Value.Nickname;
            _isProfileRequired = false;
            _statusMessage = "Profile saved.";

            if (wasProfileRequired)
            {
                _isVisible = false;
                Publish();
                return;
            }

            Publish();
            await RefreshAsync();
        }

        private void Publish()
        {
            State = new LeaderboardViewState(
                _isVisible,
                _isBusy,
                _isProfileRequired,
                _playerName,
                _statusMessage,
                _entries);
            Changed?.Invoke(State);
        }
    }

    public sealed class LeaderboardViewState
    {
        public static readonly LeaderboardViewState Hidden = new(
            false,
            false,
            false,
            string.Empty,
            string.Empty,
            Array.Empty<LeaderboardEntryData>());

        public LeaderboardViewState(
            bool isVisible,
            bool isLoading,
            bool isProfileRequired,
            string playerName,
            string statusMessage,
            IReadOnlyList<LeaderboardEntryData> entries)
        {
            IsVisible = isVisible;
            IsLoading = isLoading;
            IsProfileRequired = isProfileRequired;
            PlayerName = playerName ?? string.Empty;
            StatusMessage = statusMessage ?? string.Empty;
            Entries = entries ?? Array.Empty<LeaderboardEntryData>();
        }

        public bool IsVisible { get; }

        public bool IsLoading { get; }

        public bool IsProfileRequired { get; }

        public string PlayerName { get; }

        public string StatusMessage { get; }

        public IReadOnlyList<LeaderboardEntryData> Entries { get; }
    }
}
