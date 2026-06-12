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
