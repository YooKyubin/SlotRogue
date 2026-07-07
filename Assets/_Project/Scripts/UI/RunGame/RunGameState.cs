namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// RunGameScene 내 가능한 모든 화면 상태입니다.
    /// Navigator는 이 값으로만 화면 전환을 결정합니다.
    ///
    /// 무한 모드 v1 스코프만 정의합니다. 스토리 모드용 상태(Map/Shop/Event/RoundClear/
    /// Victory)는 코드에서 사용처가 없어 제거했습니다. 스토리 모드 재연결 시 다시 추가합니다.
    /// </summary>
    public enum RunGameState
    {
        None = 0,

        // ── 전투 ───────────────────────────────────
        Battle = 20,            // 전투 진행

        // ── 전투 후 ────────────────────────────────
        Reward = 30,            // 전투 후 보상 선택
        Defeat = 40,            // 런 패배 결과
    }
}
