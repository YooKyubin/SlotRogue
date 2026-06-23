using System;
using System.Collections.Generic;
using R3;
using SlotRogue.Relics.Pool;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.RunGame.ViewModels
{
    /// <summary>
    /// 시작 유물 선택 화면의 상태와 커맨드.
    /// v23: grade=Starter 유물(6종)만 제시하고, 선택 시 보유 유물에 추가한다.
    /// MonoBehaviour가 아닌 순수 C# 클래스.
    /// </summary>
    public sealed class StartRelicSelectViewModel
    {
        private const int StarterOptionCount = 3;
        private readonly Random _rng;
        private RelicDefinition[] _relics = Array.Empty<RelicDefinition>();
        private readonly ReactiveProperty<StartRelicSelectViewState> _state =
            new(StartRelicSelectViewState.Empty);

        public StartRelicSelectViewModel(Random rng = null)
        {
            _rng = rng ?? new Random();
        }

        public event Action RelicSelected;

        public ReadOnlyReactiveProperty<StartRelicSelectViewState> State => _state;

        public void SelectRelic(string relicId)
        {
            RelicDefinition relic = RelicCatalog.GetById(relicId);
            if (!GameFlowSession.SelectStarterRelic(relic))
            {
                return;
            }

            RelicSelected?.Invoke();
        }

        public void Refresh()
        {
            _relics = RollStarterOptions();
            var options = new StartRelicOptionViewState[_relics.Length];
            for (int index = 0; index < _relics.Length; index++)
            {
                RelicDefinition relic = _relics[index];
                options[index] = new StartRelicOptionViewState(
                    relic.Id,
                    relic.Name,
                    RelicDisplay.BuildSelectionDescription(relic),
                    relic.IconKey);
            }

            _state.Value = new StartRelicSelectViewState(GameFlowSession.BuildSummary(), options);
        }

        private RelicDefinition[] RollStarterOptions()
        {
            var pool = new List<RelicDefinition>(RelicCatalog.Starters);
            int count = Math.Min(StarterOptionCount, pool.Count);
            var options = new RelicDefinition[count];

            for (int index = 0; index < count; index++)
            {
                int selectedIndex = _rng.Next(pool.Count);
                options[index] = pool[selectedIndex];
                pool.RemoveAt(selectedIndex);
            }

            return options;
        }
    }

    public sealed class StartRelicSelectViewState
    {
        public static readonly StartRelicSelectViewState Empty =
            new(string.Empty, Array.Empty<StartRelicOptionViewState>());

        private readonly StartRelicOptionViewState[] _options;

        public StartRelicSelectViewState(
            string summary,
            IReadOnlyList<StartRelicOptionViewState> options)
        {
            Summary = summary ?? string.Empty;
            _options = Copy(options);
        }

        public string Summary { get; }

        public IReadOnlyList<StartRelicOptionViewState> Options => _options;

        private static StartRelicOptionViewState[] Copy(
            IReadOnlyList<StartRelicOptionViewState> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<StartRelicOptionViewState>();
            }

            var copy = new StartRelicOptionViewState[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }
    }

    public readonly struct StartRelicOptionViewState
    {
        public StartRelicOptionViewState(
            string id,
            string title,
            string description,
            string iconKey)
        {
            Id = id ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            IconKey = iconKey ?? string.Empty;
        }

        public string Id { get; }

        public string Title { get; }

        public string Description { get; }

        public string IconKey { get; }
    }
}
