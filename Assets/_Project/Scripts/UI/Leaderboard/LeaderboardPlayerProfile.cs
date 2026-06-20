using System;
using UnityEngine;

namespace SlotRogue.UI.Leaderboard
{
    public readonly struct LeaderboardPlayerProfile
    {
        public LeaderboardPlayerProfile(string nickname)
        {
            Nickname = nickname?.Trim() ?? string.Empty;
        }

        public string Nickname { get; }

        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(Nickname) &&
            Nickname.Length <= 50;
    }

    internal static class LeaderboardPlayerProfileStore
    {
        private const int CurrentVersion = 2;
        private const string VersionKey = "SlotRogue.Leaderboard.Profile.Version";
        private const string NicknameKey = "SlotRogue.Leaderboard.Profile.Nickname";
        private const string LegacyCountryCodeKey =
            "SlotRogue.Leaderboard.Profile.CountryCode";

        internal static bool HasProfile => TryLoad(out _);

        internal static bool TryLoad(out LeaderboardPlayerProfile profile)
        {
            profile = default;
            int storedVersion = PlayerPrefs.GetInt(VersionKey, 0);
            if (storedVersion <= 0)
            {
                return false;
            }

            profile = new LeaderboardPlayerProfile(
                PlayerPrefs.GetString(NicknameKey, string.Empty));
            if (!profile.IsComplete)
            {
                return false;
            }

            if (storedVersion != CurrentVersion ||
                PlayerPrefs.HasKey(LegacyCountryCodeKey))
            {
                PlayerPrefs.SetInt(VersionKey, CurrentVersion);
                PlayerPrefs.DeleteKey(LegacyCountryCodeKey);
                PlayerPrefs.Save();
            }

            return true;
        }

        internal static LeaderboardPlayerProfile LoadOrDefault()
        {
            return TryLoad(out LeaderboardPlayerProfile profile)
                ? profile
                : new LeaderboardPlayerProfile(string.Empty);
        }

        internal static void Save(LeaderboardPlayerProfile profile)
        {
            if (!profile.IsComplete)
            {
                throw new ArgumentException("Leaderboard profile is incomplete.", nameof(profile));
            }

            PlayerPrefs.SetInt(VersionKey, CurrentVersion);
            PlayerPrefs.SetString(NicknameKey, profile.Nickname);
            PlayerPrefs.DeleteKey(LegacyCountryCodeKey);
            PlayerPrefs.Save();
        }
    }

    internal static class LeaderboardPlayerCosmeticStore
    {
        private const string ProfileIconKey = "SlotRogue.Leaderboard.Profile.IconId";
        private const string MessageKey = "SlotRogue.Leaderboard.Profile.Message";
        private const string DefaultMessage = "허접ㅋ";

        internal static string ProfileIconId =>
            PlayerPrefs.GetString(ProfileIconKey, string.Empty);

        internal static string Message
        {
            get
            {
                string message = PlayerPrefs.GetString(MessageKey, DefaultMessage);
                return string.IsNullOrWhiteSpace(message) ? DefaultMessage : message.Trim();
            }
        }

        internal static void SaveProfileIcon(string profileIconId)
        {
            PlayerPrefs.SetString(ProfileIconKey, profileIconId ?? string.Empty);
            PlayerPrefs.Save();
        }

        internal static void SaveMessage(string message)
        {
            PlayerPrefs.SetString(
                MessageKey,
                string.IsNullOrWhiteSpace(message) ? DefaultMessage : message.Trim());
            PlayerPrefs.Save();
        }
    }
}
