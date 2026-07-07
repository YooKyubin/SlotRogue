using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// 런 중 설정/일시정지 패널의 표시·게임시간 정지·버튼 입력을 담당하는 코드 전용 presenter다.
    /// UI 참조(패널·버튼)는 RunGameSceneRoot가 인스펙터에서 받아 주입하고, 흐름 처리(포기 등)는
    /// 콜백으로 위임한다. 조립자(SceneRoot)가 런타임 패널 제어와 Time.timeScale을 들지 않게 한다.
    /// </summary>
    public sealed class RunSettingsPresenter : IDisposable
    {
        private readonly GameObject _panel;
        private readonly Button _continueButton;
        private readonly Button _giveUpButton;
        private readonly Action _onPauseRequested;
        private readonly Action _onGiveUpRequested;

        private bool _isTimePaused;
        private float _timeScaleBeforePause = 1f;
        private bool _wired;

        public RunSettingsPresenter(
            GameObject panel,
            Button continueButton,
            Button giveUpButton,
            Action onPauseRequested,
            Action onGiveUpRequested)
        {
            _panel = panel;
            _continueButton = continueButton;
            _giveUpButton = giveUpButton;
            _onPauseRequested = onPauseRequested;
            _onGiveUpRequested = onGiveUpRequested;

            WireButtons();
            if (_panel != null && _panel.activeSelf)
            {
                _panel.SetActive(false);
            }
        }

        /// <summary>일시정지 요청 시(HUD 일시정지 버튼) 호출된다. 게임시간을 멈추고 패널을 띄운다.</summary>
        public void Open()
        {
            _onPauseRequested?.Invoke();

            if (_panel == null)
            {
                Debug.LogError("[RunSettingsPresenter] SettingPanel must be wired in the inspector.");
                return;
            }

            PauseTime();
            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
        }

        /// <summary>계속하기(패널 닫기). 게임시간을 복구한다.</summary>
        public void Close()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }

            ResumeTime();
        }

        public void Dispose()
        {
            ResumeTime();
            UnwireButtons();
        }

        private void HandleGiveUp()
        {
            Close();
            _onGiveUpRequested?.Invoke();
        }

        private void PauseTime()
        {
            if (_isTimePaused)
            {
                return;
            }

            _timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            _isTimePaused = true;
        }

        private void ResumeTime()
        {
            if (!_isTimePaused)
            {
                return;
            }

            Time.timeScale = _timeScaleBeforePause;
            _isTimePaused = false;
        }

        private void WireButtons()
        {
            if (_wired)
            {
                return;
            }

            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(Close);
                _continueButton.onClick.AddListener(Close);
            }

            if (_giveUpButton != null)
            {
                _giveUpButton.onClick.RemoveListener(HandleGiveUp);
                _giveUpButton.onClick.AddListener(HandleGiveUp);
            }

            _wired = true;
        }

        private void UnwireButtons()
        {
            if (!_wired)
            {
                return;
            }

            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(Close);
            }

            if (_giveUpButton != null)
            {
                _giveUpButton.onClick.RemoveListener(HandleGiveUp);
            }

            _wired = false;
        }
    }
}
