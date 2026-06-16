# 게임 플로우 루프

**Status**: active  
**Started**: 2026-05-31  
**Owner**: _(슬롯 담당)_  
**Contributors**: _(전투 담당: 코드 수정 없음)_  
**Related design-docs**: [`game-flow.md`](../../design-docs/game-flow.md)

## Goal

전투 코드를 수정하지 않고 `게임 시작 → 시작 유물 선택 → 맵 → 전투 → 보상 → 맵`으로 반복되는 playable loop를 만든다. `RunBattle`은 기존 슬롯 결과를 기존 전투 API에 연결한다.

## Checklist

- [x] `AGENTS.md`, 문서 인덱스, 슬롯/전투 design-doc 확인
- [x] 게임 플로우 ADR/design-doc 작성
- [x] 시작/유물/맵/전투/보상 씬 추가
- [x] `SlotRogue.UI.GameFlow` 런 상태와 씬 Controller 구현
- [x] 시작 유물/보상 후처리 구현 및 테스트 추가
- [x] 빌드 설정에 플로우 씬 등록
- [x] 컴파일 또는 Unity refresh 검증
- [x] 추후 sprite 교체용 기본 이미지 슬롯 UI 배치
- [x] 맵 자동 진입 제거, 현재 노드와 다음 노드 선택 UI 추가
- [x] 전체 맵 그래프, 연결선, 현재 위치, 선택 가능 노드 표시
- [x] 전투 씬 세로형 HUD 재배치, 몬스터 이미지 슬롯과 고정 5 x 3 슬롯 보드 표시
- [x] 런타임 bootstrap 제거, View 프리팹 + Controller 방식으로 씬 갱신
- [x] 전투 스핀 결과에서 기본 공격과 패턴 성공 강조 표시
- [x] 슬롯 결과 연출 큐와 패턴/유물/최종 결과 뷰 추가
- [x] 슬롯 연출 전용 `Dev_SlotPresentation` 씬 생성 메뉴 추가
- [x] `Dev_SlotPresentation` 데모 결과를 15칸 동일 보드의 족보 누적 연출로 고정
- [x] `Dev_SlotPresentation`에 배경/슬롯 아이콘 Texture 리소스 연결
- [x] `Dev_SlotPresentation` 연출 레이어와 Texture 리소스를 `RunBattle` 생성기에 연결
- [x] 인게임 캡쳐 기준으로 `RunBattle` HUD/슬롯/인벤토리 배치 정리
- [x] 현재 `RunBattle` UI 배치를 기준으로 Safe Area/해상도 대응 보정 추가
- [x] v20.3 `OwnedRelics`를 `RelicEffectRunner`로 전투 턴에 연결
- [x] 구 `ArtifactDefinitionSO` 전투 경로와 레거시 resolver 테스트 제거
- [x] v20.3 시작 유물 6종·복수 유물 합산 EditMode 테스트 추가
- [x] 슬롯 카탈로그와 전투 UI의 `Resources.Load*` fallback 제거 및 직렬화/Composition Root 주입 전환
- [x] `SlotPatternCatalog` Addressable 엔트리와 `BattleSceneCompositionRoot` 비동기 로드 경로 연결
- [x] 심볼 풀 시작 개수 차등(고확률 체리/클로버/종 6, 저확률 레몬/다이아/7 4) 적용 + EditMode 테스트
- [x] 보상 등급 분기: 일반전 = 심볼 추가/제거, 엘리트/보스 = 유물 선택 + EditMode 테스트
- [x] 슬롯 심볼 native size 기준 1.25배 표시와 작은 족보 순차 확대·원복·패턴별 유물 공격력 연출 복구
- [ ] Unity Editor에서 `GameStart`부터 Play 검증

## Notes

