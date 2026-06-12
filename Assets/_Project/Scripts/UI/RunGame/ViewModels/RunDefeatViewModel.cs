using System;

namespace SlotRogue.UI.RunGame.ViewModels
{
    public sealed class RunDefeatViewModel
    {
        public RunDefeatViewModel()
        {
            State = RunDefeatViewState.Empty;
        }

        public event Action<RunDefeatViewState> Changed;

        public event Action NewRunRequested;

        public RunDefeatViewState State { get; private set; }

        public void Refresh(int battleNumber, int victories, int rewardsClaimed)
        {
            State = new RunDefeatViewState(
                battleNumber,
                victories,
                rewardsClaimed,
                "DEFEAT",
                $"BATTLE {battleNumber}\nVICTORIES {victories}\nREWARDS {rewardsClaimed}",
                "NEW RUN");
            Changed?.Invoke(State);
        }

        public void RequestNewRun()
        {
            NewRunRequested?.Invoke();
        }
    }

    public readonly struct RunDefeatViewState
    {
        public static readonly RunDefeatViewState Empty = new(
            0,
            0,
            0,
            "DEFEAT",
            string.Empty,
            "NEW RUN");

        public RunDefeatViewState(
            int battleNumber,
            int victories,
            int rewardsClaimed,
            string title,
            string summary,
            string newRunLabel)
        {
            BattleNumber = Math.Max(0, battleNumber);
            Victories = Math.Max(0, victories);
            RewardsClaimed = Math.Max(0, rewardsClaimed);
            Title = title ?? string.Empty;
            Summary = summary ?? string.Empty;
            NewRunLabel = newRunLabel ?? string.Empty;
        }

        public int BattleNumber { get; }

        public int Victories { get; }

        public int RewardsClaimed { get; }

        public string Title { get; }

        public string Summary { get; }

        public string NewRunLabel { get; }
    }
}
