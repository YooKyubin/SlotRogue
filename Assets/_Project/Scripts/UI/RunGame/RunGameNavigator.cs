using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// RunGameScene 내 View 전환만 담당합니다.
    /// 게임 규칙·보상 처리·전투 계산은 이 클래스에 넣지 않습니다.
    /// </summary>
    public sealed class RunGameNavigator : MonoBehaviour, IRunGameNavigator
    {
        public static RunGameNavigator Instance { get; private set; }

        /// <summary>현재 활성 상태</summary>
        public RunGameState CurrentState { get; private set; } = RunGameState.None;

        /// <summary>상태 전환 시 발행 (이전 상태, 새 상태)</summary>
        public event Action<RunGameState, RunGameState> StateChanged;

        private readonly Dictionary<RunGameState, IRunGameView> _views = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        // ── 등록 ────────────────────────────────────────────────────────

        /// <summary>
        /// CompositionRoot가 각 View를 상태 키로 등록합니다.
        /// Awake 이후, GoTo 이전에 호출해야 합니다.
        /// </summary>
        public void Register(RunGameState state, IRunGameView view)
        {
            if (view == null)
            {
                Debug.LogWarning($"[RunGameNavigator] null view 등록 시도: {state}");
                return;
            }

            _views[state] = view;
        }

        // ── 전환 ────────────────────────────────────────────────────────

        /// <summary>
        /// 지정한 상태의 View로 전환합니다.
        /// 현재 View의 OnExit → 새 View의 OnEnter 순서로 호출됩니다.
        /// </summary>
        public void GoTo(RunGameState nextState)
        {
            if (nextState == CurrentState) return;

            RunGameState prevState = CurrentState;

            // 현재 View 종료
            if (CurrentState != RunGameState.None &&
                _views.TryGetValue(CurrentState, out IRunGameView current))
            {
                current.OnExit();
            }

            CurrentState = nextState;

            // 새 View 진입
            if (_views.TryGetValue(nextState, out IRunGameView next))
            {
                next.OnEnter();
            }
            else
            {
                Debug.LogWarning($"[RunGameNavigator] 등록된 View 없음: {nextState}");
            }

            StateChanged?.Invoke(prevState, nextState);
        }
    }
}