- 전투 **Core**·`BattleSystem`·asmdef는 수정하지 않는다. RunBattle **UI Replay 연출**은 [`feature-run-battle-presentation`](../completed/feature-run-battle-presentation.md) (2026-06-01 완료).
- MVP 맵은 전체 그래프를 보여주며, 현재 위치에서 연결된 다음 노드만 클릭해야 전투에 진입한다.
- MVP 전투 씬은 참고 이미지처럼 상단 HUD, 몬스터 전투 영역, 5 x 3 슬롯 보드, 하단 결과/스핀/자원 패널을 한 화면에 고정 배치한다.
- 각 플로우 씬은 `Assets/_Project/Prefabs/UI/GameFlow/`의 View 프리팹을 인스턴스로 들고, Controller는 배치된 참조만 갱신한다. 필요 시 Unity 메뉴 `SlotRogue > Game Flow > Rebuild Scene UI Prefabs`로 재생성한다.
- 시작 유물과 보상은 `SlotCombatRequest`를 후처리한다.
- `GameFlowImageSlot`의 `SlotId` 기준으로 배경/카드/전투/보상 이미지를 교체한다.
- 2026-05-31: Unity 생성 `.csproj`가 아직 새 GameFlow 파일을 포함하지 않아, 별도 `Temp/GameFlowCompileCheck`와 `Temp/GameFlowTestsCompileCheck`로 새 코드/테스트 컴파일 확인. 경고/오류 0개. Unity Editor 인스턴스가 없어 MCP refresh/Test Runner/Play 검증은 실행하지 못함.
- 2026-05-31: RunBattle에서 패턴 성공 시 `PATTERN HIT!` 문구와 매칭 셀 색상 강조를 표시. 패턴 실패 시 `BASE ATTACK`으로 기본 공격을 명시.
- 2026-06-02: `SlotRogue.UI.SlotPresentation`에 연출용 DTO/큐/Manager/View를 추가. RunBattle은 연출 완료 콜백 뒤 기존 전투 Effect 적용을 수행한다. Unity Editor 인스턴스가 없어 프리팹 재생성 및 Play 검증은 보류.
- 2026-06-02: DOTween 추가 후 패턴/유물/최종 결과 UI 애니메이션을 DOTween 기반으로 교체.
- 2026-06-03: `SlotPresentationDemoController`와 `SlotRogue > Slot Presentation > Rebuild Demo Scene` 메뉴를 추가. 배치 모드 생성은 프로젝트가 열린 Unity 인스턴스 때문에 실패했으므로, 현재 Editor에서 메뉴 실행 필요.
- 2026-06-03: `Dev_SlotPresentation` 데모는 모든 버튼이 15칸 체리 아이콘 보드에서 작은/가로/세로/대각선/큰 패턴을 순차 재생한 뒤 `Perfect Spin x15` 특별 패턴을 재생하도록 고정. 패턴 SFX는 `Assets/Resources/Sounds`의 `SFX_C_Low`부터 `SFX_C_High`까지 단계적으로 연결.
- 2026-06-03: `Assets/Resources/Textures`의 `Background_Outside`, `Background_Inside`, `Icon_Slot`을 데모 씬 생성 메뉴에 연결. 슬롯 칸 아이콘은 `Icon_Slot` sub-sprite, 유물 카드 아이콘은 첫 sub-sprite를 사용.
- 2026-06-04: `RunBattle` 생성기에 내부 배경, 슬롯 아이콘, `SlotPresentationManager` 레이어를 연결. 본편 스핀은 전체 패턴 목록을 연출 DTO로 만들고, 연출 완료 뒤 기존 전투 Effect를 적용한다.
- 2026-06-04: `Background_Inside`와 기존 인게임 UI Texture(`Ingame_Slot`, `Ingame_hp`, `Ingame_bt_spin`, `ingame_bt_pause`, `ingame_panel_coin`, `ingame_slot_potion1/2`, `ingame_bt_spec`) 기준으로 상단 재화/일시정지, 좌측 HP, 슬롯 상단 공격력, 하단 유물/포션 인벤토리, 중앙 스핀 버튼 위치를 재배치. 몬스터 리소스가 없는 동안 전투 임시 HUD는 생성하지 않고 null 바인딩으로 둔다.
- 2026-06-04: `RunBattleResponsiveLayout`을 추가해 현재 프리팹에 배치된 RectTransform 위치를 기준 offset으로 캡처하고, 플레이 시 상단 UI는 Safe Area top, 하단 UI는 Safe Area bottom, 슬롯/공격력/스핀은 하단 중앙 기준으로 보정한다.
- 2026-06-04: `icon_slot_ani.png` sub-sprite를 스핀 중 전용 아이콘으로 연결. 스핀 중에는 흔들린 아이콘 리소스와 위치/회전/스케일 jitter를 사용하고, 컬럼이 멈출 때 기존 정적 아이콘으로 확정한다.
- 2026-06-04: `Ingame_lever.png` sub-sprite를 스핀 레버 연출로 연결. 스핀 클릭 시 레버가 내려가고, 슬롯 결과 적용 후 다시 올라간다.

- 2026-06-04: `Spin Lever`를 런타임 생성 fallback이 아니라 `RunBattleView` 프리팹에 배치된 UI 오브젝트로 전환. 에디터에서 레버를 직접 확인할 수 있고, 프리팹 재생성 경로도 같은 레버 UI를 만든다.
- 2026-06-04: 기존 UI를 재생성하지 않고 `RunBattleView`의 레버만 생성/보정하는 `SlotRogue > Game Flow > Patch Run Battle Lever (Keep UI)` 메뉴를 추가.
- 2026-06-11: 첨부 v20.3 기획과 런타임을 대조한 결과 시작 선택은 `RelicCatalog`, 전투는 구 `ArtifactDefinitionSO`를 사용해 효과가 끊긴 상태를 확인. ADR-0005와 `relic-system.md`를 기준으로 단일화한다.
- 2026-06-11: `OwnedRelics → RelicTurnResolver → CombatTurnRequestBuilder → CombatEffect[]` 경로를 연결하고 시작 6종 중 3종 무작위 선택으로 변경. `dotnet build SlotRogue.slnx` 경고/오류 0. Unity Editor EditMode 실행과 Play 검증은 보류.
- 2026-06-11: HTML과 `RelicCatalog`의 63개 ID·이름 일치, 등급별 개수 일치, 활성 씬/프리팹/자산의 `_Legacy` GUID 참조 0건, 구 Artifact/Relic 생성 메뉴 0건을 확인했다.
- 2026-06-11: ADR-0006에 따라 슬롯 카탈로그와 전투 UI Sprite의 `Resources.Load*` 경로를 제거했다. Prefab 직렬화 참조와 전투 씬 조립 경계 주입을 사용하며 AssetBundle 정책은 후속 ADR로 남긴다.
- 2026-06-11: ADR-0007에 따라 Addressables 2.9.1 로컬 기준선을 적용했다. `SlotPatternCatalog`을 `slot/catalog/patterns`로 등록하고 현재 `BattleSceneCompositionRoot`에서 UniTask로 로드·해제한다. Editor Fast Mode와 Player 빌드 동반 생성을 설정했으며 `dotnet build SlotRogue.slnx --no-restore`는 경고·오류 0개로 통과했다.

- 2026-06-12: 유물 풀 v23.2 "심볼 확률은 풀 개수로만 조작" 원칙을 적용해 `SlotSymbolPool` 시작 개수를 균등 4에서 고확률 6/저확률 4로 차등화. 5x3 보드 이항분포 기준 "3개 이상 족보" 발동 확률이 약 60% vs 32%로, 유물 수치 보정 비율(+4 vs +7 등)과 기대값 동급이 된다.
- 2026-06-12: `RunRewardCatalog.ForTier`를 등급 분기로 변경 — 일반전은 심볼 추가(+1)/제거(-1, 최소 1개 유지) 보상, 엘리트는 Common+Uncommon 유물, 보스는 Uncommon 이상 유물. `RunRewardViewModel`/`GameFlowSession.ApplySymbolReward` 기존 경로 재사용으로 View 수정 없음. `dotnet build` 4개 asm 경고/오류 0, EditMode 테스트는 Unity Test Runner 실행 보류.
- 2026-06-13: 릴 심볼 표시 크기를 기존 대비 1.25배로 복구했다. 패턴 View가 숨겨진 레거시 셀이 아니라 현재 보이는 릴 심볼을 확대하고 원래 크기로 복귀하도록 연결했으며, 유물 발동 패턴 인덱스를 보존해 `패턴 → 해당 유물과 누적 공격력 증가 → 다음 패턴` 순서로 큐를 재생한다. Addressable 유물 아이콘을 전투 진입 시 캐시하고 `dotnet build SlotRogue.slnx --no-restore` 경고·오류 0개를 확인했다. Unity Play 검증은 보류.
- 2026-06-14: 심볼 1.25배의 기준을 셀/기존 고정 크기가 아니라 각 Sprite의 `Image.SetNativeSize()` 결과로 정정했다. 정적 심볼과 세로로 긴 스핀 심볼 모두 자신의 native width/height를 유지한 채 1.25배 표시한다.

## Completion

_(completed/로 옮길 때 채움.)_

- **Finished**:
- **Outcome**:
- **Follow-ups**:
