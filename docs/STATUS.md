# 프로젝트 상태

_Last updated: 2026-06-12_

---

## 현재 포커스

**맵 Encounter SO** — [`feature-map-encounter-so`](./exec-plans/completed/feature-map-encounter-so.md) **완료** (2026-06-04): encounter·맵 노드/그래프 SO, roster 빌더, formation HUD. 현재 v1 런은 무한모드 tier 기반으로 정리되어 해당 맵 경로는 비활성이다.

**RunBattle 적 슬롯 프리팹** — [`feature-enemy-formation-slot`](./exec-plans/completed/feature-enemy-formation-slot.md) **완료** (2026-06-04): `EnemyFormationSlot` 프리팹, 슬롯 DamageAnchor + 플로팅 좌표 변환. `MonsterDefinition.portrait` 공급은 tier 기반 생성기로 정리하면서 후속 기획 확정 전까지 비활성이다.

**게임 플로우 루프** — [`feature-game-flow-loop`](./exec-plans/active/feature-game-flow-loop.md): 게임 시작 → 시작 유물 → 전투 → 보상 → 다음 전투로 이어지는 무한모드 루프. 전투 진입은 `CurrentBattleNumber`/`EncounterTier` 기반 `RunEncounterRosterBuilder.BuildForTier()` 단일 경로로 정리했으며, 전투 코드는 수정하지 않고 기존 API로 연결한다.

**유물 v23 런타임 단일화** — [ADR-0005](./adr/0005-relic-v23-runtime-model.md): v23 80종을 `RelicCatalog`에 등록하고 `OwnedRelics` 전체를 `RelicEffectRunner`로 연결했다. 시작 화면은 S-01~S-06 중 3종을 중복 없이 제시한다. 기존 전투 코어와 동작 계약이 다른 화상·감염 및 연계 유물은 보상풀에서 임시 제외했으며 전투 담당 구현 요청이 남아 있다.

**런타임 자산 로드 경계** — [ADR-0006](./adr/0006-runtime-asset-loading-boundary.md): 슬롯 패턴과 전투 UI의 `Resources.Load*` fallback을 제거하고 Prefab 직렬화 참조와 Composition Root 주입으로 전환했다. AssetBundle 그룹·배포 전략은 후속 결정으로 남긴다.

**Addressables 로컬 기준선** — [ADR-0007](./adr/0007-addressables-local-runtime-assets.md): Addressables 2.9.1과 `Default Local Group`을 적용하고 `SlotPatternCatalog`을 `slot/catalog/patterns` 키로 비동기 로드한다. Editor는 Fast Mode, Player 빌드는 Addressables 콘텐츠 동반 생성을 사용한다. 원격 배포 전략은 후속 결정으로 남긴다.

**유물 아이콘 키** — [ADR-0009](./adr/0009-relic-icon-addressable-keys.md): `RelicDefinition.IconKey`를 시작 선택·보상 ViewState까지 전달하고 `RunGameSceneRoot`가 Addressable Sprite를 캐시·해제한다. 현재 16종 시트는 `relic/icons/base`로 등록하며 유물별 `iconKey:` 덮어쓰기로 점진 확장한다.

**슬롯 MVP** — [`feature-slot-core`](./exec-plans/active/feature-slot-core.md): `Dev_Slot`에서 5×3 검증. 다음: 게임 플로우 전투 씬에서 `SlotCombatRequest` 연결.

**전투 연출 (Replay)** — [`feature-combat-presentation`](./exec-plans/completed/feature-combat-presentation.md) **완료** (2026-05-31): Dev_Battle Replay MVP + 턴 배너.

**Run Battle 전투 연출** — [`feature-run-battle-presentation`](./exec-plans/completed/feature-run-battle-presentation.md) **완료** (2026-06-01): RunBattle Replay 연동.

**다인전 전투 확장 (Core)** — [`feature-multi-participant-combat`](./exec-plans/completed/feature-multi-participant-combat.md) **완료** (2026-06-03): ADR-0004 roster·타겟·멀티히트·적 턴 + EditMode 테스트.

