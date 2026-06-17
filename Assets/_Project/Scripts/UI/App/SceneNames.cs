namespace SlotRogue.UI.App
{
    /// <summary>
    /// 빌드에 포함되는 씬 이름 상수 모음입니다.
    /// SceneManager 호출 시 문자열 리터럴 산재를 막기 위해 이 곳에서만 관리합니다.
    ///
    /// 주의: 실제 게임 씬 이름은 "RunGame"입니다. "RunGameScene"이 아닙니다.
    /// 값과 Build Settings에 등록한 씬 파일 이름이 정확히 일치해야 합니다.
    /// </summary>
    public static class SceneNames
    {
        /// <summary>앱 초기화 전용 씬. 진입점.</summary>
        public const string Title = "00_TitleScene";

        /// <summary>타이틀 / 시작 화면 씬.</summary>
        public const string Lobby = "10_LobbyScene";

        /// <summary>무한 모드 게임 전체를 담는 씬. 단독 Play도 지원합니다.</summary>
        public const string RunGame = "20_RunGameScene";
    }
}
