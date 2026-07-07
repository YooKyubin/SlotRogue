using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using R3;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.RunGame.ViewModels
{
    public sealed class RunInventoryViewModel
    {
        private RunInventoryTab _activeTab = RunInventoryTab.SymbolProbability;
        private bool _isDescriptionOpen;
        private bool _isRelicInventoryOpen;

        private readonly ReactiveProperty<RunInventoryViewState> _state =
            new(RunInventoryViewState.Empty);

        public ReadOnlyReactiveProperty<RunInventoryViewState> State => _state;

        public void Open()
        {
            OpenRelicInventory();
        }

        public void OpenDescription()
        {
            _isDescriptionOpen = true;
            _isRelicInventoryOpen = false;

            Publish();
        }

        public void OpenRelicInventory()
        {
            _isDescriptionOpen = false;
            _isRelicInventoryOpen = true;
            Publish();
        }

        public void Close()
        {
            if (!_isDescriptionOpen && !_isRelicInventoryOpen)
            {
                return;
            }

            _isDescriptionOpen = false;
            _isRelicInventoryOpen = false;
            Publish();
        }

        public void CloseDescription()
        {
            if (!_isDescriptionOpen)
            {
                return;
            }

            _isDescriptionOpen = false;
            Publish();
        }

        public void CloseRelicInventory()
        {
            if (!_isRelicInventoryOpen)
            {
                return;
            }

            _isRelicInventoryOpen = false;
            Publish();
        }

        public void SelectTab(RunInventoryTab tab)
        {
            if (tab != RunInventoryTab.SymbolProbability &&
                tab != RunInventoryTab.PatternDescription)
            {
                tab = RunInventoryTab.SymbolProbability;
            }

            if (_activeTab == tab && _isDescriptionOpen)
            {
                return;
            }

            _activeTab = tab;
            _isDescriptionOpen = true;
            _isRelicInventoryOpen = false;

            Publish();
        }

        public void Refresh()
        {
            Publish();
        }

        private void Publish()
        {
            // 런 보장은 진입 지점(RunGameSceneRoot.Awake / BattleSceneHost.BeginBattle)이
            // 담당한다. ViewModel은 상태 조회/발행만 하고 모델을 변경하지 않는다.
            _state.Value = new RunInventoryViewState(
                _isDescriptionOpen,
                _isRelicInventoryOpen,
                _activeTab,
                BuildSummary(),
                BuildSymbolItems(GameFlowSession.SlotPool),
                BuildPatternItems(),
                BuildRelicItems(GameFlowSession.OwnedRelics));
        }

        private static string BuildSummary()
        {
            return
                $"확률 합계 {GameFlowSession.SlotPool.TotalWeight}% · " +
                $"유물 {GameFlowSession.OwnedRelics.Count}개";
        }

        private static RunInventorySymbolViewState[] BuildSymbolItems(
            SlotSymbolPool pool)
        {
            IReadOnlyList<SlotSymbolType> symbols = SlotSymbolPool.ProbabilityDisplayOrder;
            var items = new RunInventorySymbolViewState[symbols.Count];
            for (int index = 0; index < symbols.Count; index++)
            {
                SlotSymbolType symbol = symbols[index];
                items[index] = new RunInventorySymbolViewState(
                    symbol,
                    RelicDisplay.SymbolKorean(symbol),
                    pool.GetWeight(symbol),
                    FormatProbability(pool, symbol),
                    SlotSymbolAttackValues.DamageFor(symbol));
            }

            return items;
        }

        private static string FormatProbability(
            SlotSymbolPool pool,
            SlotSymbolType symbol)
        {
            if (pool == null || pool.TotalWeight <= 0)
            {
                return "0%";
            }

            double percent = pool.GetWeight(symbol) * 100d / pool.TotalWeight;
            return percent.ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        private static RunInventoryPatternViewState[] BuildPatternItems()
        {
            IReadOnlyList<SlotPatternDefinition> definitions =
                SlotPatternCatalog.GetDefinitionsForDisplay();
            if (definitions == null || definitions.Count == 0)
            {
                return Array.Empty<RunInventoryPatternViewState>();
            }

            var items = new RunInventoryPatternViewState[definitions.Count];
            for (int index = 0; index < definitions.Count; index++)
            {
                SlotPatternDefinition definition = definitions[index];
                items[index] = new RunInventoryPatternViewState(
                    definition.PatternId,
                    definition.DisplayName,
                    definition.Multiplier,
                    definition.Rank.ToString());
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
        SymbolProbability = 0,
        PatternDescription = 1,
    }

    public sealed class RunInventoryViewState
    {
        public static readonly RunInventoryViewState Empty =
            new(
                false,
                false,
                RunInventoryTab.SymbolProbability,
                string.Empty,
                Array.Empty<RunInventorySymbolViewState>(),
                Array.Empty<RunInventoryPatternViewState>(),
                Array.Empty<RunInventoryRelicViewState>());

        private readonly RunInventorySymbolViewState[] _symbols;
        private readonly RunInventoryPatternViewState[] _patterns;
        private readonly RunInventoryRelicViewState[] _relics;

        public RunInventoryViewState(
            bool isOpen,
            RunInventoryTab activeTab,
            string summary,
            IReadOnlyList<RunInventorySymbolViewState> symbols,
            IReadOnlyList<RunInventoryPatternViewState> patterns,
            IReadOnlyList<RunInventoryRelicViewState> relics)
            : this(
                isOpen,
                false,
                activeTab,
                summary,
                symbols,
                patterns,
                relics)
        {
        }

        public RunInventoryViewState(
            bool isDescriptionOpen,
            bool isRelicInventoryOpen,
            RunInventoryTab activeTab,
            string summary,
            IReadOnlyList<RunInventorySymbolViewState> symbols,
            IReadOnlyList<RunInventoryPatternViewState> patterns,
            IReadOnlyList<RunInventoryRelicViewState> relics)
        {
            IsDescriptionOpen = isDescriptionOpen;
            IsRelicInventoryOpen = isRelicInventoryOpen;
            ActiveTab = activeTab;
            Summary = summary ?? string.Empty;
            _symbols = Copy(symbols);
            _patterns = Copy(patterns);
            _relics = Copy(relics);
            SymbolProbabilityText = BuildSymbolProbabilityText(_symbols);
        }

        public bool IsOpen => IsDescriptionOpen || IsRelicInventoryOpen;

        public bool IsDescriptionOpen { get; }

        public bool IsRelicInventoryOpen { get; }

        public RunInventoryTab ActiveTab { get; }

        public string Summary { get; }

        public string SymbolProbabilityText { get; }

        public IReadOnlyList<RunInventorySymbolViewState> Symbols => _symbols;

        public IReadOnlyList<RunInventoryPatternViewState> Patterns => _patterns;

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

        private static RunInventoryPatternViewState[] Copy(
            IReadOnlyList<RunInventoryPatternViewState> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<RunInventoryPatternViewState>();
            }

            var copy = new RunInventoryPatternViewState[source.Count];
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

        private static string BuildSymbolProbabilityText(
            IReadOnlyList<RunInventorySymbolViewState> symbols)
        {
            if (symbols == null || symbols.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            IReadOnlyList<SlotSymbolType> displayOrder =
                SlotSymbolPool.ProbabilityDisplayOrder;
            for (int index = 0; index < displayOrder.Count; index++)
            {
                if (index == 3)
                {
                    builder.AppendLine();
                }
                else if (index > 0)
                {
                    builder.Append("  ");
                }

                RunInventorySymbolViewState symbol =
                    FindSymbol(symbols, displayOrder[index]);
                builder
                    .Append(symbol.DisplayName)
                    .Append(' ')
                    .Append(symbol.ProbabilityText);
            }

            return builder.ToString();
        }

        private static RunInventorySymbolViewState FindSymbol(
            IReadOnlyList<RunInventorySymbolViewState> symbols,
            SlotSymbolType symbol)
        {
            for (int index = 0; index < symbols.Count; index++)
            {
                if (symbols[index].Symbol == symbol)
                {
                    return symbols[index];
                }
            }

            return new RunInventorySymbolViewState(
                symbol,
                RelicDisplay.SymbolKorean(symbol),
                0,
                "0%",
                0);
        }
    }

    public readonly struct RunInventorySymbolViewState
    {
        public RunInventorySymbolViewState(
            SlotSymbolType symbol,
            string displayName,
            int weight,
            string probabilityText)
            : this(
                symbol,
                displayName,
                weight,
                probabilityText,
                SlotSymbolAttackValues.DamageFor(symbol))
        {
        }

        public RunInventorySymbolViewState(
            SlotSymbolType symbol,
            string displayName,
            int weight,
            string probabilityText,
            int baseDamage)
        {
            Symbol = symbol;
            DisplayName = displayName ?? string.Empty;
            Weight = Math.Max(0, weight);
            ProbabilityText = probabilityText ?? string.Empty;
            BaseDamage = Math.Max(0, baseDamage);
        }

        public SlotSymbolType Symbol { get; }

        public string DisplayName { get; }

        public int Weight { get; }

        public int Count => Weight;

        public string ProbabilityText { get; }

        public int BaseDamage { get; }

        public string AttackPowerText => $"x{BaseDamage}";
    }

    public readonly struct RunInventoryPatternViewState
    {
        public RunInventoryPatternViewState(
            string id,
            string displayName,
            float multiplier,
            string rank)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Multiplier = multiplier;
            Rank = rank ?? string.Empty;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public float Multiplier { get; }

        public string Rank { get; }

        public string MultiplierText =>
            "x" + Multiplier.ToString("0.#", CultureInfo.InvariantCulture);
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
