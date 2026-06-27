using System;
using R3;
using SlotRogue.UI.RunGame.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// 런 내내 표시되는 공통 HUD View입니다.
    /// HP, 전투 횟수, 일시정지 버튼 등을 표시합니다.
    /// 화면 전환과 무관하게 항상 활성 상태를 유지합니다.
    /// </summary>
    public sealed class RunHUDView : ViewComponentBase, IRunHUDView
    {
        [SerializeField] private TMP_Text _hpTmpText;
        [SerializeField] private TMP_Text _battleIndexTmpText;
        [SerializeField] private Text     _hpText;
        [SerializeField] private Text     _battleIndexText;
        [SerializeField] private Button   _pauseButton;

        private bool _subscribed;

        public event Action PauseRequested;

        private void Awake()
        {
            EnsureRuntimeLayout();
            SubscribeButton();
        }

        /// <summary>
        /// 자기 ViewModel을 구독(상태→Render)하고 일시정지 입력을 ViewModel command로 연결한다(ADR-0020).
        /// </summary>
        public void Bind(RunHUDViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            EnsureRuntimeLayout();
            SubscribeButton();
            PauseRequested += viewModel.RequestPause;
            viewModel.State.Subscribe(Render).AddTo(this);
        }

        public void OnEnter() { }
        public void OnExit()  { }

        public void Render(RunHUDViewState state)
        {
            EnsureRuntimeLayout();

            string hpLabel = $"{state.CurrentHp} / {state.MaxHp}";
            if (_hpTmpText != null)
            {
                _hpTmpText.text = hpLabel;
            }
            else if (_hpText != null)
            {
                _hpText.text = hpLabel;
            }

            string battleIndexLabel = $"WAVE {Mathf.Max(1, state.BattleIndex)}";
            if (_battleIndexTmpText != null)
            {
                _battleIndexTmpText.text = battleIndexLabel;
            }
            else if (_battleIndexText != null)
            {
                _battleIndexText.text = battleIndexLabel;
            }
        }

        public bool EnsureRuntimeLayout()
        {
            Transform searchRoot = transform.root != null ? transform.root : transform;
            Transform waveText = FindDeepChild(transform, "Wave Text") ??
                FindDeepChild(searchRoot, "Wave Text");
            Transform hpText = FindDeepChild(transform, "HP Text");

            if (_battleIndexTmpText == null)
            {
                _battleIndexTmpText = waveText != null ? waveText.GetComponent<TMP_Text>() : null;
            }

            if (_battleIndexText == null)
            {
                _battleIndexText = waveText != null ? waveText.GetComponent<Text>() : null;
            }

            if (_hpTmpText == null)
            {
                _hpTmpText = hpText != null ? hpText.GetComponent<TMP_Text>() : null;
            }

            if (_hpText == null)
            {
                _hpText = hpText != null ? hpText.GetComponent<Text>() : null;
            }

            if (_pauseButton == null)
            {
                _pauseButton = FindDeepChild(searchRoot, "Pause Button")?.GetComponent<Button>();
            }

            return _battleIndexTmpText != null || _battleIndexText != null || _pauseButton != null;
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
