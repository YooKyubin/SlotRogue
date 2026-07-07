using System;
using R3;
using SlotRogue.UI.RunGame.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// 런 내내 표시되는 상단 HUD View. 현재 웨이브(전투 번호)와 일시정지 버튼을 담당한다.
    /// 화면 전환과 무관하게 항상 활성 상태를 유지한다.
    /// </summary>
    public sealed class RunWaveView : ViewComponentBase, IRunWaveView
    {
        [SerializeField] private TMP_Text _battleIndexTmpText;
        [SerializeField] private Button _pauseButton;

        private bool _subscribed;
        private bool _reportedMissingReferences;
        private bool _bound;

        public event Action PauseRequested;

        private void Awake()
        {
            EnsureRuntimeLayout();
            SubscribeButton();
        }

        /// <summary>자기 ViewModel을 구독(상태→Render)하고 일시정지 입력을 command로 연결한다(ADR-0020).</summary>
        public void Bind(RunHUDViewModel viewModel)
        {
            if (viewModel == null || _bound)
            {
                return;
            }

            _bound = true;
            EnsureRuntimeLayout();
            SubscribeButton();
            PauseRequested += viewModel.RequestPause;
            viewModel.State.Subscribe(Render).AddTo(this);
        }

        public void OnEnter() { }
        public void OnExit() { }

        public void Render(RunHUDViewState state)
        {
            EnsureRuntimeLayout();

            if (_battleIndexTmpText != null)
            {
                _battleIndexTmpText.text = $"WAVE {Mathf.Max(1, state.BattleIndex)}";
            }
        }

        public bool EnsureRuntimeLayout()
        {
            if (!HasRequiredReferences())
            {
                if (!_reportedMissingReferences)
                {
                    Debug.LogError(
                        "[RunWaveView] Wave HUD references must be wired in the inspector. " +
                        $"Missing: {BuildMissingReferenceSummary()}");
                    _reportedMissingReferences = true;
                }

                return false;
            }

            return true;
        }

        private bool HasRequiredReferences()
        {
            return _battleIndexTmpText != null && _pauseButton != null;
        }

        private string BuildMissingReferenceSummary()
        {
            var builder = new System.Text.StringBuilder();
            AppendMissing(builder, _battleIndexTmpText != null, "Wave Text");
            AppendMissing(builder, _pauseButton != null, "Pause Button");
            return builder.Length > 0 ? builder.ToString() : "none";
        }

        private void OnDestroy()
        {
            if (_subscribed && _pauseButton != null)
            {
                _pauseButton.onClick.RemoveListener(HandlePauseClicked);
            }

            _subscribed = false;
        }

        private void SubscribeButton()
        {
            if (_subscribed || _pauseButton == null)
            {
                return;
            }

            _pauseButton.onClick.AddListener(HandlePauseClicked);
            _subscribed = true;
        }

        private void HandlePauseClicked()
        {
            PauseRequested?.Invoke();
        }
    }
}
