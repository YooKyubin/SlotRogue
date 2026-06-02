# 플로팅 전투 텍스트 프리팹화

**Status**: active  
**Started**: 2026-06-02  
**Owner**: Yookyubin  
**Contributors**: _(없음)_  
**Related design-docs**: [`combat-core.md`](../../design-docs/combat-core.md), [`game-flow.md`](../../design-docs/game-flow.md)  
**Related ADR**: [ADR-0003](../../adr/0003-combat-presentation-replay.md)  
**Depends on**: [`feature-combat-presentation`](../completed/feature-combat-presentation.md) (완료), [`feature-run-battle-presentation`](../completed/feature-run-battle-presentation.md) (완료)

## Background

전투 연출 Replay는 ADR-0003과 [`feature-combat-presentation`](../completed/feature-combat-presentation.md)에서 Dev_Battle 기준으로 구현했고, [`feature-run-battle-presentation`](../completed/feature-run-battle-presentation.md)에서 RunBattle Spin 흐름까지 연결했다.

현재 플로팅 데미지 숫자는 `DamagePresenter.ShowFloatingDamageAsync`가 런타임 `GameObject`와 `UnityEngine.UI.Text`를 직접 만들고, 위치·크기·색·fade 시간·상승 속도를 코드 상수로 가진다. `RunBattleView` / `GameFlowScenePrefabBuilder`가 만든 `battle/presentation-overlay`는 parent root만 제공하며, `CombatPresentationHost.DefaultFont`는 builtin `LegacyRuntime/Arial`에 의존한다. `PhaseChangedPresenter`의 턴 배너도 비슷한 runtime Text spawn 구조라 같은 튜닝 pain point가 있다.

본 plan은 **플로팅 데미지 숫자**를 Inspector/프리팹 중심으로 옮기는 기능 단위 작업이다. 수명은 3~7일을 목표로 하며, Core/asmdef 변경은 원칙적으로 하지 않는다.

## Requirements

1. `FloatingDamageText.prefab`을 만든다. crit는 선택적으로 `FloatingDamageTextCritical.prefab` variant를 두거나, 단일 prefab + `Play` 파라미터로 처리한다.
2. `FloatingDamageTextView` 또는 동등한 `MonoBehaviour`를 둔다. 기본 API는 `Play(int amount, bool isCritical, CombatAnchorKind anchorKind, CancellationToken ct)` 또는 같은 의미의 얇은 호출 형태다.
3. `FloatingDamageTextView`는 DOTween/UniTask로 이동·fade·scale 등 연출 완료를 await 가능하게 만들고, `async void`를 쓰지 않는다.
4. `DamagePresenter`는 runtime Text 생성 로직을 제거하고, prefab instantiate + View `Play` 호출만 담당한다.
5. 튜닝 데이터는 prefab SerializeField를 기본안으로 검토하되, 여러 전투 텍스트가 같은 값을 공유해야 하면 `CombatPresentationSettings` ScriptableObject hybrid를 선택한다.
6. 플레이어/몬스터 spawn 위치는 코드 offset 대신 anchor `RectTransform`으로 옮기는 것을 권장한다. Dev_Battle과 RunBattle이 같은 의미의 anchor를 제공해야 한다.
7. Dev_Battle(`BattleDevHarness`)과 RunBattle(`RunBattleController`)은 같은 floating damage prefab/view 자산을 사용한다.
8. 선택 Phase로 `PhaseChangedPresenter` 턴 배너 prefab화를 검토하되, 플로팅 데미지 작업이 길어지면 별도 plan으로 분리한다.
9. 범위 밖: Addressables, TMP 전체 마이그레이션, Core 변경, pooling 필수화, HP bar tween(`CombatPresenterBase` + `CombatPresentationTweens`) 구조 변경.

## Goal

디자이너/프로그래머가 코드 수정 없이 Inspector와 prefab에서 데미지 숫자의 폰트, 기준 위치, 크기, 색, crit 강조, 이동 거리, fade/scale 시간 등을 조정할 수 있게 한다. Dev_Battle Apply Turn과 RunBattle Spin은 같은 floating damage 자산을 사용하고, 기존 ADR-0003 Replay 순서와 ViewModel HUD sync는 유지한다.

## Baseline (현재 vs 목표)

