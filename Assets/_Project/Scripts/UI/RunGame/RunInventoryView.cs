using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.RunGame.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// 런 인벤토리 그리드 View(TMP 전용). 심볼/유물 탭을 셀 프리팹 풀로 채우고,
    /// 셀 선택 시 상세 패널(Tier/Name/Desc)을 갱신한다. 선택은 View 내부 상태,
    /// open/close/tab 입력은 presenter로 전달한다(ADR-0020).
    /// </summary>
    public sealed class RunInventoryView : ViewComponentBase
    {
        private const string OpenButtonSlotId = "battle/presentation/relic-inventory-origin";
        [SerializeField] private Button _openButton;
        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Button _symbolTabButton;
        [SerializeField] private TMP_Text _symbolTabText;
        [SerializeField] private Button _relicTabButton;
        [SerializeField] private TMP_Text _relicTabText;

        [Header("Tab Sprites")]
        [SerializeField] private Sprite _symbolTabActiveSprite;
        [SerializeField] private Sprite _symbolTabInactiveSprite;
        [SerializeField] private Sprite _relicTabActiveSprite;
        [SerializeField] private Sprite _relicTabInactiveSprite;

        [SerializeField] private Button _closeButton;

        [Header("Cells")]
        [Tooltip("Scroll View > Viewport > Content")]
        [SerializeField] private Transform _cellsContainer;
        [Tooltip("셀 프리팹(Frame). 비우면 Content의 첫 Frame을 템플릿으로 사용.")]
        [SerializeField] private GameObject _cellPrefab;

        [Header("Detail Panel")]
        [Tooltip("설명 패널 루트. 평소엔 숨기고, 항목을 클릭하면 표시한다. 비우면 'Inventory Desc Panel'을 자동 탐색.")]
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private TMP_Text _detailTierText;
        [SerializeField] private TMP_Text _detailNameText;
        [SerializeField] private TMP_Text _detailDescText;

        private readonly List<InventoryCell> _cellPool = new();
        private GameObject _cellTemplate;
        private bool _poolInitialized;
        private bool _subscribed;
        private Button _subscribedOpenButton;
        private bool _reportedMissingReferences;
        private int _selectedIndex = -1;
        private int _iconVersion;

        private AddressableSpriteProvider _relicIconProvider;
        private AddressableSpriteProvider _symbolIconProvider;
        private CancellationTokenSource _iconCts;
        private RunInventoryViewState _lastState = RunInventoryViewState.Empty;

        public event Action OpenRequested;

        public event Action CloseRequested;

        public event Action SymbolTabRequested;

        public event Action RelicTabRequested;

        private void Awake()
        {
            EnsureRuntimeLayout();
        }

        private void OnDestroy()
        {
            UnsubscribeButtons();
            _iconCts?.Cancel();
            _iconCts?.Dispose();
            _iconCts = null;
            _relicIconProvider?.Dispose();
            _relicIconProvider = null;
            _symbolIconProvider?.Dispose();
            _symbolIconProvider = null;
        }

        /// <summary>자기 ViewModel을 구독하고 open/close/tab 입력을 presenter로 연결한다(ADR-0020).</summary>
        public void Bind(RunInventoryViewModel viewModel, IRunGameFlow presenter)
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

        public bool EnsureRuntimeLayout()
        {
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

            InitCellPool();
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
            _lastState = state;

            // 인벤토리 루트가 비활성으로 저작돼 있어도 열릴 때 활성화한다(닫히면 숨김).
            if (gameObject.activeSelf != state.IsOpen)
            {
                gameObject.SetActive(state.IsOpen);
            }

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
            RenderTabs(state.ActiveTab);
            PopulateCells(state);
        }

        // ── 셀 채우기 ────────────────────────────────────────────────────

        private void PopulateCells(RunInventoryViewState state)
        {
            bool isRelic = state.ActiveTab == RunInventoryTab.Relics;
            int count = isRelic ? state.Relics.Count : state.Symbols.Count;

            EnsureCellPool(count);
            _iconVersion++;
            _iconCts ??= new CancellationTokenSource();

            for (int index = 0; index < _cellPool.Count; index++)
            {
                InventoryCell cell = _cellPool[index];
                if (index >= count)
                {
                    cell.SetActive(false);
                    continue;
                }

                cell.SetActive(true);
                cell.SetHighlight(false);
                int captured = index;
                cell.SetClick(() => Select(captured));

                if (isRelic)
                {
                    RunInventoryRelicViewState relic = state.Relics[index];
                    ApplyIcon(cell, relic.IconKey, RelicProvider(), _iconVersion);
                }
                else
                {
                    RunInventorySymbolViewState symbol = state.Symbols[index];
                    ApplyIcon(
                        cell,
                        SlotSymbolIconKeys.For(symbol.Symbol),
                        SymbolProvider(),
                        _iconVersion);
                }
            }

            // 설명 패널은 평소 숨김. 항목을 클릭하면 그때 표시한다(기본 선택 없음).
            _selectedIndex = -1;
            ClearDetail();
        }

        private void Select(int index)
        {
            bool isRelic = _lastState.ActiveTab == RunInventoryTab.Relics;
            int count = isRelic ? _lastState.Relics.Count : _lastState.Symbols.Count;
            if (index < 0 || index >= count)
            {
                return;
            }

            _selectedIndex = index;
            SetActive(_detailPanel, true);
            for (int cellIndex = 0; cellIndex < _cellPool.Count; cellIndex++)
            {
                _cellPool[cellIndex].SetHighlight(cellIndex == index);
            }

            if (isRelic)
            {
                RunInventoryRelicViewState relic = _lastState.Relics[index];
                SetText(_detailTierText, relic.Grade);
                SetText(_detailNameText, relic.Name);
                SetText(_detailDescText, relic.Description);
            }
            else
            {
                RunInventorySymbolViewState symbol = _lastState.Symbols[index];
                SetText(_detailTierText, symbol.IsHighProbability ? "고확률" : "저확률");
                SetText(_detailNameText, symbol.DisplayName);
                SetText(
                    _detailDescText,
                    $"슬롯 풀 보유 {symbol.Count}개\n기본 {(symbol.IsHighProbability ? "고확률" : "저확률")} 심볼입니다.");
            }
        }

        private void ClearDetail()
        {
            SetActive(_detailPanel, false);
            SetText(_detailTierText, string.Empty);
            SetText(_detailNameText, string.Empty);
            SetText(_detailDescText, string.Empty);
        }

        private AddressableSpriteProvider RelicProvider()
        {
            return _relicIconProvider ??= new AddressableSpriteProvider(RelicIconKeys.Default);
        }

        private AddressableSpriteProvider SymbolProvider()
        {
            return _symbolIconProvider ??= new AddressableSpriteProvider(string.Empty);
        }

        private void ApplyIcon(
            InventoryCell cell,
            string key,
            AddressableSpriteProvider provider,
            int version)
        {
            cell.SetIcon(null);
            if (string.IsNullOrEmpty(key) || provider == null)
            {
                return;
            }

            LoadIconAsync(cell, key, provider, version, _iconCts.Token).Forget();
        }

        private async UniTaskVoid LoadIconAsync(
            InventoryCell cell,
            string key,
            AddressableSpriteProvider provider,
            int version,
            CancellationToken cancellationToken)
        {
            Sprite sprite;
            try
            {
                sprite = await provider.LoadAsync(key, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // 로드 중 탭이 바뀌었으면(버전 불일치) 버린다.
            if (version == _iconVersion && sprite != null)
            {
                cell.SetIcon(sprite);
            }
        }

        // ── 셀 풀 ────────────────────────────────────────────────────────

        private void InitCellPool()
        {
            if (_poolInitialized)
            {
                return;
            }

            _cellTemplate = ResolveCellTemplate();
            if (_cellsContainer != null)
            {
                for (int index = 0; index < _cellsContainer.childCount; index++)
                {
                    _cellsContainer.GetChild(index).gameObject.SetActive(false);
                }
            }

            _poolInitialized = true;
        }

        private GameObject ResolveCellTemplate()
        {
            if (_cellPrefab != null)
            {
                return _cellPrefab;
            }

            if (_cellsContainer == null)
            {
                return null;
            }

            for (int index = 0; index < _cellsContainer.childCount; index++)
            {
                Transform child = _cellsContainer.GetChild(index);
                if (ContainsOrdinalIgnoreCase(child.name, "Frame") ||
                    ContainsOrdinalIgnoreCase(child.name, "Cell") ||
                    ContainsOrdinalIgnoreCase(child.name, "Slot"))
                {
                    return child.gameObject;
                }
            }

            return _cellsContainer.childCount > 0
                ? _cellsContainer.GetChild(0).gameObject
                : null;
        }

        private void EnsureCellPool(int count)
        {
            if (count <= _cellPool.Count)
            {
                return;
            }

            if (_cellTemplate == null || _cellsContainer == null)
            {
                Debug.LogError(
                    "[RunInventoryView] Cell prefab/template was not found; cannot build the grid.");
                return;
            }

            while (_cellPool.Count < count)
            {
                GameObject clone = Instantiate(_cellTemplate, _cellsContainer);
                clone.name = $"Frame ({_cellPool.Count})";
                _cellPool.Add(InventoryCell.Resolve(clone));
            }
        }

        // ── 참조 해석 / 버튼 ─────────────────────────────────────────────

        private void EnsureOpenButton()
        {
            if (_openButton != null)
            {
                return;
            }

            _openButton = FindOpenButtonInScene();
            if (_openButton == null)
            {
                return;
            }

            Image image = _openButton.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                _openButton.targetGraphic = image;
            }
        }

        private bool HasRequiredReferences()
        {
            // 열기 버튼은 선택 사항이다. 인벤토리 열기는 외부(HUD의 Inventory Button → presenter)가
            // ViewModel.Open으로 구동하므로, View 자체 열기 버튼이 없어도 패널은 동작한다.
            return _panelRoot != null &&
                _symbolTabButton != null &&
                _relicTabButton != null &&
                _cellsContainer != null &&
                _closeButton != null;
        }

        private string BuildMissingReferenceSummary()
        {
            var builder = new System.Text.StringBuilder();
            AppendMissing(builder, _panelRoot != null, "Inventory Panel");
            AppendMissing(builder, _symbolTabButton != null, "Symbol Pool Tab Button");
            AppendMissing(builder, _relicTabButton != null, "Relic Tab Button");
            AppendMissing(builder, _cellsContainer != null, "Content");
            AppendMissing(builder, _closeButton != null, "Inventory Close Button");
            return builder.Length > 0 ? builder.ToString() : "none";
        }

        private void RenderTabs(RunInventoryTab activeTab)
        {
            bool symbolsActive = activeTab == RunInventoryTab.SymbolPool;
            ApplyTabSprite(
                _symbolTabButton,
                symbolsActive ? _symbolTabActiveSprite : _symbolTabInactiveSprite);
            ApplyTabSprite(
                _relicTabButton,
                symbolsActive ? _relicTabInactiveSprite : _relicTabActiveSprite);
            SetTextColor(_symbolTabText, Color.white);
            SetTextColor(_relicTabText, Color.white);
        }

        private void SubscribeButtons()
        {
            SubscribeOpenButton();

            if (_subscribed)
            {
                return;
            }

            _closeButton?.onClick.AddListener(HandleCloseClicked);
            _symbolTabButton?.onClick.AddListener(HandleSymbolTabClicked);
            _relicTabButton?.onClick.AddListener(HandleRelicTabClicked);
            _subscribed = true;
        }

        private void UnsubscribeButtons()
        {
            if (_subscribedOpenButton != null)
            {
                _subscribedOpenButton.onClick.RemoveListener(HandleOpenClicked);
                _subscribedOpenButton = null;
            }

            if (!_subscribed)
            {
                return;
            }

            _closeButton?.onClick.RemoveListener(HandleCloseClicked);
            _symbolTabButton?.onClick.RemoveListener(HandleSymbolTabClicked);
            _relicTabButton?.onClick.RemoveListener(HandleRelicTabClicked);
            _subscribed = false;
        }

        private void SubscribeOpenButton()
        {
            if (_openButton == null || _subscribedOpenButton == _openButton)
            {
                return;
            }

            if (_subscribedOpenButton != null)
            {
                _subscribedOpenButton.onClick.RemoveListener(HandleOpenClicked);
            }

            _openButton.onClick.RemoveListener(HandleOpenClicked);
            _openButton.onClick.AddListener(HandleOpenClicked);
            _subscribedOpenButton = _openButton;
        }

        private void HandleOpenClicked() => OpenRequested?.Invoke();

        private void HandleCloseClicked() => CloseRequested?.Invoke();

        private void HandleSymbolTabClicked() => SymbolTabRequested?.Invoke();

        private void HandleRelicTabClicked() => RelicTabRequested?.Invoke();

        private static void ApplyTabSprite(Button tabButton, Sprite sprite)
        {
            if (tabButton == null ||
                sprite == null ||
                tabButton.targetGraphic is not Image image)
            {
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.enabled = true;
        }

        private static bool ContainsOrdinalIgnoreCase(string value, string part)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private Button FindOpenButtonInScene()
        {
            Transform root = transform.root != null ? transform.root : transform;
            Button button = FindOpenButton(root);
            if (button != null)
            {
                return button;
            }

            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                button = FindOpenButton(roots[index].transform);
                if (button != null)
                {
                    return button;
                }
            }

            return null;
        }

        private static Button FindOpenButton(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            GameFlowImageSlot[] slots =
                root.GetComponentsInChildren<GameFlowImageSlot>(includeInactive: true);
            for (int index = 0; index < slots.Length; index++)
            {
                GameFlowImageSlot slot = slots[index];
                if (slot != null && slot.SlotId == OpenButtonSlotId)
                {
                    return ResolveButton(slot.transform);
                }
            }

            return null;
        }

        private static Button ResolveButton(Transform target)
        {
            return target.GetComponent<Button>() ??
                target.GetComponentInChildren<Button>(includeInactive: true) ??
                target.GetComponentInParent<Button>(includeInactive: true);
        }

        // 셀(Frame): Button(클릭) + Icon(Image) + Highlight(선택 표시).
        private sealed class InventoryCell
        {
            private readonly GameObject _root;
            private readonly Button _button;
            private readonly Image _icon;
            private readonly GameObject _highlight;

            private InventoryCell(GameObject root, Button button, Image icon, GameObject highlight)
            {
                _root = root;
                _button = button;
                _icon = icon;
                _highlight = highlight;
            }

            internal static InventoryCell Resolve(GameObject root)
            {
                Transform t = root.transform;
                Button button = root.GetComponent<Button>() ??
                    root.GetComponentInChildren<Button>(includeInactive: true);
                Image icon = FindChild<Image>(t, "Icon");
                GameObject highlight = FindChild<Transform>(t, "Highlight")?.gameObject;
                return new InventoryCell(root, button, icon, highlight);
            }

            internal void SetActive(bool active)
            {
                if (_root != null)
                {
                    _root.SetActive(active);
                }
            }

            internal void SetIcon(Sprite sprite)
            {
                if (_icon == null)
                {
                    return;
                }

                _icon.sprite = sprite;
                _icon.enabled = sprite != null;
            }

            internal void SetHighlight(bool on)
            {
                if (_highlight != null)
                {
                    _highlight.SetActive(on);
                }
            }

            internal void SetClick(Action action)
            {
                if (_button == null)
                {
                    return;
                }

                _button.onClick.RemoveAllListeners();
                if (action != null)
                {
                    _button.onClick.AddListener(() => action());
                }
            }

            private static T FindChild<T>(Transform root, string name) where T : Component
            {
                Transform[] all = root.GetComponentsInChildren<Transform>(includeInactive: true);
                for (int index = 0; index < all.Length; index++)
                {
                    if (string.Equals(all[index].name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return all[index].GetComponent<T>();
                    }
                }

                return null;
            }
        }
    }
}
