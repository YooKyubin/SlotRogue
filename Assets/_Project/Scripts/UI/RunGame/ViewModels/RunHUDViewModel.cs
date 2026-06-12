using System;
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
}
