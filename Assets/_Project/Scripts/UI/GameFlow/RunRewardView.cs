using System;
using R3;
using SlotRogue.UI.RunGame;
using SlotRogue.UI.RunGame.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunRewardView : MonoBehaviour, IRunRewardView
    {
        [SerializeField] private Text _summaryText;
        [SerializeField] private GameFlowOptionView[] _rewardOptions;

        [Header("Ads")]
        [Tooltip("Rerolls all reward options after a rewarded ad.")]
        [SerializeField] private Button _rerollButton;
        [SerializeField] private TMP_Text _rerollButtonText;
        [Tooltip("Adds one reward for elite or boss rewards.")]
        [SerializeField] private Button _addRewardButton;
        [SerializeField] private TMP_Text _addRewardButtonText;
        [Tooltip("Doubles the selected reward once.")]
        [SerializeField] private Button _doubleRewardButton;
        [SerializeField] private TMP_Text _doubleRewardButtonText;

        private bool _reportedMissingAdButtons;
        private bool _reportedMissingRewardOptions;

        public GameFlowOptionView[] RewardOptions => _rewardOptions;

        public event Action Entered;

        public event Action<int> RewardSelectionRequested;

        public event Action RerollRequested;

        public event Action ExtraRewardRequested;

        public event Action RewardDoubleRequested;

        public void Bind(Text summaryText, GameFlowOptionView[] rewardOptions)
        {
            _summaryText   = summaryText;
            _rewardOptions = rewardOptions;
        }

        /// <summary>
        /// 자기 ViewModel을 구독(상태→Render)하고 입력 event를 presenter로 연결한다.
        /// R3 구독은 .AddTo(this)로 이 View 파괴 시 자동 해제된다(ADR-0020).
        /// </summary>
        public void Bind(
            RunRewardViewModel viewModel,
            RunGameFlowController presenter,
            RelicIconRenderer iconRenderer)
        {
            if (viewModel == null || presenter == null)
            {
                return;
            }

            Entered += presenter.HandleRewardEntered;
            RewardSelectionRequested += presenter.HandleRewardSelectionRequested;
            RerollRequested += presenter.HandleRewardRerollRequested;
            ExtraRewardRequested += presenter.HandleExtraRewardRequested;
            RewardDoubleRequested += presenter.HandleRewardDoubleRequested;

            viewModel.State
                .Subscribe(state =>
                {
                    Render(state);
                    iconRenderer?.RenderRewardIcons(this, state);
                })
                .AddTo(this);
        }

        private void Awake()
        {
            ResolveAdButtonReferences();
            WireAdButtons();
        }

        private void OnEnable()
        {
            ResolveAdButtonReferences();
            WireAdButtons();
        }

        private void OnDestroy()
        {
            UnwireAdButtons();
        }

        public void OnEnter()
        {
            gameObject.SetActive(true);
            Entered?.Invoke();
        }

        public void OnExit()
        {
            gameObject.SetActive(false);
        }

        public void Render(RunRewardViewState state)
        {
            if (state == null)
            {
                return;
            }

            if (_summaryText != null)
            {
                _summaryText.text = state.Summary;
            }

            RenderOptions(state.Options);
            if (_rerollButton != null)
            {
                _rerollButton.interactable = state.CanReroll;
            }

            if (_rerollButtonText != null)
            {
                _rerollButtonText.text = state.RerollLabel;
            }

            if (_addRewardButton != null)
            {
                _addRewardButton.interactable = state.CanAddReward;
            }

            if (_addRewardButtonText != null)
            {
                _addRewardButtonText.text = state.AddRewardLabel;
            }

            if (_doubleRewardButton != null)
            {
                _doubleRewardButton.interactable = state.CanDoubleReward;
            }

            if (_doubleRewardButtonText != null)
            {
                _doubleRewardButtonText.text = state.DoubleRewardLabel;
            }

            UpdateAdButtonVisibility(state);
        }

        private void ResolveAdButtonReferences()
        {
            _rerollButton ??= FindChildComponent<Button>("Reroll Button");
            _rerollButtonText ??=
                FindChildComponent<TMP_Text>("Reroll Text") ??
                _rerollButton?.GetComponentInChildren<TMP_Text>(includeInactive: true);
            _addRewardButton ??= FindChildComponent<Button>("Add Reward Button");
            _doubleRewardButton ??= FindChildComponent<Button>("Double Reward Button");
            _addRewardButtonText ??=
                FindChildComponent<TMP_Text>("Add Reward Text") ??
                _addRewardButton?.GetComponentInChildren<TMP_Text>(includeInactive: true);
            _doubleRewardButtonText ??=
                FindChildComponent<TMP_Text>("Double Reward Text") ??
                _doubleRewardButton?.GetComponentInChildren<TMP_Text>(includeInactive: true);

            ReportMissingAdButtonReferences();
        }

        private void ReportMissingAdButtonReferences()
        {
            if (_reportedMissingAdButtons ||
                (_rerollButton != null &&
                _rerollButtonText != null &&
                _addRewardButton != null &&
                _addRewardButtonText != null &&
                _doubleRewardButton != null &&
                _doubleRewardButtonText != null))
            {
                return;
            }

            Debug.LogError(
                "[RunRewardView] Reward ad buttons must be placed in the hierarchy. " +
                $"Missing: {BuildMissingAdButtonSummary()}");
            _reportedMissingAdButtons = true;
        }

        private string BuildMissingAdButtonSummary()
        {
            var builder = new System.Text.StringBuilder();
            AppendMissing(builder, _rerollButton != null, "Reroll Button");
            AppendMissing(builder, _rerollButtonText != null, "Reroll Text");
            AppendMissing(builder, _addRewardButton != null, "Add Reward Button");
            AppendMissing(builder, _addRewardButtonText != null, "Add Reward Text");
            AppendMissing(builder, _doubleRewardButton != null, "Double Reward Button");
            AppendMissing(builder, _doubleRewardButtonText != null, "Double Reward Text");
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

        private void WireAdButtons()
        {
            if (_rerollButton != null)
            {
                _rerollButton.onClick.RemoveListener(OnRerollClicked);
                _rerollButton.onClick.AddListener(OnRerollClicked);
            }

            if (_addRewardButton != null)
            {
                _addRewardButton.onClick.RemoveListener(OnAddRewardClicked);
                _addRewardButton.onClick.AddListener(OnAddRewardClicked);
            }

            if (_doubleRewardButton != null)
            {
                _doubleRewardButton.onClick.RemoveListener(OnDoubleRewardClicked);
                _doubleRewardButton.onClick.AddListener(OnDoubleRewardClicked);
            }
        }

        private void UnwireAdButtons()
        {
            _rerollButton?.onClick.RemoveListener(OnRerollClicked);
            _addRewardButton?.onClick.RemoveListener(OnAddRewardClicked);
            _doubleRewardButton?.onClick.RemoveListener(OnDoubleRewardClicked);
        }

        private void OnRerollClicked()
        {
            Debug.Log("[RunRewardView] Rewarded reroll clicked.");
            RerollRequested?.Invoke();
        }

        private void OnAddRewardClicked()
        {
            ExtraRewardRequested?.Invoke();
        }

        private void OnDoubleRewardClicked()
        {
            RewardDoubleRequested?.Invoke();
        }

        private void UpdateAdButtonVisibility(RunRewardViewState state)
        {
            if (_addRewardButton != null)
            {
                _addRewardButton.gameObject.SetActive(state.IsBigReward);
            }

            if (_doubleRewardButton != null)
            {
                _doubleRewardButton.gameObject.SetActive(true);
            }
        }

        private T FindChildComponent<T>(string objectName) where T : Component
        {
            Transform child = FindDeepChild(transform, objectName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static Transform FindDeepChild(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == objectName)
            {
                return parent;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform found = FindDeepChild(parent.GetChild(index), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void RenderOptions(
            System.Collections.Generic.IReadOnlyList<RunRewardOptionViewState> options)
        {
            if (_rewardOptions == null)
            {
                return;
            }

            int optionCount = options != null ? options.Count : 0;
            ReportMissingRewardOptions(optionCount);
            int visibleCount = Mathf.Min(_rewardOptions.Length, optionCount);

            for (int index = 0; index < _rewardOptions.Length; index++)
            {
                GameFlowOptionView optionView = _rewardOptions[index];
                if (optionView == null)
                {
                    continue;
                }

                optionView.gameObject.SetActive(index < visibleCount);
                if (index >= visibleCount)
                {
                    continue;
                }

                RunRewardOptionViewState option = options[index];
                optionView.SetText(option.Title, option.Description);
                optionView.Button.onClick.RemoveAllListeners();

                int optionIndex = option.Index;
                optionView.Button.onClick.AddListener(
                    () => RewardSelectionRequested?.Invoke(optionIndex));
            }
        }

        private void ReportMissingRewardOptions(int requiredCount)
        {
            if (_rewardOptions == null ||
                requiredCount <= _rewardOptions.Length ||
                _reportedMissingRewardOptions)
            {
                return;
            }

            Debug.LogError(
                "[RunRewardView] Reward option views must be placed in the hierarchy. " +
                $"Required: {requiredCount}, Placed: {_rewardOptions.Length}");
            _reportedMissingRewardOptions = true;
        }
    }
}
