# ADR-0003: 전투 연출은 CombatEvent Replay로 처리한다

**Status**: proposed  
**Date**: 2026-05-31  
**Supersedes**: none  
**Superseded by**: none  
**Related design-docs**: [`docs/design-docs/combat-core.md`](../design-docs/combat-core.md)

---

## Context

전투 코어(`BattleSystem`)는 ADR-0001에 따라 동기 Effect 파이프라인과 `CombatEvent` 로그만 제공한다. Dev 씬은 Console 로거로 이벤트를 **일괄 출력**하며, Effect마다 이펙트·UI·VFX를 재생하고 끝날 때까지 기다린 뒤 다음 단계로 진행하는 **연출 레이어**는 없다.

슬롯 1스핀 = 1턴 구조에서는 스핀 결과가 확정된 뒤 연출이 이어지는 카드/로그라이크 UX가 자연스럽다. Core EditMode 테스트 자산을 유지하면서 모바일 MVP를 빠르게 검증하려면, Core API를 연출 친화적 step API로 전면 교체하기보다 **이벤트 재생(Replay)** 이 유리하다.

외부 검토(2건) 모두 MVP는 Replay, `EffectApplied`에 Before/After 스냅샷 추가, HUD는 `CombatViewModel` 경유, 슬롯 메타는 UI sidecar에 합의했다.

## Decision

- **MVP 연출 모델은 Replay(A)** 이다. `BattleSystem.ApplyPlayerTurn()`은 **동기 API를 유지**한다. UI의 전투 순서 관리자가 호출 전 이벤트 인덱스를 캡처하고, `BattlePresentationController`가 해당 인덱스 이후의 **새 `CombatEvent`만** 순서대로 `await` 재생한다.
- **`CombatEvent`는 디버그 로그가 아니라 Presentation Timeline의 한 스텝**으로 취급한다. `EffectApplied`, `ShieldReset`, `PhaseChanged`, `BattleEnded` 모두 동일 파이프라인에서 순차 `await` 한다.
- **`EffectApplied` 이벤트는 연출에 필요한 대상 Participant 스냅샷을 포함**한다: 적용 전·후 `Hp` 및 `Shield` (또는 동등한 `ParticipantSnapshot` struct). Presenter가 Core `Participant`를 폴링하거나 delta만으로 HP를 역산하지 않는다.
- **HUD·연출 표시 상태는 `CombatViewModel`** 을 통해서만 갱신한다. Core `CombatParticipant` HP/Shield에 UI를 직접 바인딩하지 않는다. 턴(또는 이벤트) 연출 종료 시 ViewModel을 Core authoritative state와 sync한다.
- **슬롯 메타** (`IsCritical`, `PatternName` 등)는 **`CombatEffect` struct를 확장하지 않고**, UI asmdef의 `PresentationContext` (sidecar DTO)로 `BattlePresentationController`가 Presenter에 전달한다. Core·Slot asmdef는 슬롯 연출 메타를 모른다.
- **비동기·트윈은 `SlotRogue.UI.Combat`만** 사용한다. `UniTask` / `DOTween`은 Core asmdef에 참조하지 않는다. `async void` 금지, `CancellationToken`(OnDestroy) 필수.
- **Effect 1건 내부** VFX·SFX·HUD는 기본 **`UniTask.WhenAll` 병렬**; 투사체→명중→HP처럼 **인과 관계가 있는 연출만 순차** `await` 한다. **Effect·이벤트 간** 순서는 항상 **순차** (`foreach` + `await`).
- **사망 연출**: 마지막 `EffectApplied`(Damage) 연출 **완료 후** `BattleEnded` Presenter를 재생한다. 즉시 Result UI만 띄우지 않는다.
- **Later**: 스킵·배속·리플레이·네트워크 동기화가 필요해지면 Core에 `BattleTurnSession` 등 **Step(B)** API를 도입할 수 있다. Presenter는 `CombatEvent`만 소비하므로 **Presenter·FlowController 계층은 유지**한다.

## Alternatives considered

- **B. Step — `BattleTurnSession.Advance()`로 Effect 1개씩 Core 적용** — 연출과 계산 lockstep, 스킵/리플레이에 유리. MVP 1~2일 스파이크에는 `BattleSystem` 리팩터·테스트 표면 증가 비용이 크다. Replay + 스냅샷으로 동일 UX 대부분 달성 가능해 **Later 후보**로 보류.
- **Core에 연출 코드 내장** (`BattleSystem`에서 DOTween/UniTask) — ADR-0001·EditMode 순수 테스트와 충돌. **거절**.
- **Replay without ViewModel — HUD가 `Participant` 직접 바인딩** — `ApplyPlayerTurn` 직후 Participant는 최종 HP라 연출 중 화면이 튄다. **거절**.
- **Replay without event snapshot — Presenter가 delta 역산** — shield·overkill 조합에서 Presenter 복잡도·버그 증가. **거절**; 스냅샷 추가가 비용 대비 효과가 큼.
- **`CombatEffect`에 crit/pattern 필드 추가** — 슬롯 DTO 개념이 Core Effect 모델에 스며듦. **거절**; UI `PresentationContext` sidecar.

## Consequences

- `docs/exec-plans/completed/feature-combat-presentation.md` — 구현 완료 (2026-05-31).
- Core: `CombatEvent` / `EffectApplied` 스냅샷 필드 추가 + 기존 `BattleSystemTests` 보강(값 assert 유지).
- UI: `BattlePresentationController`, `CombatPresentationPipeline`, Kind/Kind별 Presenter, `CombatViewModel`, `SlotRogue.UI` asmdef에 UniTask 참조.
- 초기 Play 검증은 `BattleDevHarness`의 `ApplyTurnAsync` 경로로 수행했다. 본편 통합과 Dev 씬 제거 후 하네스도 2026-06-12 삭제했다.
- 입력 잠금: `_isBusy` 또는 동등한 gate로 연출 중 스핀·턴 중복 호출 방지.
- ADR-0001 Notes의 "UI 타임라인·연출은 CombatEvent 소비자"가 구체화된다.

## Notes

- 설계 브레인스토밍·외부 AI 검토 합의: 2026-05-31.
- 구현 plan (완료): [`docs/exec-plans/completed/feature-combat-presentation.md`](../exec-plans/completed/feature-combat-presentation.md).
- 선행: [ADR-0001](./0001-combat-turn-effect-pipeline.md), [`feature-combat-core`](../exec-plans/completed/feature-combat-core.md), [`feature-combat-dev-scene`](../exec-plans/completed/feature-combat-dev-scene.md).
