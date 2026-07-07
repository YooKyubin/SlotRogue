using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using SlotRogue.Relics.Pool;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.RunGame.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    public sealed class RunRelicInventoryView : ViewComponentBase
    {
        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private Button _closeButton;

        [Header("Cells")]
        [SerializeField] private Transform _cellsContainer;
        [SerializeField] private GameObject _cellPrefab;

        private readonly List<RunInventoryCell> _cellPool = new();
        private GameObject _cellTemplate;
        private bool _poolInitialized;
        private bool _buttonsSubscribed;
        private bool _stateSubscribed;
        private bool _reportedMissingReferences;
        private int _iconVersion;

        private AddressableSpriteProvider _relicIconProvider;
        private CancellationTokenSource _iconCts;

        public event Action CloseRequested;

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
        }

        public void Bind(RunInventoryViewModel viewModel, IRunGameFlow presenter)
        {
            if (viewModel == null || presenter == null || _stateSubscribed)
            {
                return;
            }

            EnsureRuntimeLayout();
            CloseRequested += presenter.HandleInventoryCloseRequested;
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
                        "[RunRelicInventoryView] Relic inventory requires hierarchy references. " +
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
            bool isOpen = state.IsRelicInventoryOpen;
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
            PopulateRelics(state.Relics);
        }

        private bool HasRequiredReferences()
        {
            return _panelRoot != null &&
                _cellsContainer != null &&
                _closeButton != null &&
                _cellPrefab != null;
        }

        private string BuildMissingReferenceSummary()
        {
            var builder = new System.Text.StringBuilder();
            AppendMissing(builder, _panelRoot != null, "Inventory Panel");
            AppendMissing(builder, _cellsContainer != null, "Content");
            AppendMissing(builder, _closeButton != null, "Close Button");
            AppendMissing(builder, _cellPrefab != null, "Cell Prefab");
            return builder.Length > 0 ? builder.ToString() : "none";
        }

        private void InitCellPool()
        {
            if (_poolInitialized)
            {
                return;
            }

            _cellTemplate = _cellPrefab;
            if (_cellsContainer != null)
            {
                for (int index = 0; index < _cellsContainer.childCount; index++)
                {
                    _cellsContainer.GetChild(index).gameObject.SetActive(false);
                }
            }

            _poolInitialized = true;
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
                    "[RunRelicInventoryView] Cell prefab/template was not found.");
                return;
            }

            while (_cellPool.Count < count)
            {
                GameObject clone = Instantiate(_cellTemplate, _cellsContainer);
                clone.name = $"Frame ({_cellPool.Count})";
                _cellPool.Add(RunInventoryCell.Resolve(clone));
            }
        }

        private void PopulateRelics(
            IReadOnlyList<RunInventoryRelicViewState> relics)
        {
            int count = relics?.Count ?? 0;
            EnsureCellPool(count);
            _iconVersion++;
            _iconCts ??= new CancellationTokenSource();

            for (int index = 0; index < _cellPool.Count; index++)
            {
                RunInventoryCell cell = _cellPool[index];
                if (index >= count)
                {
                    cell.SetActive(false);
                    continue;
                }

                cell.SetActive(true);
                cell.SetHighlight(false);
                cell.SetClick(null);

                RunInventoryRelicViewState relic = relics[index];
                ApplyRelicIcon(cell, relic.IconKey);
                cell.SetRelicText(relic.Name, relic.Description);
            }
        }

        private void SubscribeButtons()
        {
            if (_buttonsSubscribed)
            {
                return;
            }

            _closeButton.onClick.AddListener(HandleCloseClicked);
            _buttonsSubscribed = true;
        }

        private void UnsubscribeButtons()
        {
            if (!_buttonsSubscribed)
            {
                return;
            }

            _closeButton?.onClick.RemoveListener(HandleCloseClicked);
            _buttonsSubscribed = false;
        }

        private void HandleCloseClicked() => CloseRequested?.Invoke();

        private void ApplyRelicIcon(RunInventoryCell cell, string key)
        {
            cell.SetIcon(null);
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            LoadIconAsync(
                cell,
                key,
                RelicProvider(),
                _iconVersion,
                _iconCts.Token).Forget();
        }

        private AddressableSpriteProvider RelicProvider()
        {
            return _relicIconProvider ??=
                new AddressableSpriteProvider(RelicIconKeys.Default);
        }

        private async UniTaskVoid LoadIconAsync(
            RunInventoryCell cell,
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
                cell.SetIcon(sprite);
            }
        }

    }
}
