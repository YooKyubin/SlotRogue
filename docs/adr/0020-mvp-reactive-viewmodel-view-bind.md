# ADR-0020: UI는 MVP + Reactive ViewModel로 정리하고 View가 자기 ViewModel을 Bind한다

**Status**: accepted
**Date**: 2026-06-23
**Supersedes**: 0008 (binding/경계 부분), 0019 (바인딩 소유권 부분)
**Superseded by**: none
**Related design-docs**: none

---

## Context

ADR-0008은 strict MVVM 경계를 정하고 "View가 ViewModel을 직접 보유"하는 구성을 명시적으로 거절했다. ADR-0019는 메뉴/상태표시 ViewModel을 R3 `ReactiveProperty`로 바꾸되, **바인딩 소유권을 소유자(`RunGameSceneRoot`)에 두어** `vm.State.Subscribe(view.Render).AddTo(this)`로 연결하도록 결정했고 MVP(Passive View) 전환은 거절했다.

그 결과 `RunGameSceneRoot`는 (1) 객체 생성, (2) State→View 바인딩, (3) 입력 event 배선, (4) 전투/보상/패배/부활/튜토리얼/네비게이션 **흐름 제어**까지 한 클래스(약 1000줄)에 모여 비대해졌다. 흐름 제어와 조립이 한 곳에 섞여 읽기·테스트가 어렵다.

## Decision

UI는 **순수 MVVM이 아니라 MVP 기반 + Reactive ViewModel**로 정리한다.

- **흐름 제어는 Presenter/FlowController가 담당한다.** 전투 진입/결과, 보상 선택·광고 리롤, 패배/부활, 홈/랭킹 이동, 튜토리얼 진행 같은 "언제·어디로 전환하는가"는 FlowController가 결정하고 ViewModel/Session/System을 호출한다. `RunGameSceneRoot`는 생성·연결·초기 진입 호출만 담당한다.
- **ViewModel은 화면 상태만 가진다.** R3 `ReactiveProperty<T>`를 내부 보유하고 외부에는 `ReadOnlyReactiveProperty<T>`로 노출한다. 게임 진행 로직을 넣지 않는다. (보상 추첨/적용은 중복 추첨 방지를 위해 `RunRewardViewModel`에 의도적으로 유지한다 — ADR-0019 범위 유지.)
- **View가 자기 ViewModel을 Bind한다.** `Bind(viewModel, presenter)`에서 View가 `vm.State.Subscribe(Render).AddTo(this)`로 직접 구독하고, 버튼 입력을 presenter로 전달한다. 이 부분이 ADR-0019의 "소유자가 구독" 결정을 대체한다. `SceneRoot`는 `view.Bind(vm, presenter)`만 호출한다.
- **전투 연출·슬롯 파이프라인은 명령형 `await`를 유지한다** (ADR-0019 그대로).

### 단계적 적용 (compile-first, 동작 보존)

1. **(완료) FlowController 추출** — `RunGameSceneRoot`의 흐름 제어를 순수 C# `RunGameFlowController`로 분리. MonoBehaviour가 아니므로 씬/프리팹/GUID 변경 없음. SceneRoot는 생성·바인딩·event 배선·초기 GoTo만 유지.
2. **(완료) View.Bind 전환** — State→View 구독과 View 입력 event 연결을 SceneRoot에서 각 View의 `Bind(vm, presenter)`로 이동. View가 `vm.State.Subscribe(Render).AddTo(this)`로 자기 ViewModel을 직접 구독한다. (BattleView·LeaderboardView는 reactive ViewModel 미보유 — 3단계 대상.)
3. **(완료) 잔여 event→R3 통일** — `LeaderboardViewModel`을 `event Changed`+`State` 스냅샷에서 `ReactiveProperty`/`ReadOnlyReactiveProperty`로 전환. `LeaderboardView.Bind(vm)`가 상태 구독·close/refresh 입력을 소유하고, `RunGameSceneRoot`/`GameStartSceneRoot`의 leaderboard 거울쌍을 제거했다. (`GameStart`의 로그인 View는 같은 VM 상태를 렌더하므로 SceneRoot가 `State.Subscribe(...).AddTo(this)`로 구독을 소유한다. `CombatViewModel`은 ADR-0019대로 명령형 유지.)

## Alternatives considered

- **ADR-0019 그대로(소유자 구독) 유지** — 동작하지만 `RunGameSceneRoot` 비대 문제와 흐름/조립 혼재가 남는다. 거절.
- **순수 MVVM(흐름까지 Subscribe 체인)** — 턴제 전투 흐름을 reactive 체인으로 만들면 안무 타이밍이 깨진다. 거절.

## Consequences

- 흐름 제어가 `RunGameFlowController`로 분리되어 SceneRoot는 조립자로 축소된다. FlowController는 순수 C#이라 흐름 단위 테스트가 가능해진다.
- View가 ViewModel/Presenter 참조를 받으므로 ADR-0008의 "View는 ViewModel을 모른다" 경계는 폐기된다. 대신 "View는 도메인/Session을 모른다"는 유지한다.
- 비동기 수명: FlowController는 OnDestroy가 없으므로 SceneRoot의 destroy 토큰을 주입받아 부활 카운트다운/로비 복귀 작업을 자동 취소한다.
- 1·2·3단계는 별도 변경으로 나누어 컴파일 가능성을 우선한다.