| 항목 | 현재 | 목표 |
|------|------|------|
| 데미지 텍스트 생성 | `DamagePresenter`가 runtime `GameObject` + `Text` 생성 | `FloatingDamageText.prefab` instantiate 후 `FloatingDamageTextView.Play` |
| 폰트 | `CombatPresentationHost.DefaultFont` → builtin `LegacyRuntime/Arial` | prefab의 `Text` 필드 또는 설정으로 교체 가능 |
| 위치 | 코드 offset: 플레이어 -120, 몬스터 40 | Player/Monster anchor `RectTransform` 기준 |
| 크기 | 코드 상수: 일반 28, crit 34 | prefab SerializeField 또는 settings |
| 색 | 코드 상수 | prefab SerializeField 또는 settings |
| 연출 | 코드 상수 fade 0.55s + 상승 속도 | Inspector에서 duration, move distance, scale/fade curve 조정 |
| Dev_Battle / RunBattle | 같은 presenter지만 root·offset 튜닝은 코드 의존 | 같은 prefab과 같은 anchor contract 사용 |
| 턴 배너 | `PhaseChangedPresenter` runtime Text spawn | 선택 Phase B에서 banner prefab으로 분리 |
| HP bar tween | `CombatPresenterBase` + `CombatPresentationTweens` | 범위 밖, 기존 구조 유지 |

## Phases

---

### Phase 1 — FloatingDamageText prefab + View

- [x] `FloatingDamageText.prefab` — `UnityEngine.UI.Text` 기반 MVP 자산 생성, `.meta` 동반
- [x] `FloatingDamageTextView` — amount/crit/anchorKind 입력, text formatting, color/scale 선택
- [x] `FloatingDamageTextView.Play` — DOTween Sequence 또는 tween handle을 UniTask로 await
- [x] tween lifecycle — `SetLink(gameObject)` 또는 명시적 `DOKill`로 destroy/disable 안전성 확보
- [x] crit 처리 방식 확정 — **단일 prefab + SerializeField 파라미터** (`_criticalFontSize`, `_criticalColor`, `_criticalPrefix`, scale punch)

**🔍 Review:** Dev_Battle overlay 아래에 prefab을 수동 배치하거나 임시 instantiate해 일반/crit 숫자가 Play Mode에서 이동·fade 후 정리되는지 확인.

---

### Phase 2 — DamagePresenter 리팩터 + Host 참조

- [x] `CombatPresentationHost` — floating damage prefab 참조와 player/monster anchor 참조 추가
- [x] `DamagePresenter` — `ShowFloatingDamageAsync`의 runtime Text spawn 제거
- [x] `DamagePresenter` — target participant/anchorKind 결정 후 prefab instantiate + `FloatingDamageTextView.Play`
- [x] `DamagePresenter` — 기존 HP/Shield tween과 floating text await 순서 유지 (`Effect` 내부 병렬 규칙 유지)
- [x] `CombatPresentationPipeline.CreateDefault` 호출부 — Host 필드 미할당 시 명확한 fallback 또는 에러 로그 정책 확정 (warning log + early return)

**🔍 Review:** Dev_Battle Apply Turn에서 기존 데미지 연출 순서, HP tween, BattleEnded 순서가 깨지지 않는지 Play Mode로 확인.

---

### Phase 3 — Dev_Battle + RunBattle anchor/binding

- [x] Dev_Battle runtime UI — `FloatingTextRoot` 아래 player/monster damage anchor `RectTransform` 추가 (`BattleDevHarness.CreateUi`)
- [x] `BattleDevHarness` — Host에 floating damage prefab과 anchor 바인딩
- [x] `RunBattleView.prefab` — `battle/presentation-overlay` 하위 player/monster damage anchor 추가
- [x] `RunBattleController` — Host에 동일 prefab과 RunBattle anchor 바인딩 (view anchor 우선 + fallback 생성)
- [x] `GameFlowScenePrefabBuilder` — Rebuild 메뉴가 overlay/anchor/prefab 참조를 재생성하도록 갱신
- [x] Rebuild 후 diff 확인 — prefab/scene YAML과 `.meta`만 의도대로 변경됐는지 검토

**🔍 Review:** Dev_Battle Apply Turn과 RunBattle Spin 양쪽에서 플레이어 피격/몬스터 피격 숫자가 각 anchor 근처에 뜨는지 Play Mode로 확인.

---

### Phase 4 — Play Mode 체크리스트

**담당:** Unity **Play Mode 수동 테스트**. 자동 테스트는 UI 연출 특성상 필수 아님. 코드 리뷰와 컴파일 green은 Phase 4 전제 조건.