**다인전 플레이·UI (RunBattle)** — [`feature-multi-participant-play-ui`](./exec-plans/completed/feature-multi-participant-play-ui.md) **완료** (2026-06-04): 2/3몹 인카운터 hint, 몬스터별 HUD·타겟 선택·연출 anchor.

**RunGame Battle 화면 MVVM 정리** — [`feature-run-battle-mvvm`](./exec-plans/active/feature-run-battle-mvvm.md): 별도 `RunBattle` 씬은 없으며 `RunGame` 내부 `BattleView`를 세분화 View + 순수 ViewModel 구조로 전환 중이다. 씬 조립은 `BattleSceneCompositionRoot`, 턴 순서는 순수 C# `BattleFlowController`, HUD·입력은 `BattleScreenController`, 타겟 선택은 `BattleTargetSelectionController`, 런 결과 반영은 `RunBattleResultRecorder`, RunGame 화면 전환은 `RunGameSceneRoot`로 분리했다. 씬의 새 컴포넌트 저장과 구 호환 스크립트 삭제는 완료했고 RunGame 수동 플레이테스트가 남아 있다.

**전체 UI strict MVVM** — [`feature-ui-strict-mvvm`](./exec-plans/active/feature-ui-strict-mvvm.md): 현재 씬은 `BootScene`, `GameStart`, `RunGame`이며 전투는 `RunGame` 내부 `BattleView`다. ADR-0008에 따라 View는 렌더링·입력 event만 담당하고, 순수 ViewModel과 SceneRoot가 상태 및 흐름을 연결한다. 승리는 Reward View, 패배는 Defeat View로 버튼 입력 없이 자동 전환한다.

**몬스터 패턴 SO** — [`feature-monster-pattern-so`](./exec-plans/completed/feature-monster-pattern-so.md) 완료.

**몬스터 턴 스케줄** — [`feature-monster-turn-schedule`](./exec-plans/completed/feature-monster-turn-schedule.md) 완료 (Q2).

**전투 Dev 씬** — [`feature-combat-dev-scene`](./exec-plans/completed/feature-combat-dev-scene.md) 완료.

**전투 코어 MVP** — [`feature-combat-core`](./exec-plans/completed/feature-combat-core.md) 완료.

---

## 주차 마일스톤

High-level 마일스톤. 각 주차 안에서 기능 단위 exec-plan으로 분해한다.

- [ ] **Week 1 — playable core loop**
  - 스핀 시작 → 릴 회전 → 결과 결정 → 페이아웃 평가 → 결과 표시
  - 단위 테스트: RNG / 페이아웃
- [ ] **Week 2 — meta progression**
  - 런 구조, 노드 맵, 첫 보상 풀, 세이브/로드
- [ ] **Week 3 — content & balance**
  - 심볼·이벤트 콘텐츠 확장, 경제 1차 튜닝
- [ ] **Week 4 — polish & build**
  - 사운드, 연출, UX 정리, Android 빌드, 실기기 검증

---

## Active exec-plans

| Plan | Owner | Started | Goal (한 줄) |
|------|-------|---------|----------------|
| [`feature-game-flow-loop.md`](./exec-plans/active/feature-game-flow-loop.md) | _(슬롯 담당)_ | 2026-05-31 | 시작/유물/전투/보상/다음 전투 무한모드 루프 |
| [`feature-slot-core.md`](./exec-plans/active/feature-slot-core.md) | _(슬롯 담당)_ | 2026-05-28 | Dev_Slot에서 5 x 3 슬롯 MVP 테스트 |
| [`feature-attribute-combat-link.md`](./exec-plans/active/feature-attribute-combat-link.md) | _(전투 담당)_ | 2026-06-05 | 속성 유물을 전투 상태이상 컴포넌트 구조로 연결 |
| [`feature-run-battle-mvvm.md`](./exec-plans/active/feature-run-battle-mvvm.md) | _(슬롯 담당)_ | 2026-06-05 | RunGame Battle 화면 CompositionRoot + 세분화 View + ViewModel 정리 |
| [`feature-ui-strict-mvvm.md`](./exec-plans/active/feature-ui-strict-mvvm.md) | _(슬롯 담당)_ | 2026-06-11 | GameStart·RunGame 전체 화면 strict MVVM 통일 |

