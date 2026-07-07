using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Leaderboard
{
    public sealed class LeaderboardRankRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _designationText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _waveText;
        [SerializeField] private Image _profileImage;

        public bool IsValid =>
            _rankText != null &&
            _nameText != null &&
            _waveText != null;

        private void Awake()
        {
            ValidateRequiredReferences();
        }

        public void Render(LeaderboardEntryData entry)
        {
            if (!ValidateRequiredReferences())
            {
                return;
            }

            gameObject.SetActive(true);
            SetText(_rankText, entry.Rank.ToString());
            SetText(_nameText, entry.PlayerName);
            SetText(_waveText, $"WAVE {entry.Wave}");

            if (_profileImage != null)
            {
                _profileImage.enabled = true;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private bool ValidateRequiredReferences()
        {
            if (IsValid)
            {
                return true;
            }

            Debug.LogError(
                "[LeaderboardRankRowView] UI references must be wired in the inspector. " +
                $"Missing: {BuildMissingReferenceSummary()}",
                this);
            return false;
        }

        private string BuildMissingReferenceSummary()
        {
            var builder = new System.Text.StringBuilder();
            AppendMissing(builder, _rankText != null, "Rank Text");
            AppendMissing(builder, _nameText != null, "Name Text");
            AppendMissing(builder, _waveText != null, "Wave Text");
            return builder.Length > 0 ? builder.ToString() : "none";
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static void AppendMissing(
            System.Text.StringBuilder builder,
            bool hasReference,
            string label)
        {
            if (hasReference)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(label);
        }
    }
}
