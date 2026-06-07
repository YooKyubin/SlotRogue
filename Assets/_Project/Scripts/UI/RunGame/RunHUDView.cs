using SlotRogue.UI.RunGame.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// 런 내내 표시되는 공통 HUD View입니다.
    /// HP, 전투 횟수, 일시정지 버튼 등을 표시합니다.
    /// 화면 전환과 무관하게 항상 활성 상태를 유지합니다.
    /// </summary>
    public sealed class RunHUDView : MonoBehaviour, IRunHUDView
    {
        [SerializeField] private Text  _hpText;
        [SerializeField] private Text  _battleIndexText;
        [SerializeField] private Button _pauseButton;

        private RunHUDViewModel _viewModel;

        // ── IRunHUDView (MVVM) ───────────────────────────────────────────

        public void Bind(RunHUDViewModel viewModel)
        {
            if (_viewModel != null)
                _viewModel.Changed -= Render;

            _viewModel = viewModel;
            _viewModel.Changed += Render;

            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
                _pauseButton.onClick.AddListener(_viewModel.RequestPause);
            }

            Render();
        }

        /// <summary>HUD는 씬 내내 활성 상태. OnEnter/OnExit는 빈 구현입니다.</summary>
        public void OnEnter() { }
        public void OnExit()  { }

        // ── 렌더링 ──────────────────────────────────────────────────────

        private void Render()
        {
            if (_viewModel == null) return;

            if (_hpText != null)
                _hpText.text = $"{_viewModel.CurrentHp} / {_viewModel.MaxHp}";

            if (_battleIndexText != null)
                _battleIndexText.text = $"Battle {_viewModel.BattleIndex}";
        }

        private void OnDestroy()
        {
            if (_viewModel != null)
                _viewModel.Changed -= Render;
        }
    }
}
