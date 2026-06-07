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

        private StartRelicSelectViewModel _viewModel;

        // ── 기존 Editor 빌더용 Bind (유지) ──────────────────────────────

        public void Bind(Text summaryText, GameFlowOptionView[] artifactOptions)
        {
            _summaryText = summaryText;
            _artifactOptions = artifactOptions;
        }

        // ── IStartRelicSelectView (MVVM) ─────────────────────────────────

        public void Bind(StartRelicSelectViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void OnEnter()
        {
            gameObject.SetActive(true);
            if (_viewModel == null) return;

            _viewModel.Refresh();
            SetSummary(_viewModel.Summary);
            RebindOptions();
        }

        public void OnExit()
        {
            gameObject.SetActive(false);
        }

        // ── 내부 ────────────────────────────────────────────────────────

        public void SetSummary(string value)
        {
            if (_summaryText != null)
                _summaryText.text = value;
        }

        private void RebindOptions()
        {
            if (_viewModel == null || _artifactOptions == null) return;

            var artifacts = _viewModel.Artifacts;
            int visibleCount = Mathf.Min(_artifactOptions.Length, artifacts.Count);

            for (int i = 0; i < _artifactOptions.Length; i++)
            {
                GameFlowOptionView option = _artifactOptions[i];
                option.gameObject.SetActive(i < visibleCount);
                if (i >= visibleCount) continue;

                ArtifactDefinitionSO artifact = artifacts[i];
                option.SetText(artifact.DisplayName, artifact.Description);
                option.Button.onClick.RemoveAllListeners();

                string artifactId = artifact.ArtifactId;
                option.Button.onClick.AddListener(() => _viewModel.SelectArtifact(artifactId));
            }
        }
    }
}
