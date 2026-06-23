using System;
using R3;
using SlotRogue.UI.RunGame;
using SlotRogue.UI.RunGame.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class StartArtifactSelectionView : MonoBehaviour, IStartRelicSelectView
    {
        [SerializeField] private Text _summaryText;
        [SerializeField] private GameFlowOptionView[] _artifactOptions;

        public GameFlowOptionView[] ArtifactOptions => _artifactOptions;

        public event Action Entered;

        public event Action<string> RelicSelectionRequested;

        public void Bind(Text summaryText, GameFlowOptionView[] artifactOptions)
        {
            _summaryText = summaryText;
            _artifactOptions = artifactOptions;
        }

        /// <summary>
        /// 자기 ViewModel을 구독(상태→Render)하고 입력 event를 presenter로 연결한다.
        /// R3 구독은 .AddTo(this)로 이 View 파괴 시 자동 해제된다(ADR-0020).
        /// </summary>
        public void Bind(
            StartRelicSelectViewModel viewModel,
            RunGameFlowController presenter,
            RelicIconRenderer iconRenderer)
        {
            if (viewModel == null || presenter == null)
            {
                return;
            }

            Entered += presenter.HandleStartRelicEntered;
            RelicSelectionRequested += presenter.HandleStarterRelicSelectionRequested;

            viewModel.State
                .Subscribe(state =>
                {
                    Render(state);
                    iconRenderer?.RenderStartRelicIcons(this, state);
                })
                .AddTo(this);
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

        public void Render(StartRelicSelectViewState state)
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
        }

        private void RenderOptions(
            System.Collections.Generic.IReadOnlyList<StartRelicOptionViewState> options)
        {
            if (_artifactOptions == null)
            {
                return;
            }

            int optionCount = options != null ? options.Count : 0;
            int visibleCount = Mathf.Min(_artifactOptions.Length, optionCount);

            for (int index = 0; index < _artifactOptions.Length; index++)
            {
                GameFlowOptionView optionView = _artifactOptions[index];
                if (optionView == null)
                {
                    continue;
                }

                optionView.gameObject.SetActive(index < visibleCount);
                if (index >= visibleCount)
                {
                    continue;
                }

                StartRelicOptionViewState option = options[index];
                optionView.SetText(option.Title, option.Description);
                optionView.Button.onClick.RemoveAllListeners();

                string relicId = option.Id;
                optionView.Button.onClick.AddListener(
                    () => RelicSelectionRequested?.Invoke(relicId));
            }
        }
    }
}
