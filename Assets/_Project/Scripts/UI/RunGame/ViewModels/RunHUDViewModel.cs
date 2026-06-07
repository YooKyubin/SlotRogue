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
        // ── 상태 ────────────────────────────────────────────────────────

        public int CurrentHp => GameFlowSession.PlayerCurrentHp;
        public int MaxHp => GameFlowSession.PlayerMaxHp;
        public int BattleIndex => GameFlowSession.BattleIndex;
        public int Victories => GameFlowSession.Victories;

        // ── 이벤트 ──────────────────────────────────────────────────────

        /// <summary>HUD 데이터가 변경되어 View를 다시 렌더링해야 할 때 발행합니다.</summary>
        public event Action Changed;

        /// <summary>일시정지 버튼 클릭 시 발행합니다.</summary>
        public event Action PauseRequested;

        // ── 커맨드 ──────────────────────────────────────────────────────

        /// <summary>전투 결과 등 세션 데이터 변경 후 HUD 갱신이 필요할 때 호출합니다.</summary>
        public void Refresh()
        {
            Changed?.Invoke();
        }

        /// <summary>View의 Pause 버튼이 클릭되었을 때 호출합니다.</summary>
        public void RequestPause()
        {
            PauseRequested?.Invoke();
        }
    }
}