새 plan을 시작하면 [`exec-plans/active/`](./exec-plans/active/)에 `feature-<name>.md`로 추가하고 같은 커밋에 이 표에도 등록.

---

## Recently completed

| Plan | Finished | Outcome (한 줄) |
|------|----------|-----------------|
| [`feature-attribute-artifacts.md`](./exec-plans/completed/feature-attribute-artifacts.md) | 2026-06-11 | 구 Artifact 시작 유물 모델을 v20.3 RelicCatalog로 대체 |
| [`feature-enemy-formation-2d-world.md`](./exec-plans/completed/feature-enemy-formation-2d-world.md) | 2026-06-04 | RunBattle 몬스터 슬롯을 월드 2D Sprite + World Space HUD로 전환 |
| [`feature-enemy-formation-slot.md`](./exec-plans/completed/feature-enemy-formation-slot.md) | 2026-06-04 | EnemyFormationSlot 프리팹, MonsterDefinition.portrait, 슬롯 anchor·플로팅 좌표 정합 |
| [`feature-map-encounter-so.md`](./exec-plans/completed/feature-map-encounter-so.md) | 2026-06-04 | RunEncounterDefinition·맵 그래프 SO, roster 빌더, formation 3슬롯 HUD |
| [`feature-multi-participant-play-ui.md`](./exec-plans/completed/feature-multi-participant-play-ui.md) | 2026-06-04 | RunBattle 2/3몹 roster hint, 몬스터별 HUD·타겟 선택·id anchor 연결 |
| [`feature-multi-participant-combat.md`](./exec-plans/completed/feature-multi-participant-combat.md) | 2026-06-03 | 다인전 Core·타겟·멀티히트·적 턴 규칙 + EditMode 테스트, Dev_Battle 2몹 옵션 |
| [`feature-floating-combat-text.md`](./exec-plans/completed/feature-floating-combat-text.md) | 2026-06-02 | 데미지 텍스트를 prefab/view + anchor 기반으로 전환, Dev/RunBattle 공용 자산화 |
| [`feature-run-battle-presentation.md`](./exec-plans/completed/feature-run-battle-presentation.md) | 2026-06-01 | RunBattle Spin→RunTurnAsync, ViewModel HUD, overlay·플로팅 데미지·턴 배너 |
| [`feature-combat-presentation.md`](./exec-plans/completed/feature-combat-presentation.md) | 2026-05-31 | Replay 연출 MVP — Flow/Presenter/ViewModel, Dev_Battle RunTurnAsync, 턴 배너 |
| [`feature-monster-pattern-so.md`](./exec-plans/completed/feature-monster-pattern-so.md) | 2026-05-31 | 몬스터 패턴·정의 SO + Factory + Goblin asset, Harness SO-only |
| [`feature-monster-turn-schedule.md`](./exec-plans/completed/feature-monster-turn-schedule.md) | 2026-05-31 | `MonsterTurnSchedule` 턴 순환 + Dev_Battle 3턴 Inspector |
| [`feature-combat-dev-scene.md`](./exec-plans/completed/feature-combat-dev-scene.md) | 2026-05-31 | `Dev_Battle` Harness + Request 변환 + Console 이벤트 로거 |
| [`feature-combat-core.md`](./exec-plans/completed/feature-combat-core.md) | 2026-05-31 | `BattleSystem` 턴 파이프라인 + EditMode 테스트 20개 |

가장 최근 5~10개의 완료 plan만 표시. 전체는 [`exec-plans/completed/`](./exec-plans/completed/) 참조.

---

## Known issues / blockers

- **기획 문서 미확정**: 로그라이크 메타 / 경제.
- **브랜치 / PR 워크플로 미정**: 사용해본 후 결정 → ADR로 박제 예정.
- **ADR-0001 Status**: 구현 완료, `accepted` 전환은 팀 합의 후.

---

## 테스팅 단계

- **현재 Stage 1**: 결정론적 로직(슬롯 RNG, 페이아웃, 전투 Effect/턴)에 EditMode 단위 테스트.
- **다음 트리거**: 세이브 시스템 도입 시 PlayMode 통합 테스트 추가 (Stage 2).
- 상세는 추후 [`design-docs/`](./design-docs/)에 `testing-policy.md`로 분리.
