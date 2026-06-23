# UI는 strict MVVM 경계를 따른다

**Status**: accepted  
**Date**: 2026-06-11  
**Supersedes**: none  
**Superseded by**: 0020 (View가 ViewModel을 모른다는 경계 부분 — MVP + Reactive ViewModel로 전환)  
**Related design-docs**: [`../design-docs/game-flow.md`](../design-docs/game-flow.md)

---

## Context

현재 게임 UI는 `BootScene`, `GameStart`, `RunGame` 세 씬으로 구성된다. 전투는 별도 `RunBattle` 씬이 아니라 `RunGame` 안의 `BattleView` 상태다. 과거 씬 구조에서 유래한 `RunBattle*` 이름과 View가 ViewModel 또는 Flow Controller를 직접 호출하는 코드가 남아 있어 화면 경계와 생명주기 책임이 불명확하다.

Unity UI 객체와 게임 상태 변경을 분리하지 않으면 View 테스트가 어려워지고, 화면 재진입 시 RNG 재실행이나 이벤트 중복 구독처럼 표시 계층이 게임 동작을 바꾸는 문제가 생긴다.

## Decision

모든 production UI 화면은 strict MVVM 경계를 따른다.

- **View**는 Unity UI 참조, `Render(state)`, 사용자 입력 event, 활성화/비활성화만 담당한다.
- View는 ViewModel, Flow Controller, `GameFlowSession`, 전투·슬롯·유물 도메인 객체를 직접 참조하거나 호출하지 않는다.
- **ViewModel**은 `UnityEngine`을 참조하지 않는 순수 C# 타입이다. 화면 상태 DTO와 command를 제공하며 도메인 상태를 화면 상태로 변환한다.
- **SceneRoot**가 View 입력 event를 ViewModel command와 Flow Controller에 연결하고, ViewModel 상태를 View에 렌더링하며 화면 전환을 조율한다.
- 일회성 전투·슬롯 연출은 immutable presentation DTO를 받는 View와 순수 Presenter/Controller로 구성한다. 연출 View는 게임 상태를 직접 변경하지 않는다.
- `RunGame` 내부 전투 화면을 별도 `RunBattle` 씬으로 표현하지 않는다. 코드와 문서에서는 `RunGame Battle 화면` 또는 `Battle*` 역할명을 사용한다.

## Alternatives considered

- **View가 ViewModel을 직접 보유하고 command 호출** — 일반적인 Unity MVVM 구현이지만 View 생명주기가 RNG·세션 변경을 유발할 수 있고 화면 테스트가 ViewModel 구현에 결합되어 거절한다.
- **Controller 중심 MVP 구조 유지** — 구현량은 적지만 화면마다 경계가 달라지고 이미 보상 중복 추첨 문제가 발생해 거절한다.

## Consequences

- 화면 입력과 렌더링을 독립적으로 테스트할 수 있다.
- ViewModel 상태 DTO와 SceneRoot wiring 코드가 추가된다.
- 기존 prefab/scene의 MonoBehaviour GUID와 직렬화 참조를 보존하며 역할명을 단계적으로 정리해야 한다.
- 개발 전용 하네스는 production 화면과 분리해 유지할 수 있지만 production View를 직접 제어하는 경로는 허용하지 않는다.
