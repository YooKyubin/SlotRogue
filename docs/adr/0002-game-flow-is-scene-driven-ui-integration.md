# 게임 플로우는 씬 기반 UI 연동 계층에서 조립한다

**Status**: proposed  
**Date**: 2026-05-31

## Context

전체 게임 흐름은 `게임 시작 → 시작 유물 선택 → 맵 → 전투 → 보상 → 맵` 순환으로 이어져야 한다. 현재 슬롯은 `SlotCombatRequest`를 만들고, 전투는 `CombatEffect[]`를 받아 `BattleSystem`에서 턴을 처리한다. 전투 담당 코드와 슬롯 담당 코드는 asmdef로 분리되어 있으며, 전투 코드는 수정하지 않는 조건이 있다.

따라서 새 플로우는 기존 전투 코어를 변경하지 않고, 슬롯 결과를 전투 입력으로 변환하는 연동 계층에서 조립해야 한다.

## Decision

게임 플로우 MVP는 씬을 분리하고, `SlotRogue.UI`의 `GameFlow` 계층이 씬 전환과 런 상태를 조율한다.

- 씬은 `GameStart`, `StartArtifactSelection`, `RunMap`, `RunBattle`, `RunReward`로 나눈다.
- 각 씬의 UI는 View 프리팹으로 배치하고, Controller는 배치된 UI 참조를 갱신한다.
- `SlotRogue.UI.GameFlow`는 슬롯 `SlotCombatRequest`와 전투 `BattleSystem`을 연결한다.
- 전투 코어와 전투 Dev 하네스는 수정하지 않는다.
- `RunBattle`은 `SlotMachineViewModel`로 스핀 결과를 만들고, 기존 `SlotCombatRequestToCombatEffectsConverter`를 통해 `BattleSystem.ApplyPlayerTurn(...)`에 넘긴다.
- 시작 유물과 보상 효과는 전투 코어가 아니라 플로우 연동 계층에서 `SlotCombatRequest` 후처리로 적용한다.
- 맵 MVP는 실제 그래프 생성 전 단계로, 현재 진행 층을 보여준 뒤 다음 전투로 진입한다.

## Alternatives considered

### 단일 씬 상태 머신

한 씬 안에서 패널만 교체하면 씬 로드 비용과 상태 전달 문제는 줄어든다. 하지만 이번 목표는 실제 게임 화면 흐름을 씬 단위로 검증하는 것이므로, 시작/맵/전투/보상 경계가 흐려진다.

### 전투 씬 안에서 슬롯과 보상까지 모두 처리

빠르게 붙일 수 있지만 전투 씬이 전체 런 흐름을 알게 된다. 전투 담당 코드와 화면 책임이 섞일 가능성이 커서 제외한다.

### 슬롯에서 전투를 직접 호출

슬롯 asmdef가 전투 코어를 참조하게 되어 기존 `slot-core.md`의 S4 결정을 깨뜨린다. 슬롯은 요청 DTO까지만 만들고, 연결은 UI/GameFlow 계층이 담당한다.

## Consequences

- 전투 코드를 수정하지 않고도 playable loop를 확인할 수 있다.
- MVP 맵은 고정 로그라이트 그래프를 사용한다. 추후 `roguelike-meta.md` 또는 별도 맵 plan에서 seed 기반 그래프 생성으로 교체할 수 있다.
- 시작 유물/보상은 현재 `SlotCombatRequest` 기반 후처리라, 더 복잡한 효과는 나중에 ScriptableObject 데이터로 승격할 수 있다.
