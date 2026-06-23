using System;
using System.Text;
using R3;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.RunGame.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    // MonoBehaviour는 클래스명과 같은 파일명에 있어야 Unity가 MonoScript를 만들어
    // 에디터에서 컴포넌트로 부착할 수 있다. (RunGameSceneRoot.cs에서 분리)
    public sealed class RunInventoryView : MonoBehaviour
    {
        private static readonly Color TabActiveColor = new(0.88f, 0.62f, 0.18f, 1f);
        private static readonly Color TabInactiveColor = new(0.18f, 0.18f, 0.24f, 1f);

        [SerializeField] private Button _openButton;
        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private Text _titleText;
        [SerializeField] private TMP_Text _titleTmpText;
        [SerializeField] private Text _summaryText;
        [SerializeField] private TMP_Text _summaryTmpText;
        [SerializeField] private Button _symbolTabButton;
        [SerializeField] private Text _symbolTabText;
        [SerializeField] private TMP_Text _symbolTabTmpText;
        [SerializeField] private Button _relicTabButton;
        [SerializeField] private Text _relicTabText;
        [SerializeField] private TMP_Text _relicTabTmpText;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Text _contentText;
        [SerializeField] private TMP_Text _contentTmpText;
        [SerializeField] private Button _closeButton;

        private bool _subscribed;
        private bool _reportedMissingReferences;

        public event Action OpenRequested;

        public event Action CloseRequested;

        public event Action SymbolTabRequested;

        public event Action RelicTabRequested;

        private void Awake()
        {
            EnsureRuntimeLayout();
        }

        /// <summary>
        /// 자기 ViewModel을 구독(상태→Render)하고 입력 event를 presenter로 연결한다(ADR-0020).
        /// </summary>
        public void Bind(RunInventoryViewModel viewModel, RunGameFlowController presenter)
        {
            if (viewModel == null || presenter == null)
            {
                return;
            }

            OpenRequested += presenter.HandleInventoryOpenRequested;
            CloseRequested += presenter.HandleInventoryCloseRequested;
            SymbolTabRequested += presenter.HandleInventorySymbolTabRequested;
            RelicTabRequested += presenter.HandleInventoryRelicTabRequested;

            viewModel.State.Subscribe(Render).AddTo(this);
        }

        private void OnDestroy()
        {
            UnsubscribeButtons();
        }

        public bool EnsureRuntimeLayout()
        {
            ResolveSceneReferences();
            EnsureOpenButton();

            if (!HasRequiredReferences())
            {
                if (!_reportedMissingReferences)
                {
                    Debug.LogError(
                        "[RunInventoryView] Required inventory UI objects must be placed in the hierarchy. " +
                        $"Missing: {BuildMissingReferenceSummary()}");
                    _reportedMissingReferences = true;
                }

                return false;
            }

            SubscribeButtons();
            return true;
        }

        public void Render(RunInventoryViewState state)
        {
            if (!EnsureRuntimeLayout())
            {
                return;
            }

            state ??= RunInventoryViewState.Empty;
            if (_panelRoot != null)
            {
                _panelRoot.gameObject.SetActive(state.IsOpen);
            }

            if (!state.IsOpen)
            {
                return;
            }

            transform.SetAsLastSibling();
            SetText(_titleText, "런 인벤토리");
            SetText(_titleTmpText, "런 인벤토리");
            SetText(_summaryText, state.Summary);
            SetText(_summaryTmpText, state.Summary);
            RenderTabs(state.ActiveTab);
            string content = BuildContentText(state);
            SetText(_contentText, content);
            SetText(_contentTmpText, content);

            if (_contentText != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    _contentText.rectTransform);
            }

            if (_contentTmpText != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    _contentTmpText.rectTransform);
            }

            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void ResolveSceneReferences()
        {
            _panelRoot ??= FindDeepChild(transform, "Run Inventory Panel") as RectTransform;
            _titleText ??= FindChildComponent<Text>("Run Inventory Title");
            _titleTmpText ??= FindChildComponent<TMP_Text>("Run Inventory Title");
            _summaryText ??= FindChildComponent<Text>("Run Inventory Summary");
            _summaryTmpText ??= FindChildComponent<TMP_Text>("Run Inventory Summary");
            _symbolTabButton ??= FindChildComponent<Button>("Symbol Pool Tab Button");
            _symbolTabText ??= FindChildComponent<Text>("Symbol Pool Tab Text");
            _symbolTabTmpText ??= FindChildComponent<TMP_Text>("Symbol Pool Tab Text");
            _relicTabButton ??= FindChildComponent<Button>("Relic Tab Button");
            _relicTabText ??= FindChildComponent<Text>("Relic Tab Text");
            _relicTabTmpText ??= FindChildComponent<TMP_Text>("Relic Tab Text");
            _scrollRect ??= FindChildComponent<ScrollRect>("Run Inventory Scroll");
            _contentText ??= FindChildComponent<Text>("Run Inventory Content");
            _contentTmpText ??= FindChildComponent<TMP_Text>("Run Inventory Content");
            _closeButton ??= FindChildComponent<Button>("Run Inventory Close Button");
        }

        private void EnsureOpenButton()
        {
            if (_openButton != null)
            {
                return;
            }

            Transform searchRoot = transform.root != null ? transform.root : transform;
            Transform origin =
                SceneComponentResolver.FindDeepChild(searchRoot, "Relic Inventory Origin");
            if (origin == null)
            {
                return;
            }

            _openButton = origin.GetComponent<Button>();
            Image image = origin.GetComponent<Image>();

            if (image != null && _openButton != null)
            {
                image.raycastTarget = true;
                _openButton.targetGraphic = image;
            }
        }

        private bool HasRequiredReferences()
        {
            return _openButton != null &&
                _panelRoot != null &&
                HasText(_titleText, _titleTmpText) &&
                HasText(_summaryText, _summaryTmpText) &&
                _symbolTabButton != null &&
                HasText(_symbolTabText, _symbolTabTmpText) &&
                _relicTabButton != null &&
                HasText(_relicTabText, _relicTabTmpText) &&
                _scrollRect != null &&
                HasText(_contentText, _contentTmpText) &&
                _closeButton != null;
        }

        private string BuildMissingReferenceSummary()
        {
            var builder = new StringBuilder();
            AppendMissing(builder, _openButton != null, "Relic Inventory Origin Button");
            AppendMissing(builder, _panelRoot != null, "Run Inventory Panel");
            AppendMissing(builder, HasText(_titleText, _titleTmpText), "Run Inventory Title");
            AppendMissing(builder, HasText(_summaryText, _summaryTmpText), "Run Inventory Summary");
            AppendMissing(builder, _symbolTabButton != null, "Symbol Pool Tab Button");
            AppendMissing(builder, HasText(_symbolTabText, _symbolTabTmpText), "Symbol Pool Tab Text");
            AppendMissing(builder, _relicTabButton != null, "Relic Tab Button");
            AppendMissing(builder, HasText(_relicTabText, _relicTabTmpText), "Relic Tab Text");
            AppendMissing(builder, _scrollRect != null, "Run Inventory Scroll");
            AppendMissing(builder, HasText(_contentText, _contentTmpText), "Run Inventory Content");
            AppendMissing(builder, _closeButton != null, "Run Inventory Close Button");
            return builder.Length > 0 ? builder.ToString() : "none";
        }

        private static void AppendMissing(
            StringBuilder builder,
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

        private void SubscribeButtons()
        {
            UnsubscribeButtons();

            if (_openButton != null)
            {
                _openButton.onClick.AddListener(HandleOpenClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_panelRoot != null)
            {
                Button backdropButton = _panelRoot.GetComponent<Button>();
                if (backdropButton != null)
                {
                    backdropButton.onClick.AddListener(HandleCloseClicked);
                }
            }

            if (_symbolTabButton != null)
            {
                _symbolTabButton.onClick.AddListener(HandleSymbolTabClicked);
            }

            if (_relicTabButton != null)
            {
                _relicTabButton.onClick.AddListener(HandleRelicTabClicked);
            }

            _subscribed = true;
        }

        private void UnsubscribeButtons()
        {
            if (!_subscribed)
            {
                return;
            }

            _openButton?.onClick.RemoveListener(HandleOpenClicked);
            _closeButton?.onClick.RemoveListener(HandleCloseClicked);
            if (_panelRoot != null)
            {
                Button backdropButton = _panelRoot.GetComponent<Button>();
                backdropButton?.onClick.RemoveListener(HandleCloseClicked);
            }

            _symbolTabButton?.onClick.RemoveListener(HandleSymbolTabClicked);
            _relicTabButton?.onClick.RemoveListener(HandleRelicTabClicked);
            _subscribed = false;
        }

        private void RenderTabs(RunInventoryTab activeTab)
        {
            bool symbolsActive = activeTab == RunInventoryTab.SymbolPool;
            SetButtonColor(_symbolTabButton, symbolsActive ? TabActiveColor : TabInactiveColor);
            SetButtonColor(_relicTabButton, symbolsActive ? TabInactiveColor : TabActiveColor);
            SetTextColor(_symbolTabText, symbolsActive ? Color.black : Color.white);
            SetTextColor(_symbolTabTmpText, symbolsActive ? Color.black : Color.white);
            SetTextColor(_relicTabText, symbolsActive ? Color.white : Color.black);
            SetTextColor(_relicTabTmpText, symbolsActive ? Color.white : Color.black);
        }

        private static string BuildContentText(RunInventoryViewState state)
        {
            return state.ActiveTab == RunInventoryTab.SymbolPool
                ? BuildSymbolContent(state)
                : BuildRelicContent(state);
        }

        private static string BuildSymbolContent(RunInventoryViewState state)
        {
            if (state.Symbols.Count == 0)
            {
                return "심볼 풀이 비어 있습니다.";
            }

            var builder = new StringBuilder();
            builder.AppendLine("현재 심볼 풀");
            builder.AppendLine();
            for (int index = 0; index < state.Symbols.Count; index++)
            {
                RunInventorySymbolViewState symbol = state.Symbols[index];
                builder.Append(symbol.DisplayName);
                builder.Append("  ");
                builder.Append(symbol.Count);
                builder.Append("개");
                builder.Append(symbol.IsHighProbability ? "  기본 고확률" : "  기본 저확률");
                if (index < state.Symbols.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string BuildRelicContent(RunInventoryViewState state)
        {
            if (state.Relics.Count == 0)
            {
                return "보유 유물이 없습니다.";
            }

            var builder = new StringBuilder();
            builder.Append("현재 유물 ");
            builder.Append(state.Relics.Count);
            builder.AppendLine("개");
            for (int index = 0; index < state.Relics.Count; index++)
            {
                RunInventoryRelicViewState relic = state.Relics[index];
                builder.AppendLine();
                builder.Append(index + 1);
                builder.Append(". ");
                builder.Append(relic.Name);
                builder.Append(" [");
                builder.Append(relic.Id);
                builder.Append("]");
                builder.AppendLine();
                builder.Append('[');
                builder.Append(relic.Grade);
                builder.Append(" · ");
                builder.Append(relic.Role);
                builder.AppendLine("]");
                builder.Append(relic.Description);
                if (index < state.Relics.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private void HandleOpenClicked()
        {
            OpenRequested?.Invoke();
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        private void HandleSymbolTabClicked()
        {
            SymbolTabRequested?.Invoke();
        }

        private void HandleRelicTabClicked()
        {
            RelicTabRequested?.Invoke();
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

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static bool HasText(Text text, TMP_Text tmpText)
        {
            return text != null || tmpText != null;
        }

        private static void SetTextColor(Text text, Color color)
        {
            if (text != null)
            {
                text.color = color;
            }
        }

        private static void SetTextColor(TMP_Text text, Color color)
        {
            if (text != null)
            {
                text.color = color;
            }
        }

        private static void SetButtonColor(Button button, Color color)
        {
            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = color;
            }
        }
    }
}
