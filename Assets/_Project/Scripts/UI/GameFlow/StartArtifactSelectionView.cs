using System;
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
