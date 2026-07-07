using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.RunGame.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    public sealed class RunDescriptionView : ViewComponentBase
    {
        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Button _symbolTabButton;
        [SerializeField] private Button _patternTabButton;

        [Header("Tab Sprites")]
        [SerializeField] private Sprite _symbolTabActiveSprite;
        [SerializeField] private Sprite _symbolTabInactiveSprite;
        [SerializeField] private Sprite _patternTabActiveSprite;
        [SerializeField] private Sprite _patternTabInactiveSprite;

        [SerializeField] private Button _closeButton;

        [SerializeField] private Transform _symbolPanel;
        [SerializeField] private Transform _patternPanel;
        [SerializeField] private RunDescriptionRowView[] _symbolRowViews;
        [SerializeField] private RunDescriptionRowView[] _patternRowViews;

        private readonly List<RunDescriptionRow> _symbolRows = new();
        private readonly List<RunDescriptionRow> _patternRows = new();
        private bool _rowsInitialized;
        private bool _buttonsSubscribed;
        private bool _stateSubscribed;
        private bool _reportedMissingReferences;
        private int _iconVersion;

        private AddressableSpriteProvider _symbolIconProvider;
        private CancellationTokenSource _iconCts;

        public event Action CloseRequested;

        public event Action SymbolTabRequested;

        public event Action PatternTabRequested;

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
            _symbolIconProvider?.Dispose();
            _symbolIconProvider = null;
        }

        public void Bind(RunInventoryViewModel viewModel, IRunGameFlow presenter)
        {
            if (viewModel == null || presenter == null || _stateSubscribed)
            {
                return;
            }

            EnsureRuntimeLayout();
            CloseRequested += presenter.HandleDescriptionCloseRequested;
            SymbolTabRequested += presenter.HandleInventorySymbolTabRequested;
            PatternTabRequested += presenter.HandleInventoryPatternTabRequested;
            viewModel.State.Subscribe(Render).AddTo(this);
            _stateSubscribed = true;
        }

        public bool EnsureRuntimeLayout()
        {
            if (!HasRequiredReferences())
            {
                if (!_reportedMissingReferences)
                {
                    Debug.LogError(
                        "[RunDescriptionView] Desc panel requires hierarchy references. " +
                        $"Missing: {BuildMissingReferenceSummary()}");
                    _reportedMissingReferences = true;
                }

                return false;
            }

            InitRows();
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
            bool isOpen = state.IsDescriptionOpen;
            if (gameObject.activeSelf != isOpen)
            {
                gameObject.SetActive(isOpen);
            }

            SetActive(_panelRoot, isOpen);
            if (!isOpen)
            {
                return;
            }

            transform.SetAsLastSibling();
            bool showPatterns = state.ActiveTab == RunInventoryTab.PatternDescription;
            SetText(_titleText, showPatterns ? "패턴" : "심볼");
            RenderTabs(state.ActiveTab);
            SetActive(_symbolPanel, !showPatterns);
            SetActive(_patternPanel, showPatterns);

            if (showPatterns)
            {
                PopulatePatternRows(state.Patterns);
            }
            else
            {
                PopulateSymbolRows(state.Symbols);
            }
        }

        private bool HasRequiredReferences()
        {
            return _panelRoot != null &&
                _titleText != null &&
                _symbolTabButton != null &&
                _patternTabButton != null &&
                _closeButton != null &&
                _symbolPanel != null &&
                _patternPanel != null &&
                HasRows(_symbolRowViews) &&
                HasRows(_patternRowViews);
        }

        private string BuildMissingReferenceSummary()
        {
            var builder = new System.Text.StringBuilder();
            AppendMissing(builder, _panelRoot != null, "Desc Panel");
            AppendMissing(builder, _titleText != null, "Title Text");
            AppendMissing(builder, _symbolTabButton != null, "Symbol Tab Button");
            AppendMissing(builder, _patternTabButton != null, "Pattern Tab Button");
            AppendMissing(builder, _closeButton != null, "Close Button");
            AppendMissing(builder, _symbolPanel != null, "Symbol Desc Panel");
            AppendMissing(builder, _patternPanel != null, "Pattern Desc Panel");
            AppendMissing(builder, HasRows(_symbolRowViews), "Symbol Desc Rows");
            AppendMissing(builder, HasRows(_patternRowViews), "Pattern Desc Rows");
            return builder.Length > 0 ? builder.ToString() : "none";
        }

        private static bool HasRows(RunDescriptionRowView[] rows)
        {
            return rows != null && rows.Length > 0 && rows[0] != null;
        }

        private void InitRows()
        {
            if (_rowsInitialized)
            {
                return;
            }

            _symbolRows.Clear();
            _patternRows.Clear();
            CollectRows(_symbolRowViews, _symbolRows);
            CollectRows(_patternRowViews, _patternRows);
            _rowsInitialized = true;
        }

        private static void CollectRows(
            RunDescriptionRowView[] rowViews,
            List<RunDescriptionRow> rows)
        {
            if (rowViews == null || rows == null)
            {
                return;
            }

            for (int index = 0; index < rowViews.Length; index++)
            {
                RunDescriptionRowView rowView = rowViews[index];
                if (rowView == null)
                {
                    continue;
                }

                RunDescriptionRow row = RunDescriptionRow.FromView(rowView);
                rows.Add(row);
                row.SetActive(false);
            }
        }

        private static void EnsureRowPool(
            List<RunDescriptionRow> rows,
            Transform parent,
            string rowNamePrefix,
            int count)
        {
            if (rows == null || parent == null || count <= rows.Count)
            {
                return;
            }

            if (rows.Count == 0)
            {
                return;
            }

            GameObject template = rows[0].Root;
            while (rows.Count < count)
            {
                GameObject clone = Instantiate(template, template.transform.parent);
                clone.name = $"{rowNamePrefix} ({rows.Count})";
                rows.Add(RunDescriptionRow.Resolve(clone));
            }
        }

        private void PopulateSymbolRows(
            IReadOnlyList<RunInventorySymbolViewState> symbols)
        {
            EnsureRowPool(
                _symbolRows,
                _symbolPanel,
                "Symbol Content",
                symbols?.Count ?? 0);
            _iconVersion++;
            _iconCts ??= new CancellationTokenSource();

            for (int index = 0; index < _symbolRows.Count; index++)
            {
                RunDescriptionRow row = _symbolRows[index];
                if (symbols == null || index >= symbols.Count)
                {
                    row.SetActive(false);
                    continue;
                }

                RunInventorySymbolViewState symbol = symbols[index];
                row.SetActive(true);
                row.SetTitle(symbol.DisplayName);
                row.SetAttackPower(symbol.AttackPowerText);
                row.SetProbability(symbol.ProbabilityText);
                row.SetMultiplier(string.Empty);
                ApplySymbolIcon(row, symbol);
            }
        }

        private void PopulatePatternRows(
            IReadOnlyList<RunInventoryPatternViewState> patterns)
        {
            EnsureRowPool(
                _patternRows,
                _patternPanel,
                "Pattern Content",
                patterns?.Count ?? 0);

            for (int index = 0; index < _patternRows.Count; index++)
            {
                RunDescriptionRow row = _patternRows[index];
                if (patterns == null || index >= patterns.Count)
                {
                    row.SetActive(false);
                    continue;
                }

                RunInventoryPatternViewState pattern = patterns[index];
                row.SetActive(true);
                row.SetTitle(pattern.DisplayName);
                row.SetAttackPower(string.Empty);
                row.SetProbability(string.Empty);
                row.SetMultiplier(pattern.MultiplierText);
            }
        }

        private void RenderTabs(RunInventoryTab activeTab)
        {
            bool symbolsActive = activeTab == RunInventoryTab.SymbolProbability;
            ApplyTabSprite(
                _symbolTabButton,
                symbolsActive ? _symbolTabActiveSprite : _symbolTabInactiveSprite);
            ApplyTabSprite(
                _patternTabButton,
                symbolsActive ? _patternTabInactiveSprite : _patternTabActiveSprite);
        }

        private void SubscribeButtons()
        {
            if (_buttonsSubscribed)
            {
                return;
            }

            _closeButton.onClick.AddListener(HandleCloseClicked);
            _symbolTabButton.onClick.AddListener(HandleSymbolTabClicked);
            _patternTabButton.onClick.AddListener(HandlePatternTabClicked);
            _buttonsSubscribed = true;
        }

        private void UnsubscribeButtons()
        {
            if (!_buttonsSubscribed)
            {
                return;
            }

            _closeButton?.onClick.RemoveListener(HandleCloseClicked);
            _symbolTabButton?.onClick.RemoveListener(HandleSymbolTabClicked);
            _patternTabButton?.onClick.RemoveListener(HandlePatternTabClicked);
            _buttonsSubscribed = false;
        }

        private void HandleCloseClicked() => CloseRequested?.Invoke();

        private void HandleSymbolTabClicked() => SymbolTabRequested?.Invoke();

        private void HandlePatternTabClicked() => PatternTabRequested?.Invoke();

        private void ApplySymbolIcon(
            RunDescriptionRow row,
            RunInventorySymbolViewState symbol)
        {
            row.SetIcon(null);
            string key = SlotSymbolIconKeys.For(symbol.Symbol);
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            LoadIconAsync(
                row,
                key,
                SymbolProvider(),
                _iconVersion,
                _iconCts.Token).Forget();
        }

        private AddressableSpriteProvider SymbolProvider()
        {
            return _symbolIconProvider ??= new AddressableSpriteProvider(string.Empty);
        }

        private async UniTaskVoid LoadIconAsync(
            RunDescriptionRow row,
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

            if (version == _iconVersion && sprite != null)
            {
                row.SetIcon(sprite);
            }
        }

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
    }
}