- [ ] Dev_Battle Apply Turn — 일반 damage, crit, player hit, monster hit, multi-hit 시나리오 확인
- [ ] RunBattle Spin — 일반 damage, crit, player hit, monster hit, multi-hit 시나리오 확인
- [ ] 연출 중 scene disable/destroy 또는 전투 종료 시 DOTween callback/null reference 오류 없음
- [ ] 최종 HUD HP/Shield는 턴 종료 `SyncFrom` 후 Core state와 일치
- [ ] 팀 Playtest 1회 — 위치·크기·색·속도가 모바일 세로 화면에서 읽히는지 확인

**🔍 Review:** 아래 표의 Dev_Battle 5개, RunBattle 5개 시나리오를 같은 빌드/에디터 세션에서 통과 처리.

#### Play Mode 체크리스트 (수동)

| 영역 | # | 시나리오 | 조작 | 기대 |
|------|---|----------|------|------|
| Dev_Battle | 1 | 몬스터 일반 피격 | `Damage > 0`, `IsCritical = false`, Apply Turn | 몬스터 anchor에서 일반 크기/색 숫자, HP tween과 순서 일치 |
| Dev_Battle | 2 | 몬스터 crit 피격 | crit context가 true인 request로 Apply Turn | crit 크기/색/scale이 일반과 구분되고 같은 prefab contract 사용 |
| Dev_Battle | 3 | 플레이어 피격 | 적 턴 damage가 발생하도록 Apply Turn | 플레이어 anchor에서 숫자 표시, 몬스터 anchor와 위치가 섞이지 않음 |
| Dev_Battle | 4 | multi-hit | `AttackCount >= 3` request로 Apply Turn | 여러 숫자가 순차 이벤트 간격을 유지하고 이전 인스턴스가 정상 정리 |
| Dev_Battle | 5 | 사망 직전/사망 | lethal damage request로 Apply Turn | 마지막 damage 숫자 완료 후 `BattleEnded` 연출/상태 표시 |
| RunBattle | 1 | 패턴 hit 일반 damage | `GameStart` → RunBattle → 패턴 매칭 Spin | 몬스터 anchor에서 damage 숫자, 슬롯 강조와 전투 Replay 순서 유지 |
| RunBattle | 2 | crit 또는 crit-equivalent context | crit가 가능한 request/context로 Spin | crit 표현이 Dev_Battle과 동일 자산 기준으로 표시 |
| RunBattle | 3 | 플레이어 피격 | 적 턴 damage가 발생하도록 Spin 진행 | 플레이어 HUD 근처 anchor에서 damage 숫자 표시 |
| RunBattle | 4 | multi-hit Spin | 다타 request가 나오도록 Spin 반복 | 숫자 overlap이 읽을 수 있는 수준이고 최종 HP가 Core state와 일치 |
| RunBattle | 5 | 승리/패배 전환 | 몬스터 또는 플레이어 HP 0까지 진행 | scene 전환/결과 버튼 표시 중 tween leak, null reference, orphan text 없음 |

---

### Phase 5 — 문서 정리와 완료 처리

- [ ] 본 plan 체크리스트·Notes·Completion 갱신 후 `git mv`로 `docs/exec-plans/completed/` 이동
- [ ] [`docs/STATUS.md`](../../STATUS.md) — Active 제거, Recently completed 추가, `_Last updated` 갱신
- [ ] [`feature-combat-presentation`](../completed/feature-combat-presentation.md) — Follow-ups의 prefab/SO 튜닝 항목 완료 cross-ref
- [ ] [`feature-run-battle-presentation`](../completed/feature-run-battle-presentation.md) — Follow-ups의 플로팅 데미지 prefab/SO 튜닝 항목 완료 cross-ref
- [ ] [`combat-core.md`](../../design-docs/combat-core.md), [`game-flow.md`](../../design-docs/game-flow.md) — Presentation/overlay 섹션 cross-ref 한 줄 필요 여부 확인
- [ ] inbound 참조 grep — active/completed 경로와 plan 링크 깨짐 없음

**🔍 Review:** `docs/INDEX.md`, `docs/STATUS.md`, active/completed README와 plan 링크가 서로 일치하는지 확인.

---

### Phase B — 턴 배너 prefab화 (선택)

