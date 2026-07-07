# 전체 UI strict MVVM

**Status**: active  
**Started**: 2026-06-11  
**Owner**: _(슬롯 담당)_  
**Related design-docs**: [`../../design-docs/game-flow.md`](../../design-docs/game-flow.md), [`../../adr/0008-ui-strict-mvvm-boundary.md`](../../adr/0008-ui-strict-mvvm-boundary.md)

## Goal

`GameStart`와 `RunGame`의 모든 production 화면을 View 입력 event + 순수 ViewModel 화면 상태 + SceneRoot wiring 구조로 통일한다. 별도 `RunBattle` 씬이 존재하지 않는 현재 구조를 코드와 문서에 반영하고, 과거 명칭과 직접 의존을 제거한다.

## Checklist

- [x] 현재 씬이 `BootScene`, `GameStart`, `RunGame` 세 개뿐인지 확인
- [x] strict MVVM 경계를 ADR-0008로 확정
- [x] `GameStart`를 View + ViewModel + SceneRoot로 분리
- [x] 시작 유물 View의 ViewModel·도메인 직접 참조 제거
- [x] 보상 View의 ViewModel·도메인 직접 참조와 중복 추첨 제거
- [x] 공통 HUD View의 ViewModel 직접 참조 제거
- [x] BattleView의 Flow Controller 직접 참조 제거
- [x] production View에 남은 ViewModel·Controller·게임 상태 직접 의존 전수 검사
- [ ] 과거 `RunBattle` 씬 명칭을 현재 `RunGame Battle 화면` 역할명으로 정리
- [x] ViewModel 및 wiring 테스트 추가
- [x] `dotnet build SlotRogue.slnx --no-restore` 검증
- [x] 승리 시 종료 버튼 없이 Reward View 자동 전환
- [x] 패배 시 Defeat View 자동 전환 및 새 런 command 연결
- [x] Battle View의 legacy 보상/재시작 버튼 비활성화
- [x] `GameFlowOptionView` 카드 제목·설명을 `TMP_Text`로 전환
- [x] `RunRewardDefinition`을 보상 종류별 값 동등성으로 비교해 추가 보상 중복 방지
- [x] 유물 아이콘 Addressables 로드·취소·render version 수명을 `RelicIconRenderer`로 분리
- [x] `RunDefeatView`가 직렬화/기존 자식 UI를 우선 사용하고 runtime layout은 빈 화면 fallback으로 제한
- [x] 주요 RunGame/Battle 씬 참조에 이름이 확정된 `AutoWire`만 적용
- [x] AutoWire가 TMP 내부 linked text 필드를 자기 참조시키는 문제를 차단해 Editor StackOverflow 수정
- [x] 전투 런 인벤토리 팝업을 View event + 순수 ViewModel + SceneRoot wiring으로 연결
- [x] 로비 우주 배경에 하이어라키 배치 행성 Sprite 부유 애니메이션 연결 — Codex
- [x] 로비/결과 랭킹 UI의 런타임 생성 fallback 제거 — Codex
- [x] 하이어라키 배치형 주간 랭킹 패널에 profile/message/wave/detail 바인딩 적용 — Codex
- [x] 방향 전환: strict MVVM → MVP + Reactive ViewModel (ADR-0020)
- [x] (1단계) `RunGameSceneRoot` 흐름 제어를 `RunGameFlowController`로 추출 (동작 보존, compile PASS)
- [x] (2단계) State→View 구독과 View 입력 event 연결을 각 View `Bind(vm, presenter)`로 이동 (compile PASS)
- [x] (3단계) `LeaderboardViewModel` event→R3 통일 + `LeaderboardView.Bind` (compile PASS)
- [x] (후속) `BattleView.Bind(presenter)`로 진입 입력 정리 (`CombatViewModel`/전투 연출은 명령형 유지)
- [x] View↔presenter 결합을 `IRunGameFlow` 인터페이스로 분리 (View가 추상에 의존, compile PASS)
- [x] FlowController의 MonoBehaviour 의존을 인터페이스(`IRunGameNavigator`/`IBattleSceneController`/`ITutorialOverlay`/`IDefeatPortraitView`, battleView는 `IRunGameView`)로 추상화
- [x] `RunGameFlowController` 흐름 단위 EditMode 테스트 7종 추가 (compile PASS — Unity Test Runner 실행 대기)
- [x] `GameStartSceneRoot`(728줄→~330줄) 분리: 로비 배경→`LobbySpaceBackground`, 디버그 버튼→`LobbyDebugButtons` (compile PASS)
- [ ] (보류) `GameStartViewModel` R3화 — 화면 상태 없이 Start/Quit intent event만 가져 reactive 대상 아님(RunDefeatVM 요청 event와 동일 취급)
- [x] ViewModel 단위 테스트: `RunDefeatViewModelTests`(7) + `RunRewardViewModelTests`(6) (compile PASS)
- [x] View 공용 헬퍼 `ViewComponentBase`로 중복 제거 (Reward/Defeat/Inventory/TutorialOverlay)
- [x] 보상 추첨/적용 도메인 로직을 `RunRewardViewModel`→`IRunRewardService`/`RunRewardService`로 분리 (VM은 화면 상태만)
- [x] AutoWire 휴리스틱 자동 배선 도구 제거 (`SceneAutoWireTool`/`AutoWireAttribute`), `[AutoWire]`→`[SerializeField]` 치환
- [ ] Unity EditMode 테스트 및 RunGame 수동 플레이테스트 (+ AutoWire 제거 후 씬 Inspector 참조 채워졌는지 확인)

## Notes

- 전투 화면 자산은 `Assets/_Project/Prefabs/UI/GameFlow/RunGame/10_BattleView.prefab`이며 별도 `RunBattle.unity`는 없다.
- 사용자 작업 중인 prefab/scene 배치를 유지하고 MonoBehaviour GUID를 보존한다.
- 전투 씬 조립은 `BattleSceneCompositionRoot`, 턴 순서는 순수 C# `BattleFlowController`, RunGame 화면 전환은 `RunGameSceneRoot`가 담당한다. 씬 저장 후 참조가 끊긴 기존 `RunBattleCompositionRoot`와 `RunGameCompositionRoot` 호환 컴포넌트는 삭제했다.
- production `*View.cs` 정적 검사에서 ViewModel 보관, Flow Controller 직접 호출, `GameFlowSession`/전투 도메인 직접 참조가 남지 않았음을 확인했다.
- `dotnet build SlotRogue.slnx --no-restore`는 경고 0개, 오류 0개로 통과했다. Unity Editor instance가 없어 EditMode 테스트와 수동 플레이테스트는 대기 중이다.
- 전투 종료 시 `BattleFlowController`가 연출 완료 직후 결과 event를 발행한다. 승리는 `Reward`, 패배는 `Defeat` 상태로 자동 전환하며 전투 화면의 `Claim Reward`/`Return To Start` 버튼은 항상 숨긴다.
- 패배 화면은 기존 빈 `DefeatView` 자리를 `RunDefeatView`가 사용하며, 화면 상태와 새 런 command는 순수 `RunDefeatViewModel`을 통해 연결한다.
- 시작 유물/보상 카드 prefab은 기존 `TextMeshProUGUI`와 Galmuri11 SDF 자산을 유지한다. `GameFlowOptionView`는 비어 있는 직렬화 필드를 카드 자식 이름으로 자동 복구한다.
- 2026-06-12: 보상 정의는 `Relic.Id`, `Symbol+Amount`, `RunRewardType` 기준 값 동등성을 사용한다. `RunRewardCatalog.ForTier()`가 새 wrapper를 만들어도 추가 보상 제외 목록이 같은 보상을 인식하며 관련 EditMode 테스트를 `RelicCatalogTests`에 추가했다.
- 2026-06-12: `RunGameSceneRoot`의 유물 아이콘 로드 책임을 `RelicIconRenderer`로 이동했다. `AddressableSpriteProvider`, 취소 토큰, 시작/보상 render version의 생성·해제 수명을 렌더러가 소유한다.
- 2026-06-12: 패배 화면은 Inspector 참조와 `Defeat Title`/`Defeat Summary`/`New Run Button` 자식을 먼저 복구한다. 현재 씬의 빈 `DefeatView` placeholder에서는 기존 runtime layout fallback을 유지하며, 일부 참조만 연결된 경우 중복 UI를 만들지 않고 설정 오류를 노출한다.
- 2026-06-12: `RunGameSceneRoot`, `BattleSceneCompositionRoot`, `RunBattleScreenView`의 실제 하이어라키 이름이 확인된 필드에만 `AutoWire`를 적용했다. 기존 Inspector 값은 `AllowOverwrite=false` 기본값으로 보존된다.
- 2026-06-13: Hierarchy 변경 시 휴리스틱 AutoWire가 새 TMP까지 스캔해 패키지 내부 `m_linkedTextComponent`와 `parentLinkedComponent`에 자기 자신을 배선하면서 `ReleaseLinkedTextComponent()`가 무한 재귀했다. AutoWire 대상을 `SlotRogue.*` 타입으로 제한하고, 자동 실행은 `[AutoWire]` 필드만 처리하며 휴리스틱 자기 참조 후보를 제외했다. 새 TMP 생성 검증은 PASS했고 전체 scene/prefab의 linked text 순환 참조가 없음을 확인했다.
- 2026-06-18: 전투 화면의 `Relic Inventory Origin` 입력은 `RunInventoryView` event로만 노출하고, 심볼 확률/보유 유물 스냅샷과 탭 상태는 `RunInventoryViewModel`이 만든다. `RunGameSceneRoot`가 열기/닫기/탭 전환을 wiring하며 View는 `GameFlowSession`을 직접 읽지 않는다.
- 2026-06-19: `10_LobbyScene`의 `00_MachineArea` 아래에 `Animated Planet Layer`와 `Floating Planet 01~04` Image를 미리 배치했다. `GameStartSceneRoot`는 이 오브젝트들을 생성하지 않고 바인딩만 하며, 각 행성은 `Resources/Textures/UI/Lobby/back_astronaut-Sheet` sliced Sprite를 `localScale=4`로 표시한다. 레이어는 `StageImage`보다 앞 sibling이라 스테이지 창에 가려지고, 넓은 배치에서 서로 다른 속도·위상으로 drift/bob/rotation을 적용한다.
- 2026-06-19: `GameStartSceneRoot`와 `RunGameSceneRoot`에서 랭킹 UI를 새로 생성하던 fallback을 제거했다. `LeaderboardView`와 `RunDefeatView`는 하이어라키에 배치된 `Leaderboard Open Button`/`Leaderboard Panel`/`Ranking Button` 등을 이름으로 바인딩하고, 필수 오브젝트가 없으면 생성 대신 오류를 낸다.
- 2026-06-23: ADR-0019(R3 reactive ViewModel) 위에서 방향을 MVP + Reactive ViewModel로 확정(ADR-0020). 흐름 제어(전투/보상/광고/패배/부활/튜토리얼/네비게이션)를 `RunGameSceneRoot`에서 순수 C# `RunGameFlowController`로 추출했다. SceneRoot는 생성·View 바인딩·event 배선·초기 GoTo만 유지한다. FlowController는 SceneRoot의 destroy 토큰을 주입받아 부활 카운트다운·로비 복귀 비동기 작업을 자동 취소한다. `RunRewardViewModel`의 보상 추첨/적용은 중복 추첨 방지를 위해 의도적으로 유지했다. `dotnet build SlotRogue.slnx`는 경고 0·오류 0으로 PASS. View.Bind 전환과 잔여 event→R3 통일은 다음 단계로 남긴다.
- 2026-06-23 (정리 A): (A1) reactive ViewModel 안전망으로 `RunDefeatViewModelTests`(phase 전이·부활 게이트·카운트다운·RESTART/REVIVE intent) 7종, `RunRewardViewModelTests`(옵션 추첨·claim 1회·리롤/2배 라벨·광고 제거 gating) 6종을 추가했다. (A2) 6개 View가 복붙하던 `FindDeepChild`/`FindChildComponent`/`SetText`/`HasText`/`SetTextColor`/`SetActive`/`AppendMissing`를 `ViewComponentBase`(추상 MonoBehaviour)로 모으고 `RunRewardView`/`RunDefeatView`/`RunInventoryView`/`RunTutorialOverlayView`가 상속하도록 바꿔 호출부 변경 없이 중복을 제거했다(`RunDefeatView`의 nested `SymbolStatRow`는 상속된 `FindDeepChild`를 그대로 사용). (A3) 보상 추첨(RNG+RunRewardCatalog)·적용(GameFlowSession)을 `IRunRewardService`/`RunRewardService`로 분리해 `RunRewardViewModel`이 GameFlowSession/Catalog/Random을 직접 참조하지 않고 화면 상태만 갖도록 했다(생성자 기본값으로 기존 `new RunRewardViewModel()` 호출 보존, 테스트용 주입 가능). `dotnet build SlotRogue.slnx` 경고 0·오류 0 PASS.
- 2026-06-23 (GameStart 분리): `GameStartSceneRoot`(728줄)에서 로비 우주 배경 행성 애니메이션을 순수 헬퍼 `LobbySpaceBackground`(Bind(scene)/Tick(dt))로, 임시 디버그 버튼 UI 생성·배선을 `LobbyDebugButtons`(Install(canvas, 4 callbacks)/Dispose())로 추출했다. 디버그 버튼의 "무엇을 하는가"(튜토리얼 시작/스킵/초기화·광고 초기화)는 SceneRoot가 콜백으로 보유한다. SceneRoot는 ~330줄로 줄어 ViewModel 생성·View 바인딩·시작/종료 흐름·IAP 버튼 구성만 담당한다. 헬퍼는 MonoBehaviour가 아니라 SceneRoot가 소유·구동하므로 씬/프리팹 변경이 없다. `GameStartViewModel`은 화면 상태가 없어 R3화 보류. `dotnet build SlotRogue.slnx` 경고 0·오류 0 PASS.
- 2026-06-23 (테스트 토대): `RunGameFlowController`가 의존하던 MonoBehaviour 협력자를 인터페이스로 추상화했다 — `IRunGameNavigator`(RunGameNavigator), `IBattleSceneController`(BattleSceneCompositionRoot), `ITutorialOverlay`(RunTutorialOverlayView), `IDefeatPortraitView`(RunDefeatView), battleView는 기존 `IRunGameView` 재사용. 리더보드는 null-체크만 쓰여 View 참조 대신 `bool hasInSceneLeaderboard`로 대체했다. 생성자가 인터페이스를 받으므로 순수 C# 단위 테스트가 가능해졌고, fake 협력자 + 실제 ViewModel로 `RunGameFlowControllerTests` 7종(전투 진입/시작 유물→전투/승리→보상/보상 수령→다음 전투(GoTo·View 재진입)/인벤토리 열고닫기)을 추가했다. 광고·씬 이동 static 경로는 테스트에서 제외. `dotnet build SlotRogue.slnx` 경고 0·오류 0 PASS, Unity Test Runner 실행은 대기.
- 2026-06-23 (seam): View가 입력을 전달하는 presenter를 구체 `RunGameFlowController` 대신 `IRunGameFlow` 인터페이스에 의존하도록 분리했다(`StartArtifactSelectionView`/`RunRewardView`/`RunInventoryView`/`BattleView`의 `Bind` 시그니처). View 단위 테스트에서 presenter를 대역으로 교체 가능. SceneRoot는 구체 컨트롤러를 그대로 주입한다. `dotnet build SlotRogue.slnx` 경고 0·오류 0 PASS.
- 2026-06-23 (3단계): `LeaderboardViewModel`을 `event Action<LeaderboardViewState> Changed` + `State { get; private set; }`에서 `ReactiveProperty`/`ReadOnlyReactiveProperty State`로 전환했다. `LeaderboardView.Bind(vm)`가 `State.Subscribe(Render).AddTo(this)`와 close/refresh 입력(ViewModel command)을 소유한다. `RunGameSceneRoot`·`GameStartSceneRoot`의 `Changed +=/-=` 거울쌍과 초기 `Render(vm.State)` 호출을 제거했고, RunGame에서만 쓰이던 `RunGameFlowController.HandleLeaderboardRefreshRequested`와 GameStart의 `HandleLeaderboardRefreshRequested`(dead)를 삭제했다. launcher의 `OpenRequested`는 씬마다 진입 경로가 달라 SceneRoot가 연결한다. GameStart 로그인 View는 같은 VM 상태를 렌더하므로 SceneRoot가 `State.Subscribe(_loginView.Render).AddTo(this)`로 구독을 소유한다. `dotnet build SlotRogue.slnx` 경고 0·오류 0 PASS. `CombatViewModel`/`BattleView`는 ADR-0019대로 명령형 유지.
- 2026-06-23 (2단계): State→View 구독을 `RunGameSceneRoot.SubscribeEvents`에서 각 View의 `Bind(viewModel, presenter)`로 이동했다. `StartArtifactSelectionView`/`RunRewardView`/`RunInventoryView`/`RunHUDView`/`RunDefeatView`가 `viewModel.State.Subscribe(Render).AddTo(this)`로 자기 ViewModel을 직접 구독하고, View 입력 event를 presenter(`RunGameFlowController`) 또는 ViewModel command로 연결한다. Start/Reward는 `RelicIconRenderer`를 주입받아 구독 콜백에서 아이콘까지 렌더한다. SceneRoot는 `BindViews`에서 `view.Bind(...)`만 호출하고, View-input event의 수동 `-=`/거울쌍을 제거했다(View가 publisher라 파괴 시 자동 해제). VM→presenter intent event, Ads/Leaderboard/BattleSceneCompositionRoot 배선만 SceneRoot에 남는다. `dotnet build SlotRogue.slnx` 경고 0·오류 0 PASS. BattleView·LeaderboardView는 reactive ViewModel이 없어 다음 단계 대상.
- 2026-06-19: `LeaderboardView`에서 runtime UI 생성 코드를 제거하고 하이어라키에 배치된 `Leaderboard Open Button`, `Leaderboard Panel`, `Close Button`, entry row/detail panel을 바인딩하도록 바꿨다. 리더보드 패널의 profile save/input 흐름은 제거하고 로비 프로필 입력(`20_LogInArea`)만 프로필 저장을 담당한다. 랭킹 metadata는 최고 Wave score, 보유 유물 id, 심볼별 확률 가중치, profile icon id, message를 저장한다.

## Completion

- **Finished**:
- **Outcome**:
- **Follow-ups**:
