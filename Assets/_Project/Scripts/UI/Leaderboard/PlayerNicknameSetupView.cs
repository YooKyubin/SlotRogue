using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Leaderboard
{
    /// <summary>
    /// 플레이어 닉네임 설정 모달(로그인이 아니다). 튜토리얼 이후 등 프로필이 필요한 시점에
    /// 노출되어 닉네임을 입력받아 제출한다. 입력 상태는 LeaderboardViewModel이 관리한다.
    /// </summary>
    public sealed class PlayerNicknameSetupView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _nicknameInput;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TMP_Text _statusText;

        private bool _initialized;
        private bool _referencesValid;
        private bool _subscribed;

        public event Action<string> NicknameSubmitted;

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _referencesValid = ValidateRequiredReferences();
            if (_referencesValid)
            {
                _nicknameInput.characterLimit = LeaderboardConstants.MaxNicknameLength;
                SubscribeButton();
            }

            _initialized = true;
        }

        public void Render(LeaderboardViewState state)
        {
            Initialize();

            bool shouldShow = state?.IsVisible == true &&
                state.IsProfileRequired;
            gameObject.SetActive(shouldShow);

            if (!shouldShow || state == null)
            {
                return;
            }

            transform.SetAsLastSibling();

            if (_nicknameInput != null && !_nicknameInput.isFocused)
            {
                _nicknameInput.text = state.PlayerName;
            }

            if (_statusText != null)
            {
                _statusText.text = state.StatusMessage;
            }

            SetInteractable(_nicknameInput, !state.IsLoading);
            SetInteractable(_confirmButton, !state.IsLoading);
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (_subscribed && _confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            }
        }

        private bool ValidateRequiredReferences()
        {
            bool hasReferences =
                _nicknameInput != null &&
                _confirmButton != null &&
                _statusText != null;
            if (!hasReferences)
            {
                Debug.LogError(
                    "[PlayerNicknameSetupView] UI references must be wired in the inspector. " +
                    $"Missing: {BuildMissingReferenceSummary()}",
                    this);
            }

            return hasReferences;
        }

        private void SubscribeButton()
        {
            if (_subscribed || !_referencesValid)
            {
                return;
            }

            _confirmButton.onClick.AddListener(HandleConfirmClicked);
            _subscribed = true;
        }

        private void HandleConfirmClicked()
        {
            NicknameSubmitted?.Invoke(_nicknameInput?.text ?? string.Empty);
        }

        private string BuildMissingReferenceSummary()
        {
            var builder = new System.Text.StringBuilder();
            AppendMissing(builder, _nicknameInput != null, "Nickname Input");
            AppendMissing(builder, _confirmButton != null, "Confirm Button");
            AppendMissing(builder, _statusText != null, "Status Text");
            return builder.Length > 0 ? builder.ToString() : "none";
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

        private static void SetInteractable(
            Selectable selectable,
            bool interactable)
        {
            if (selectable != null)
            {
                selectable.interactable = interactable;
            }
        }
    }
}
