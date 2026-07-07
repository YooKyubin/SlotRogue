using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using R3;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.RunGame.ViewModels
{
    /// <summary>
    /// 런 내내 표시되는 공통 HUD 상태를 담습니다.
    /// HP·Gold·Round·Pause 버튼 등 어느 View에서도 항상 보이는 정보입니다.
    /// 순수 C# 클래스입니다.
    /// </summary>
    public sealed class RunHUDViewModel : IDisposable
    {
        private readonly ReactiveProperty<RunHUDViewState> _state =
            new(RunHUDViewState.Empty);

        public RunHUDViewModel()
        {
            GameFlowSession.RunCoinsChanged += HandleRunCoinsChanged;
        }

        public event Action PauseRequested;

        public ReadOnlyReactiveProperty<RunHUDViewState> State => _state;

        public void Refresh()
        {
            _state.Value = new RunHUDViewState(
                GameFlowSession.CurrentBattleNumber,
                GameFlowSession.RunCoins,
                GameFlowSession.Victories,
                BuildSymbolProbabilityText(GameFlowSession.SlotPool));
        }

        public void RequestPause()
        {
            PauseRequested?.Invoke();
        }

        public void Dispose()
        {
            GameFlowSession.RunCoinsChanged -= HandleRunCoinsChanged;
        }

        private void HandleRunCoinsChanged(int _)
        {
            Refresh();
        }

        private static string BuildSymbolProbabilityText(SlotSymbolPool pool)
        {
            int total = pool != null ? pool.TotalWeight : 0;
            IReadOnlyList<SlotSymbolType> symbols =
                SlotSymbolPool.ProbabilityDisplayOrder;
            var builder = new StringBuilder();

            for (int index = 0; index < symbols.Count; index++)
            {
                if (index == 3)
                {
                    builder.AppendLine();
                }
                else if (index > 0)
                {
                    builder.Append("  ");
                }

                SlotSymbolType symbol = symbols[index];
                double percent = total > 0
                    ? pool.GetWeight(symbol) * 100d / total
                    : 0d;
                builder
                    .Append(SymbolProbabilityLabel(symbol))
                    .Append(' ')
                    .Append(percent.ToString("0.#", CultureInfo.InvariantCulture))
                    .Append('%');
            }

            return builder.ToString();
        }

        private static string SymbolProbabilityLabel(SlotSymbolType symbol) =>
            symbol switch
            {
                SlotSymbolType.Cherry => "체리",
                SlotSymbolType.Lemon => "레몬",
                SlotSymbolType.Clover => "클로버",
                SlotSymbolType.Bell => "종",
                SlotSymbolType.Diamond => "다이아",
                SlotSymbolType.Seven => "7",
                _ => symbol.ToString(),
            };
    }

    public readonly struct RunHUDViewState
    {
        public static readonly RunHUDViewState Empty = new(0, 0, 0, string.Empty);

        public RunHUDViewState(
            int battleIndex,
            int coins,
            int victories,
            string symbolProbabilityText)
        {
            BattleIndex = battleIndex;
            Coins = Math.Max(0, coins);
            Victories = victories;
            SymbolProbabilityText = symbolProbabilityText ?? string.Empty;
        }

        public int BattleIndex { get; }

        public int Coins { get; }

        public int Victories { get; }

        public string SymbolProbabilityText { get; }
    }
}
