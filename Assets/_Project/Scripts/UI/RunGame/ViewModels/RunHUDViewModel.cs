using System;
using System.Collections.Generic;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.RunGame.ViewModels
{
    /// <summary>
    /// 런 내내 표시되는 공통 HUD 상태를 담습니다.
    /// HP·Gold·Round·Pause 버튼 등 어느 View에서도 항상 보이는 정보입니다.
    /// 순수 C# 클래스입니다.
    /// </summary>
    public sealed class RunHUDViewModel
    {
        public RunHUDViewModel()
        {
            State = RunHUDViewState.Empty;
        }

        public event Action<RunHUDViewState> Changed;

        public event Action PauseRequested;

        public RunHUDViewState State { get; private set; }

        public void Refresh()
        {
            State = new RunHUDViewState(
                GameFlowSession.PlayerCurrentHp,
                GameFlowSession.PlayerMaxHp,
                GameFlowSession.CurrentBattleNumber,
                GameFlowSession.Victories);
            Changed?.Invoke(State);
        }

        public void RequestPause()
        {
            PauseRequested?.Invoke();
        }
    }

    public readonly struct RunHUDViewState
    {
        public static readonly RunHUDViewState Empty = new(0, 1, 0, 0);

        public RunHUDViewState(int currentHp, int maxHp, int battleIndex, int victories)
        {
            CurrentHp = currentHp;
            MaxHp = maxHp;
            BattleIndex = battleIndex;
            Victories = victories;
        }

        public int CurrentHp { get; }

        public int MaxHp { get; }

        public int BattleIndex { get; }

        public int Victories { get; }
    }

    public sealed class RunInventoryViewModel
    {
        private RunInventoryTab _activeTab = RunInventoryTab.SymbolPool;
        private bool _isOpen;

        public RunInventoryViewModel()
        {
            State = RunInventoryViewState.Empty;
        }

        public event Action<RunInventoryViewState> Changed;

        public RunInventoryViewState State { get; private set; }

        public void Open()
        {
            _isOpen = true;
            Publish();
        }

        public void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            Publish();
        }

        public void SelectTab(RunInventoryTab tab)
        {
            if (_activeTab == tab && _isOpen)
            {
                return;
            }

            _activeTab = tab;
            Publish();
        }

        public void Refresh()
        {
            Publish();
        }

        private void Publish()
        {
            GameFlowSession.EnsureRunStarted();

            State = new RunInventoryViewState(
                _isOpen,
                _activeTab,
                BuildSummary(),
                BuildSymbolItems(GameFlowSession.SlotPool),
                BuildRelicItems(GameFlowSession.OwnedRelics));
            Changed?.Invoke(State);
        }

        private static string BuildSummary()
        {
            return
                $"심볼 {GameFlowSession.SlotPool.Total}개 · " +
                $"유물 {GameFlowSession.OwnedRelics.Count}개";
        }

        private static RunInventorySymbolViewState[] BuildSymbolItems(
            SlotSymbolPool pool)
        {
            IReadOnlyList<SlotSymbolType> symbols = SlotSymbolPool.Symbols;
            var items = new RunInventorySymbolViewState[symbols.Count];
            for (int index = 0; index < symbols.Count; index++)
            {
                SlotSymbolType symbol = symbols[index];
                items[index] = new RunInventorySymbolViewState(
                    symbol,
                    RelicDisplay.SymbolKorean(symbol),
                    pool.GetCount(symbol),
                    SlotSymbolPool.IsHighProbability(symbol));
            }

            return items;
        }

        private static RunInventoryRelicViewState[] BuildRelicItems(
            IReadOnlyList<RelicDefinition> relics)
        {
            if (relics == null || relics.Count == 0)
            {
                return Array.Empty<RunInventoryRelicViewState>();
            }

            var items = new RunInventoryRelicViewState[relics.Count];
            for (int index = 0; index < relics.Count; index++)
            {
                RelicDefinition relic = relics[index];
                items[index] = new RunInventoryRelicViewState(
                    relic.Id,
                    relic.Name,
                    RelicDisplay.GradeKorean(relic.Grade),
                    RelicDisplay.RoleKorean(relic.Role),
                    RelicDisplay.BuildDescription(relic),
                    relic.IconKey);
            }

            return items;
        }
    }

    public enum RunInventoryTab
    {
        SymbolPool = 0,
        Relics = 1,
    }

    public sealed class RunInventoryViewState
    {
        public static readonly RunInventoryViewState Empty =
            new(
                false,
                RunInventoryTab.SymbolPool,
                string.Empty,
                Array.Empty<RunInventorySymbolViewState>(),
                Array.Empty<RunInventoryRelicViewState>());

        private readonly RunInventorySymbolViewState[] _symbols;
        private readonly RunInventoryRelicViewState[] _relics;

        public RunInventoryViewState(
            bool isOpen,
            RunInventoryTab activeTab,
            string summary,
            IReadOnlyList<RunInventorySymbolViewState> symbols,
            IReadOnlyList<RunInventoryRelicViewState> relics)
        {
            IsOpen = isOpen;
            ActiveTab = activeTab;
            Summary = summary ?? string.Empty;
            _symbols = Copy(symbols);
            _relics = Copy(relics);
        }

        public bool IsOpen { get; }

        public RunInventoryTab ActiveTab { get; }

        public string Summary { get; }

        public IReadOnlyList<RunInventorySymbolViewState> Symbols => _symbols;

        public IReadOnlyList<RunInventoryRelicViewState> Relics => _relics;

        private static RunInventorySymbolViewState[] Copy(
            IReadOnlyList<RunInventorySymbolViewState> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<RunInventorySymbolViewState>();
            }

            var copy = new RunInventorySymbolViewState[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }

        private static RunInventoryRelicViewState[] Copy(
            IReadOnlyList<RunInventoryRelicViewState> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<RunInventoryRelicViewState>();
            }

            var copy = new RunInventoryRelicViewState[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }
    }

    public readonly struct RunInventorySymbolViewState
    {
        public RunInventorySymbolViewState(
            SlotSymbolType symbol,
            string displayName,
            int count,
            bool isHighProbability)
        {
            Symbol = symbol;
            DisplayName = displayName ?? string.Empty;
            Count = Math.Max(0, count);
            IsHighProbability = isHighProbability;
        }

        public SlotSymbolType Symbol { get; }

        public string DisplayName { get; }

        public int Count { get; }

        public bool IsHighProbability { get; }
    }

    public readonly struct RunInventoryRelicViewState
    {
        public RunInventoryRelicViewState(
            string id,
            string name,
            string grade,
            string role,
            string description,
            string iconKey)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Grade = grade ?? string.Empty;
            Role = role ?? string.Empty;
            Description = description ?? string.Empty;
            IconKey = iconKey ?? string.Empty;
        }

        public string Id { get; }

        public string Name { get; }

        public string Grade { get; }

        public string Role { get; }

        public string Description { get; }

        public string IconKey { get; }
    }
}
