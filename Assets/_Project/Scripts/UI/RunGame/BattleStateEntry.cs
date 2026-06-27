namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// Battle 상태의 진입 핸들러입니다. 화면이 없는 순수 어댑터로, 네비게이터가 Battle 상태로
    /// 들어올 때(또는 다음 웨이브 재진입 시) 전투 시작(HandleBattleEntered)을 호출합니다.
    /// MonoBehaviour가 아니므로 씬 오브젝트·인스펙터 배선이 필요 없습니다(View 아님).
    /// </summary>
    internal sealed class BattleStateEntry : IRunGameView
    {
        private IRunGameFlow _flow;

        public void Bind(IRunGameFlow flow)
        {
            _flow = flow;
        }

        public void OnEnter()
        {
            _flow?.HandleBattleEntered();
        }

        public void OnExit()
        {
        }
    }
}
