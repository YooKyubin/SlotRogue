# 최초 튜토리얼

**Status**: draft  
**Last updated**: 2026-07-06

## Purpose

최초 플레이어에게 본편 전투 루프의 핵심 규칙을 **전투 시작 전 순차 안내**로 설명한다. 전투에 진입하면 스핀을 돌리거나 SWAP을 요구하지 않고, 안내 단계가 한 장씩 표시된다. 플레이어는 튜토리얼 UI의 `다음` 버튼을 눌러 다음 단계로 넘어가며, 마지막 단계까지 본 뒤에는 튜토리얼이 완료되고 일반 런 전투가 시작된다.

안내하는 핵심 규칙:

1. 몬스터를 처치하는 것이 목표다.
2. 매 턴 슬롯을 돌린 결과가 그대로 이번 턴 공격력이 된다.
3. 결과가 마음에 들지 않으면 한 턴에 한 번 SWAP으로 다시 돌릴 수 있다.
4. 그대로 공격하면 별조각을 받고, SWAP을 쓰면 그 턴에는 별조각을 받지 못한다.
5. 모은 별조각으로 상점에서 유물을 구매해 강해진다.

## Decisions

| # | 결정 | 요약 |
|---|------|------|
| T1 | [ADR-0017](../adr/0017-first-run-tutorial-run-game-mode.md) | 최초 튜토리얼은 별도 Scene 복제가 아니라 `RunGame` 튜토리얼 모드로 실행한다. |
| T2 | [ADR-0008](../adr/0008-ui-strict-mvvm-boundary.md) | 튜토리얼 안내 View는 입력을 직접 처리하지 않고, SceneRoot/Flow 신호를 받아 렌더링만 한다. |

## Runtime flow

```text
GameStart / Start Button
├─ TutorialCompleted == false
│  └─ GameFlowSession.StartTutorialRun()
│     └─ RunGame / Battle 진입(HandleBattleEntered)
│        ├─ 실제 전투를 시작하지 않고 순차 안내를 재생
│        ├─ 안내 단계 1장 표시 → 다음 버튼 → 다음 단계 …
│        └─ 마지막 단계에서 다음 버튼을 누르면
│           ├─ FirstRunTutorialState.MarkCompleted()
│           ├─ GameFlowSession.CompleteTutorialAndContinueAsNormalRun()
│           └─ BeginBattle()로 일반 런 전투 시작
└─ TutorialCompleted == true
   └─ GameFlowSession.StartNewRun()
      └─ RunGame / Battle
```

## System boundary

| 영역 | 책임 |
|------|------|
| `FirstRunTutorialState` | `PlayerPrefs` 기반 최초 튜토리얼 완료 플래그 저장/조회 |
| `GameFlowSession` | 현재 런이 튜토리얼인지 보관하고, 순차 안내 종료 시 일반 런으로 전환(`CompleteTutorialAndContinueAsNormalRun`) |
| `RunGameFlowController` | 전투 진입 시 튜토리얼이면 안내 단계를 순서대로 재생하고, 마지막 `다음`에서 완료 처리 후 `BeginBattle` 호출 |
| `RunBattleTutorialSequenceDefinition` | 순서가 있는 튜토리얼 단계(`_steps`)를 asset으로 보관. 각 단계는 강조 타겟, 문구 위치, 문구, 손가락 표시 여부를 가진다. |
| `RunTutorialOverlayView` | 문구 렌더링, 화면 암전/스포트라이트, 손가락 포인터, `다음` 버튼을 표시한다. `다음` 버튼 클릭 시 `onAdvance`를 1회 호출한다. |

## Tutorial content

튜토리얼은 실제 전투를 조립하기 전에, 전투 화면 위 오버레이로 안내 단계를 한 장씩 표시한다. 단계는 `FirstRunBattleTutorialSequence.asset`의 `_steps`에 순서대로 정의하며, 각 단계는 `RunBattleTutorialTargetKey`, `RunTutorialMessagePlacement`, 문구, 손가락 표시 여부를 가진다. 플레이어가 `다음` 버튼을 누를 때마다 다음 단계로 넘어가고, 마지막 단계에서 누르면 완료 플래그를 저장하고 일반 런으로 전환한 뒤 첫 전투를 시작한다.

기존의 시그널 기반(SPIN/SWAP/ATTACK/상점 조작을 요구하는) 스포트라이트 튜토리얼은 이 전투 전 순차 안내로 대체되었다. `BattleTutorialSignal` 배선은 남아 있지만 순차 안내 흐름에서는 사용하지 않는다. 스포트라이트 렌더링은 각 단계의 타겟을 강조하는 용도로만 사용한다.

## Open questions

| ID | 질문 | 비고 |
|----|------|------|
| Q1 | 튜토리얼 다시 보기 진입점 | 로비의 튜토리얼 초기화 버튼이 `FirstRunTutorialState.ResetForDebug()`를 호출해 다음 시작을 튜토리얼로 되돌린다. |
| Q2 | 튜토리얼 전용 몬스터/고정 스핀 | 나레이션 종료 후 곧바로 일반 전투로 진입하므로 튜토리얼 전용 고정 전투 콘텐츠(`TutorialBattleDefinition`·`TutorialEncounterRosterFactory`·`TutorialSlotSpinFactory`)와 `BattleSceneHost._tutorialMonsterDefinition` 필드는 **제거**했다. 튜토리얼 런 플래그(`IsTutorialRun`)는 나레이션 트리거용으로 유지한다. 조작형 튜토리얼을 되살리려면 이 콘텐츠를 다시 도입해야 한다. |
| Q3 | 나레이션 종료 후 첫 전투 난이도 | 튜토리얼 완료 직후의 첫 전투는 일반 런 1번 전투로 진행된다. 최초 플레이어 난이도 조정이 필요한지는 플레이 테스트로 판단한다. |

## Alternatives considered

### 별도 튜토리얼 Scene

`RunGame`의 UI와 전투 조립 참조를 복제해야 하므로 유지보수 비용이 크다. 본편 전투 화면이 바뀔 때 튜토리얼 Scene도 같이 갱신해야 해서 최초 구현에서는 채택하지 않는다.

### 안내 없는 확정 첫 전투

확정 스핀만으로는 플레이어가 몬스터 의도와 턴 전환을 무엇으로 읽어야 하는지 알기 어렵다. 전투 UI 위에 짧은 비차단 문구를 표시한다.
