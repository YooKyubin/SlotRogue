namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// RunGameScene 내 가능한 모든 화면 상태입니다.
    /// Navigator는 이 값으로만 화면 전환을 결정합니다.
    /// </summary>
    public enum RunGameState
    {
        None = 0,

        // ── 런 진입 ────────────────────────────────
        StartRelicSelect = 1,   // 시작 유물 선택

        // ── 탐색 ───────────────────────────────────
        Map       = 10,         // 맵 노드 선택
        Shop      = 11,         // 상점
        Event     = 12,         // 랜덤 이벤트

        // ── 전투 ───────────────────────────────────
        Battle    = 20,         // 전투 진행

        // ── 전투 후 ────────────────────────────────
        Reward     = 30,        // 보상 선택
        RoundClear = 31,        // 라운드 클리어 연출

        // ── 종료 ───────────────────────────────────
        GameOver  = 40,         // 패배
        Victory   = 41,         // 승리
    }
}
