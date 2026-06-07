namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// RunGameScene 내 모든 View가 구현해야 하는 인터페이스.
    /// Navigator가 이 인터페이스만 알고 있으며, View 구현 세부사항에 의존하지 않습니다.
    /// </summary>
    public interface IRunGameView
    {
        /// <summary>이 View가 활성화될 때 Navigator가 호출합니다.</summary>
        void OnEnter();

        /// <summary>이 View가 비활성화될 때 Navigator가 호출합니다.</summary>
        void OnExit();
    }
}
