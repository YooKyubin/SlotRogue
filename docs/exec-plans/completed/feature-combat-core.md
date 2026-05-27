# 전투 코어 (Combat Core)

**Status**: completed  
**Started**: 2026-05-26  
**Finished**: 2026-05-27  
**Owner**: _(전투 담당 — GitHub id 기입)_  
**Contributors**: _(슬롯 담당 — Phase B `ISpinCombatConsumer` 연동은 follow-up)_  
**Related design-docs**: [`combat-core.md`](../../design-docs/combat-core.md) (C1–C10)

## Goal

[`combat-core.md`](../../design-docs/combat-core.md)에 합의된 전투 규칙을 **슬롯·UI 없이** 먼저 검증 가능한 코드로 만든 뒤, `BattleTest`에서 Mock 스핀으로 한 턴이 돌아가게 한다. Phase A는 EditMode 테스트 녹색이 완료 기준, Phase B는 씬에서 HP·턴 변화 확인이 완료 기준이다.

## Phase A — 로직만 (슬롯·UI 없음)

**완료 기준**: EditMode 테스트 전부 통과. Unity 씬·연출 없이도 `dotnet test` / Test Runner로 검증 가능.

- [x] `Assets/_Project/Scripts/` 폴더 및 asmdef 생성 (`SlotRogue.Core`, `SlotRogue.Data`, `SlotRogue.Core.Tests` EditMode)
- [x] `CombatSpinOutcome`, `ISpinCombatConsumer` (`SlotRogue.Core.Combat`)
- [x] `BattleState` (HP, `PatternIndex`, `PendingMonsterDefense`, 승패 플래그)
- [x] `BattleResolver` — C1 턴 순서, C2 피해식, C3 Player→Monster, C5 패턴 인덱스·루프
- [x] `BattleResolver` — C7 Defend pending, C8 HP≤0 승패(동시 사망=패배), C9 attack 0 턴 진행
- [x] `MonsterActionDefinition`, `MonsterPattern`, `PatternStep`, `MonsterDefinition` SO 스케치 (`SlotRogue.Data`)
- [x] EditMode: C2 `max(0, atk - def)` 플레이어·몬스터 대칭
- [x] EditMode: 패턴 0→1→…→loop 및 `OverrideRawAttack`
- [x] EditMode: Defend → 다음 스핀 PlayerPhase 소비·미소비 시 pending 유지(C7·C9)
- [x] EditMode: 승리/패배/동시 사망(C8)

## Phase B — 씬·연동 준비

**완료 기준**: `BattleTest`에서 Mock `OnSpinResolved`로 몬스터 HP·턴 진행 확인. 슬롯 팀에 인터페이스 시그니처 공유.

**검증 (2026-05-27)**: EditMode 테스트 통과 + Play Mode 수동 확인(HP·패턴·승패 Console 로그, Mock 버튼 동작).

- [x] `BattleResolver`가 `ISpinCombatConsumer` 구현(C10)
- [x] `BattleTest` 씬 — Mock 스핀 버튼(고정 `CombatSpinOutcome`) → Resolver 주입 (`Assets/_Project/Scenes/BattleTest.unity`)
- [x] 샘플 SO 인스턴스 1세트 (`GoblinPattern`, `Slash`/`Guard` 액션) — `Assets/_Project/Data/Combat/`
- [x] `BattlePresenter` 스텁 — 전투→UI 이벤트 발행만(HP 변경·`MonsterActionExecuted`·`BattleEnded`, 로그 OK)

## Notes

- EditMode 테스트 기본값: `playerMaxHp = 30`, `monsterMaxHp = 50` (`BattleResolverTests`).
- Unity Test Runner: `Window > General > Test Runner > EditMode`, assembly `SlotRogue.Core.Tests`.
- 구현 순서: **Phase A 전부 → Phase B**. Phase B 중 UI 연출·DOTWEEN은 MVP 스텁 이후.
- `BattleResolver`는 스핀당 `OnSpinResolved` 1회만 처리(C1·C10). global `SpinResolved` 이벤트 사용 안 함.
- `BattleTest` EventSystem: `InputSystemUIInputModule` GUID는 패키지 버전별로 다름. 씬에 Missing Script가 보이면 `GameObject > UI > Event System`으로 재생성.
- 구현 커밋: Phase A `c262d7e`, Phase B `77d494c`.

## Completion

- **Finished**: 2026-05-27
- **Outcome**: `BattleResolver`(C1–C10) + EditMode 테스트, `ISpinCombatConsumer`/`BattlePresenter`, Goblin 샘플 SO, `BattleTest` Mock 스핀 씬(Play Mode 검증). 슬롯·실 UI 없이 전투 코어 MVP 달성.
- **Follow-ups**:
  - 슬롯 팀: `slot-core.md` 작성 후 `OnSpinResolved` 호출 타이밍 확정 및 `ISpinCombatConsumer` 연동 (`feature-slot-core` 등 별도 plan).
  - UI: `BattlePresenter` 구독 HP 바·피격/승패 연출 (`SlotRogue.UI` asmdef).
  - Buff/Special 몬스터 액션 구현(C4 범위 밖이면 design-doc 갱신 후).
