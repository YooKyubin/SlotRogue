using System;
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

        public event Action PauseRequested;

        private void Awake()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(HandlePauseClicked);
            }
        }

        public void OnEnter() { }
        public void OnExit()  { }

        public void Render(RunHUDViewState state)
        {
            if (_hpText != null)
            {
                _hpText.text = $"{state.CurrentHp} / {state.MaxHp}";
            }

            if (_battleIndexText != null)
            {
                _battleIndexText.text = $"Battle {state.BattleIndex}";
            }
        }

        private void OnDestroy()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveListener(HandlePauseClicked);
            }
        }

        private void HandlePauseClicked()
        {
            PauseRequested?.Invoke();
        }
    }
}
