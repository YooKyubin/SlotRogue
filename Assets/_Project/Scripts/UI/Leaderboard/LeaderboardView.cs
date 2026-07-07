using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Leaderboard
{
    public sealed class LeaderboardView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Transform _rowsContainer;
        [SerializeField] private LeaderboardRankRowView _rowPrefab;
        [SerializeField] private LeaderboardRankRowView _goldRow;
        [SerializeField] private LeaderboardRankRowView _silverRow;
        [SerializeField] private LeaderboardRankRowView _bronzeRow;
        [SerializeField] private LeaderboardRankRowView _myRankRow;

        private readonly List<LeaderboardRankRowView> _rowPool = new();
        private bool _poolInitialized;
        private bool _subscribed;
        private bool _bound;

        public event Action CloseRequested;

        public void Bind(LeaderboardViewModel viewModel)
        {
            if (viewModel == null || _bound)
            {
                return;
            }

            _bound = true;
            CloseRequested += viewModel.Close;
            viewModel.State.Subscribe(Render).AddTo(this);
        }

        public bool EnsureRuntimeLayout()
        {
            if (!ValidateRequiredReferences())
            {
                Debug.LogError(
                    "[LeaderboardView] UI references must be wired in the inspector. " +
                    $"Missing: {BuildMissingReferenceSummary()}",
                    this);
                return false;
            }

            InitRowPool();
            SubscribeButtons();
            return true;
        }

        public void Render(LeaderboardViewState state)
        {
            if (!EnsureRuntimeLayout())
            {
                return;
            }

            bool isProfileRequired = state?.IsProfileRequired == true;
            bool isVisible = state?.IsVisible == true && !isProfileRequired;

            _panel.SetActive(isVisible);

            if (!isVisible || state == null)
            {
                return;
            }

            _panel.transform.SetAsLastSibling();
            RenderEntries(state.Entries);
        }

        private void RenderEntries(IReadOnlyList<LeaderboardEntryData> entries)
        {
            IReadOnlyList<LeaderboardEntryData> safe =
                entries ?? Array.Empty<LeaderboardEntryData>();

            RenderRow(_goldRow, safe, 0);
            RenderRow(_silverRow, safe, 1);
            RenderRow(_bronzeRow, safe, 2);

            int listCount = Mathf.Max(0, safe.Count - 3);
            EnsureRowPool(listCount);
            for (int poolIndex = 0; poolIndex < _rowPool.Count; poolIndex++)
            {
                if (poolIndex < listCount)
                {
                    _rowPool[poolIndex].Render(safe[poolIndex + 3]);
                }
                else
                {
                    _rowPool[poolIndex].Hide();
                }
            }

            RenderMyRank(safe);
        }

        private static void RenderRow(
            LeaderboardRankRowView row,
            IReadOnlyList<LeaderboardEntryData> entries,
            int index)
        {
            if (index < entries.Count)
            {
                row.Render(entries[index]);
            }
            else
            {
                row.Hide();
            }
        }

        private void RenderMyRank(IReadOnlyList<LeaderboardEntryData> entries)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].IsCurrentPlayer)
                {
                    _myRankRow.Render(entries[index]);
                    return;
                }
            }

            _myRankRow.Hide();
        }

        private void OnDestroy()
        {
            UnsubscribeButtons();
        }

        private bool ValidateRequiredReferences()
        {
            return _panel != null &&
                _closeButton != null &&
                _rowsContainer != null &&
                IsValidRow(_rowPrefab) &&
                IsValidRow(_goldRow) &&
                IsValidRow(_silverRow) &&
                IsValidRow(_bronzeRow) &&
                IsValidRow(_myRankRow);
        }

        private string BuildMissingReferenceSummary()
        {
            var builder = new System.Text.StringBuilder();
            AppendMissing(builder, _panel != null, "Leaderboard Panel");
            AppendMissing(builder, _closeButton != null, "Close Button");
            AppendMissing(builder, _rowsContainer != null, "Rows Container");
            AppendMissing(builder, IsValidRow(_rowPrefab), "Ranking Row Prefab");
            AppendMissing(builder, IsValidRow(_goldRow), "Gold Row");
            AppendMissing(builder, IsValidRow(_silverRow), "Silver Row");
            AppendMissing(builder, IsValidRow(_bronzeRow), "Bronze Row");
            AppendMissing(builder, IsValidRow(_myRankRow), "My Rank Row");
            return builder.Length > 0 ? builder.ToString() : "none";
        }

        private static bool IsValidRow(LeaderboardRankRowView row)
        {
            return row != null && row.IsValid;
        }

        private static void AppendMissing(
            System.Text.StringBuilder builder,
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

        private void InitRowPool()
        {
            if (_poolInitialized)
            {
                return;
            }

            _rowPrefab.Hide();
            _poolInitialized = true;
        }

        private void EnsureRowPool(int count)
        {
            if (count <= _rowPool.Count)
            {
                return;
            }

            while (_rowPool.Count < count)
            {
                LeaderboardRankRowView row = Instantiate(_rowPrefab, _rowsContainer);
                row.name = $"RankingPanel ({_rowPool.Count})";
                row.Hide();
                _rowPool.Add(row);
            }
        }

        private void SubscribeButtons()
        {
            if (_subscribed)
            {
                return;
            }

            _closeButton.onClick.AddListener(HandleCloseClicked);
            _subscribed = true;
        }

        private void UnsubscribeButtons()
        {
            if (!_subscribed)
            {
                return;
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            _subscribed = false;
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }
    }
}
