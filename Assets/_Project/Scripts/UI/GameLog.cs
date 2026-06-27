using System.Diagnostics;

namespace SlotRogue.UI
{
    /// <summary>
    /// 정보성 로그 래퍼. [Conditional] 덕분에 릴리스 빌드(에디터/개발 빌드가 아닌 경우)에서는
    /// 호출 자체가 컴파일 단계에서 제거되어, 로그 문자열 생성·전송 비용이 남지 않습니다.
    /// 경고/오류는 출시 후 진단에 필요하므로 Debug.LogWarning/LogError를 직접 사용합니다.
    /// </summary>
    public static class GameLog
    {
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string message)
        {
            UnityEngine.Debug.Log(message);
        }
    }
}
