using SlotRogue.UI.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// 전투 화면 전체를 감싸는 View입니다.
    /// IRunGameView 계약을 구현하며, 실제 전투 로직은
    /// RunBattleCompositionRoot에 위임합니다.
    /// </summary>
    public sealed class BattleView : MonoBehaviour, IRunGameView
    {
        [SerializeField] private RunBattleCompositionRoot _compositionRoot;

        /// <summary>RunGameCompositionRoot가 결과 콜백을 받기 위해 구독합니다.</summary>
        public event System.Action BattleWon;

        /// <summary>RunGameCompositionRoot가 결과 콜백을 받기 위해 구독합니다.</summary>
        public event System.Action BattleLost;

        private void Awake()
        {
            if (_compositionRoot != null)
            {
                _compositionRoot.BattleVictory += () => BattleWon?.Invoke();
                _compositionRoot.BattleDefeat  += () => BattleLost?.Invoke();
            }
        }

        // ── IRunGameView ─────────────────────────────────────────────────

        public void OnEnter()
        {
            gameObject.SetActive(true);
            _compositionRoot?.BeginBattle();
        }

        public void OnExit()
        {
            gameObject.SetActive(false);
        }
    }
}
