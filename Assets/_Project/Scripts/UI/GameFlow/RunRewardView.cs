using System;
using SlotRogue.UI.RunGame;
using SlotRogue.UI.RunGame.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunRewardView : MonoBehaviour, IRunRewardView
    {
        [SerializeField] private Text _summaryText;
        [SerializeField] private GameFlowOptionView[] _rewardOptions;

        [Header("광고 (스텁)")]
        [Tooltip("광고로 보상 3개 싹 바꾸기 — 모든 보상 화면에 표시")]
        [SerializeField] private Button _rerollButton;
        [Tooltip("광고로 보상 1개 더 — 엘리트/보스(큰 보상)에서만 표시")]
        [SerializeField] private Button _addRewardButton;

        public GameFlowOptionView[] RewardOptions => _rewardOptions;

        public event Action Entered;

        public event Action<int> RewardSelectionRequested;

        public event Action RerollRequested;

        public event Action ExtraRewardRequested;

        public void Bind(Text summaryText, GameFlowOptionView[] rewardOptions)
        {
            _summaryText   = summaryText;
            _rewardOptions = rewardOptions;
        }

        private void Awake()
        {
            WireAdButtons();
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
            UpdateAdButtonVisibility(state.IsBigReward);
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
        }

        private void OnRerollClicked()
        {
            RerollRequested?.Invoke();
        }

        private void OnAddRewardClicked()
        {
            ExtraRewardRequested?.Invoke();
        }

        private void UpdateAdButtonVisibility(bool isBigReward)
        {
            if (_addRewardButton != null)
            {
                _addRewardButton.gameObject.SetActive(isBigReward);
            }
        }

        private void RenderOptions(
            System.Collections.Generic.IReadOnlyList<RunRewardOptionViewState> options)
        {
            if (_rewardOptions == null)
            {
                return;
            }

            int optionCount = options != null ? options.Count : 0;
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
    }
}