- [ ] `PhaseChangedPresenter` runtime Text spawn 구조를 floating damage 작업과 같은 기준으로 평가
- [ ] `TurnPhaseBanner.prefab` + `TurnPhaseBannerView.Play(phase, ct)`로 분리할지 결정
- [ ] 같은 `CombatPresentationHost` overlay root를 쓰되 damage anchor와 banner anchor를 분리
- [ ] 일정이 3~7일을 넘기면 본 Phase는 별도 `feature-turn-banner-prefab.md` plan으로 분리

**🔍 Review:** 선택 구현 시 Dev_Battle과 RunBattle에서 Resolving / EnemyTurn / PlayerTurn 배너가 prefab 자산으로 표시되는지 Play Mode로 확인.

## Alternatives considered

| 대안 | 장점 | 단점 | 판단 |
|------|------|------|------|
| Prefab only | 가장 빠름, Inspector에서 폰트·색·duration을 바로 조정, variant로 crit 분기 쉬움 | 여러 prefab이 같은 값을 공유할 때 중복 튜닝 가능 | **기본안**. MVP에는 충분하다. |
| ScriptableObject only (`CombatPresentationSettings`) | Dev_Battle/RunBattle 공통 값 관리, diff가 명확함 | 실제 UI 구조와 폰트/Graphic 참조는 결국 prefab이 필요, 과한 추상화 가능 | 단독 사용은 보류. |
| Hybrid (prefab + settings) | 자산 구조는 prefab, 공통 수치는 SO로 공유 가능 | 참조 경로가 늘고 미할당 오류 표면 증가 | crit/배너/힐/실드까지 확장할 때 채택 후보. |
| 코드 상수 유지 | 구현 없음 | 목표와 반대, 튜닝마다 코드 수정 필요 | 거절. |

새 ADR은 작성하지 않는다. ADR-0003의 Replay/Presenter 경계 안에서 UI 자산화 수준을 정하는 구현 plan이며, Core API나 연출 모델 결정은 바꾸지 않는다.

## Anchor policy

플레이어/몬스터 위치는 코드 offset보다 **anchor `RectTransform`** 을 권장한다. RunBattle은 세로 모바일 레이아웃과 monster portrait/HUD 위치가 바뀔 가능성이 높고, Dev_Battle도 테스트 UI가 단순하므로 각 화면의 overlay 하위에 의미 기반 anchor를 두는 편이 유지보수에 유리하다.

권장 anchor 이름:

| Anchor | 의미 |
|--------|------|
| `player-damage-anchor` | 플레이어가 피해를 받을 때 숫자가 시작되는 기준점 |
| `monster-damage-anchor` | 몬스터가 피해를 받을 때 숫자가 시작되는 기준점 |
| `phase-banner-anchor` | 선택 Phase B에서 턴 배너가 표시되는 기준점 |

## Later (본 plan 범위 밖)

- Floating text pooling — multi-hit 빈도가 실제 성능 문제가 될 때 도입
- TMP 마이그레이션 — 전체 UI 텍스트 정책과 함께 별도 plan/ADR 후보
- Heal/Shield floating text — `FloatingCombatTextView`로 일반화할지 별도 검토
- Addressables — 전투 연출 자산 로딩 전략 결정 후 적용
- 연출 스킵·2x speed — Replay 전체 속도 제어와 함께 처리
- Screen-safe area / mobile notch 대응 — 전투 UI polish 단계에서 검토

## Notes

- Core, `BattleSystem`, `CombatEvent`, asmdef 변경은 원칙적으로 하지 않는다.
- `SlotRogue.UI.Combat`의 기존 UniTask/DOTween 사용 규칙을 따른다. `async void` 금지, `CancellationToken` 전달, `OperationCanceledException`은 상위 flow 정책과 맞춘다.
- DOTween tween은 `SetLink(gameObject)` 또는 handle 보관 + `DOKill`로 scene unload/destroy 시 callback 누수를 막는다.
- prefab/scene/ScriptableObject를 추가하거나 갱신하면 `.meta`를 같은 커밋에 포함한다.
- `UnityEngine.UI.Text` 기반 MVP를 유지한다. TMP 전환은 Later이며 이 plan에서 부분 도입하지 않는다.
- `CombatPresentationTweens`와 HP bar tween은 이번 plan에서 구조를 바꾸지 않는다. 필요하면 View가 같은 helper를 재사용하는 정도만 허용한다.

## Completion

_(completed/로 옮길 때 채움.)_

- **Finished**:
- **Outcome**:
- **Follow-ups**:
