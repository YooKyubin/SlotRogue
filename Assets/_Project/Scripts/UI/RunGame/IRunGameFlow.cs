namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// View가 입력을 전달하는 presenter(흐름 제어자) 추상입니다(ADR-0020).
    /// View는 구체 <see cref="RunGameFlowController"/> 대신 이 인터페이스에 의존하므로,
    /// View 단위 테스트에서 presenter를 대역(fake)으로 바꿔 입력 전달을 검증할 수 있습니다.
    ///
    /// 화면 상태(ViewModel) 갱신을 직접 트리거하는 입력만 포함합니다. 전투 결과·광고 콜백 등
    /// 비-View 입력은 SceneRoot가 구체 컨트롤러로 직접 연결합니다.
    /// </summary>
    public interface IRunGameFlow
    {
        void HandleRewardEntered();

        void HandleRewardSelectionRequested(int optionIndex);

        void HandleRewardRerollRequested();

        void HandleExtraRewardRequested();

        void HandleRewardDoubleRequested();

        void HandleInventoryOpenRequested();

        void HandleInventoryCloseRequested();

        void HandleInventorySymbolTabRequested();

        void HandleInventoryRelicTabRequested();

        void HandleBattleEntered();
    }
}
