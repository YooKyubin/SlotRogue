# ADR-0019: 메뉴/상태표시 UI는 R3 ReactiveProperty로 바인딩한다 (전투 연출은 명령형 유지)

**Status**: accepted
**Date**: 2026-06-23
**Supersedes**: none
**Superseded by**: 0020 (바인딩 소유권 부분 — View가 자기 ViewModel을 Bind)
**Related design-docs**: none

---

## Context

UI는 MVVM 구조(View / ViewModel / Model)로 작성돼 있다. 그러나 Unity에는 데이터 바인딩이 없어, ViewModel→View 동기화를 손으로 만든 옵저버로 흉내 내 왔다:

- 각 ViewModel이 `event Action<TViewState> Changed`를 발사하고, `State` 스냅샷 프로퍼티를 노출한다.
- `RunGameSceneRoot`가 `+=`로 구독해 `Handle*StateChanged` 핸들러에서 `View.Render(state)`를 호출하고, `OnDestroy`의 `UnsubscribeEvents`에서 `-=`로 일일이 해제한다.

이 "바인딩 없는 MVVM"은 ViewModel·ViewState 의식은 다 치르면서 정작 자동 동기화는 못 받아, 무게가 `RunGameSceneRoot`의 약 185줄 구독/해제 거울쌍 한 곳에 쏠린다. `-=`를 하나만 빠뜨려도 누수·중복 구독이 발생한다.

한편 **전투 연출은 이미 MVVM이 아니다.** `BattleFlowController`/`SlotTurnController`/`BattlePresentationController`(+ 이벤트별 Presenter 패밀리)는 `await`로 순서를 지휘하는 명령형(director)이다. 턴제 전투는 "모델이 바뀐 즉시"가 아니라 "애니메이션이 착지한 순간"에 표시해야 하므로, 자동 동기화(reactive)는 오히려 안무를 깬다.

R3(Cysharp, 이미 쓰는 UniTask와 동일 제작자)을 NuGetForUnity로 설치했다(`R3` 1.3.1 + `System.Threading.Channels` + UPM `R3.Unity`).

## Decision

**정적 메뉴/상태표시 ViewModel은 R3 `ReactiveProperty`로 바인딩하고, 전투 연출·슬롯 파이프라인은 명령형 `await`를 유지한다.**

- 대상 ViewModel은 `event Changed` + `State { get; private set; }`를 `ReactiveProperty<TViewState>`(쓰기) + `ReadOnlyReactiveProperty<TViewState>`(노출) 하나로 통합한다. `Publish`는 `_state.Value = new TViewState(...)`로 갱신한다.
- 소유자(예: `RunGameSceneRoot`)는 `vm.State.Subscribe(view.Render).AddTo(this)` 한 줄로 바인딩한다. 구독 즉시 현재 값이 1회 흐르므로 초기 `Render` 호출이 불필요하고, `AddTo(this)`가 컴포넌트 파괴 시 구독을 자동 해제해 수동 `-=` 거울쌍과 `Handle*StateChanged` 핸들러가 사라진다.
- 버튼/입력 이벤트는 점진적으로 `OnClickAsObservable()`로 옮길 수 있으나 이번 결정의 필수 범위는 아니다(상태 출력 채널 우선).

**적용 범위(메뉴/상태표시):** `RunDefeatViewModel`, `RunHUDViewModel`, `RunInventoryViewModel`, `RunRewardViewModel`, `StartRelicSelectViewModel`, `LeaderboardViewModel`.

**제외(명령형 유지):**
- 전투 연출 파이프라인(`BattleFlowController`, `SlotTurnController`, `BattlePresentationController`와 Presenter 패밀리, `SlotMachineSpinPresenter`) — 시퀀스 안무는 `await` 명령형이 정답이며 reactive 자동 동기화는 타이밍을 깬다.
- `CombatViewModel`(전투 중 라이브 수치) — 상태표시지만 "애니메이션 비트 시점"에만 갱신돼야 한다. 추후 "비트 시점에만 `Value`를 세팅"하는 규율이 정립되면 선택적으로 도입한다.
- `SlotMachineModel`(구 `SlotMachineViewModel`) — 슬롯 서비스/리졸버/계산기를 모아 `Spin()` 파이프라인을 돌리는 **Model**이다. 명령형으로 질의(`Spin()` 후 `Current*` pull)되므로 reactive 대상이 아니다. 역할에 맞게 이름을 `SlotMachineModel`로 정정한다.

## Alternatives considered

- **손배선 옵저버 유지(현행)** — 동작은 하나 `RunGameSceneRoot` 거울쌍 보일러플레이트와 `-=` 누락 위험이 그대로다. 제외.
- **MVP(Passive View)로 전환** — 라이브러리 없이 Presenter가 `IView`를 직접 호출. 정직하지만 전 화면의 View 경계를 다시 긋는 비용이 크고, 이미 보유한 "순수 C# 테스트 가능 로직" 이득은 추가되지 않는다. 전투는 이미 사실상 이 모델이므로 메뉴까지 굳이 통일할 실익이 작다. 제외.
- **UniRx 도입** — R3의 구세대. 유지보수 정체·GC 이슈가 있어 신규 채택 이유가 없다. 제외.

## Consequences

- `RunGameSceneRoot`의 구독/해제 거울쌍과 `Handle*StateChanged` 핸들러, `BindViews`의 초기 `Render` 호출이 대상 화면만큼 줄어든다. `AddTo(this)`로 구독 누수 위험이 제거된다.
- 의존성에 R3(`com.cysharp.r3`)와 NuGetForUnity가 추가된다. R3는 UniTask와 동일 Cysharp 생태계라 통합 충돌 위험이 낮다. `SlotRogue.UI.asmdef`에 `R3.Unity`/`R3.Unity.TextMeshPro` 참조를 추가한다.
- R3 코어는 순수 .NET이라 ViewModel EditMode 테스트는 그대로 유지되며, "값 세팅 시 1회 emit"까지 검증할 수 있다.
- 전투 연출과 메뉴가 의도적으로 다른 패턴(명령형 vs reactive)을 쓴다. 이 경계는 "시퀀스 지휘 = 명령형, 상태 표시 = reactive"로 명문화한다.
- `SlotMachineViewModel` → `SlotMachineModel` 이름 정정(파일·테스트 포함). 네임스페이스/폴더(`Slot/ViewModels`) 이동은 후속 정리 대상으로 남긴다.
