using System;
using UnityEngine;

namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// 전투 화면 전체를 감싸는 View입니다.
    /// 실제 전투 시작과 결과 처리는 SceneRoot가 전투 Flow Controller에 연결합니다.
    /// </summary>
    public sealed class BattleView : MonoBehaviour, IRunGameView
    {
        public event Action Entered;

        /// <summary>
        /// 진입 입력을 presenter로 연결한다(ADR-0020). 전투 연출은 명령형 유지(ADR-0019)라
        /// reactive ViewModel 구독은 없다.
        /// </summary>
        public void Bind(RunGameFlowController presenter)
        {
            if (presenter == null)
            {
                return;
            }

            Entered += presenter.HandleBattleEntered;
        }

        public void OnEnter()
        {
            gameObject.SetActive(true);
            Entered?.Invoke();
        }

        public void OnExit()
        {
            gameObject.SetActive(false);
        }
    }
}
