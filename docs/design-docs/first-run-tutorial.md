# 최초 튜토리얼

**Status**: draft  
**Last updated**: 2026-06-23

## Purpose

최초 플레이어가 본편 전투 루프를 실제 조작으로 한 번 경험하게 한다. 시작 버튼을 누른 뒤 시작 유물 없이 전투에 들어가고, 첫 슬롯 스핀에서 확정 족보를 보여주며, 족보 결과가 전투 피해로 이어지고 몬스터 턴에는 의도와 피해를 확인해야 한다는 점을 안내한다.

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
│     └─ RunGame / Battle
│        ├─ 안내: 몬스터 의도 아이콘을 먼저 확인하고 SPIN 버튼 누르기
│        ├─ 첫 Spin: 고정 세로 3칸 족보
│        ├─ 안내: 족보 결과가 피해로 적용됨
│        ├─ 몬스터 공격 피격 후 안내: 플레이어 HP 감소 확인
│        ├─ 몬스터 턴 후 안내: 다음 의도 갱신과 다음 행동 판단
│        └─ 전투 종료 시 TutorialCompleted 저장 후 StartRelicSelect로 전환
└─ TutorialCompleted == true
   └─ GameFlowSession.StartNewRun()
      └─ RunGame / StartRelicSelect
```

## System boundary

| 영역 | 책임 |
|------|------|
| `FirstRunTutorialState` | `PlayerPrefs` 기반 최초 튜토리얼 완료 플래그 저장/조회 |
| `GameFlowSession` | 현재 런이 튜토리얼인지 보관하고 시작 유물 스킵 여부 제공 |
| `BattleSceneCompositionRoot` | 튜토리얼 전용 몬스터 정의 참조, 튜토리얼 HP/action plan 주입, 전투 신호 전달 |
| `BattleFlowController` | 슬롯 연출 완료, 몬스터 턴 종료 같은 전투 단계 신호 발생 |
| `RunTutorialOverlayView` | 비차단 안내 메시지 렌더링 |

## Tutorial content

첫 스핀은 5 x 3 보드의 첫 번째 세로줄을 같은 심볼로 고정한다. 이 결과는 기존 `SlotPatternResolver`와 `SlotResultCalculator`를 그대로 통과하므로 튜토리얼만 다른 전투 계산식을 갖지 않는다. 튜토리얼 몬스터 HP는 첫 확정 족보 피해보다 높게 유지해, 플레이어가 첫 턴에 몬스터 공격을 실제로 맞고 HP 변화를 확인하게 한다.

안내 문구는 다음 순서를 따른다.

1. 전투 시작: 몬스터 의도 아이콘이 다음 행동이라는 점과, 확인 후 `SPIN` 버튼을 누르라는 안내.
2. 슬롯 결과 연출 완료: 같은 심볼이 족보를 만들고, 족보 점수가 피해로 전환된다는 안내.
3. 몬스터 공격 피격: 플레이어 HP가 줄어드는 것을 확인하고, 방어/회복 선택의 의미를 안내.
4. 몬스터 턴 종료: 다음 의도 갱신과 다음 플레이어 턴 판단을 안내.
5. 전투 종료: 다음 실행부터 일반 런이 시작된다는 안내와 완료 플래그 저장.

## Open questions

| ID | 질문 | 비고 |
|----|------|------|
| Q1 | 튜토리얼 다시 보기 진입점 | 설정/옵션 화면이 확정되면 `FirstRunTutorialState.ResetForDebug()`와 별도 버튼 연결 후보. |
| Q2 | 튜토리얼 전용 몬스터 asset | 현재는 `BattleSceneCompositionRoot._tutorialMonsterDefinition`에 연결된 `MonsterDefinition`을 튜토리얼 적 2마리의 visual/data source로 사용하고, HP와 action plan은 튜토리얼 코드에서 고정 주입한다. 콘텐츠 확정 후 별도 튜토리얼 전용 asset 제작 여부를 결정한다. |
| Q3 | 보상 선택까지 튜토리얼에 포함할지 | 첫 범위는 전투 1회로 제한한다. 보상 UI 학습은 후속 온보딩 후보. |

## Alternatives considered

### 별도 튜토리얼 Scene

`RunGame`의 UI와 전투 조립 참조를 복제해야 하므로 유지보수 비용이 크다. 본편 전투 화면이 바뀔 때 튜토리얼 Scene도 같이 갱신해야 해서 최초 구현에서는 채택하지 않는다.

### 안내 없는 확정 첫 전투

확정 스핀만으로는 플레이어가 몬스터 의도와 턴 전환을 무엇으로 읽어야 하는지 알기 어렵다. 전투 UI 위에 짧은 비차단 문구를 표시한다.
