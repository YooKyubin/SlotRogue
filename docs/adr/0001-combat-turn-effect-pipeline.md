# ADR-0001: 전투 턴은 Effect 목록 파이프라인으로 처리한다

**Status**: proposed  
**Date**: 2026-05-30  
**Supersedes**: none  
**Superseded by**: none  
**Related design-docs**: [`docs/design-docs/combat-core.md`](../design-docs/combat-core.md), [`docs/design-docs/slot-core.md`](../design-docs/slot-core.md)

---

## Context

SlotRogue 전투는 슬롯 1스핀과 1:1로 대응하는 턴제 구조다. 기존 v1/v2 전투 구현은 2026-05-30 재설계로 제거되었고, 슬롯 MVP(`slot-core.md`)는 전투 타입을 참조하지 않은 채 `SlotCombatRequest` DTO만 준비해 두었다.

전투 코어는 (1) 슬롯 계산과 분리된 순수 로직, (2) 플레이어·몬스터 공통 효과 모델, (3) UI·연출을 위한 이벤트 로그, (4) 속성·버프 등 후속 확장 — 을 동시에 만족해야 한다. 팀은 턴 파이프라인을 먼저 고정하고 Participant 콘텐츠·슬롯 연동 세부는 이후 plan에서 구현하기로 했다.

## Decision

- **1스핀 = 1턴**으로 고정한다. 전투는 `StartBattle(player, monster)`로 시작하고, 플레이어 턴은 슬롯이 넘긴 **Effect 목록**을 `ApplyPlayerTurn(effects)`로 처리한다.
- Effect 1항은 **`Kind + Amount + Target`** 구조를 사용한다. MVP Kind는 `Damage`, `Shield`, `Heal`이며 Target은 `Self`, `Enemy`다. 플레이어·몬스터 행동 모두 동일 타입을 쓴다.
- Effect **적용 순서는 입력 목록 순서 그대로** 따른다. 전투 Resolver가 Kind별로 재정렬하지 않는다.
- **스탯 방어력은 없다.** 방어도는 `Shield` Effect로만 획득하며, `Damage` 적용 시 `실피해 = max(0, Amount - shield)`, 막은 만큼 shield를 차감한다. 플레이어 shield는 적 턴 종료 후 초기화하고, 몬스터 shield는 플레이어 턴 종료 후 초기화한다.
- HP·shield·maxHp는 **Participant(플레이어/몬스터) 객체 내부**에서 관리한다. `StartBattle`에는 Participant 참조를 넘긴다.
- 적 턴은 **미리 정해진 Effect 목록**을 순서대로 실행한다. MVP에서는 몬스터 행동 순환·패턴 배열 인덱스는 구현하지 않고 고정 1세트로 둔다.
- 턴 처리 중 발생한 단계(Phase 변경, Effect 적용, HP/shield 변경, 전투 종료)는 **CombatEvent 로그**로 남긴다. Resolver는 Unity `MonoBehaviour` / UI에 의존하지 않는다.
- **Slot asmdef는 Combat 타입을 참조하지 않는다.** 슬롯→전투 변환은 연동 계층(별도 plan)에서 `Effect[]`로 수행한다.

## Alternatives considered

- **단일 DTO(`SlotCombatRequest`)를 전투 입력으로 직접 사용** — 슬롯 MVP에 이미 존재하나, 피해·회복·방어를 한 객체 필드로 묶어 확장(속성, DoT, 다중 패턴) 시 Resolver 분기가 커진다. Effect 목록 + 공통 Kind 모델로 거절.
- **전투 Resolver가 Kind별 고정 적용 순서를 강제** (예: Shield → Heal → Damage) — 유물·패턴 조합이 늘면 슬롯·기획과 순서 불일치가 생긴다. 입력 순서 그대로 적용으로 거절.
- **스탯 방어력 + Shield 중첩** — 기획서의 “방어 행동 = 방어도 증가”와 맞추되, 상시 스탯 방어력은 MVP 범위 밖이다. Shield-only 모델로 거절.
- **몬스터 전용 행동 타입(`MonsterAction` enum)** — Resolver 이중화를 피하기 위해 Effect 공통 타입으로 통합. 별도 타입 거절.

## Consequences

- `SlotRogue.Core`(또는 전투 전용 asmdef)에 `CombatEffect`, `BattlePhase`, `BattleResolver`, `CombatEvent`, Participant 타입이 추가된다.
- EditMode 단위 테스트로 턴 순서·shield 소진·사막 시 적 턴 스킵·이벤트 로그를 검증할 수 있다.
- 슬롯 연동 plan에서 `SlotCombatRequest` → `CombatEffect[]` 변환 규칙과 asmdef 참조 방향을 별도로 정해야 한다.
- 속성 공격·DoT·버프는 Effect에 optional 메타 필드(`Element`, `Status`, `Duration` 등)를 추가하는 방식으로 확장한다. Kind를 `FireDamage`처럼 분기하지 않는다.
- UI 타임라인·연출은 CombatEvent 소비자로 분리한다. Resolver 본문에 연출 코드를 넣지 않는다.

## Notes

- 기획 narrative: [`docs/_scratch/combat-turn-pipeline-diagrams.md`](../_scratch/combat-turn-pipeline-diagrams.md) (gitignored 개인 scratch, 다이어그램 초안).
- 구현 완료: [`docs/exec-plans/completed/feature-combat-core.md`](../exec-plans/completed/feature-combat-core.md), [`feature-combat-dev-scene.md`](../exec-plans/completed/feature-combat-dev-scene.md) (슬롯→전투 변환·Dev_Battle), [`feature-monster-turn-schedule.md`](../exec-plans/completed/feature-monster-turn-schedule.md), [`feature-monster-pattern-so.md`](../exec-plans/completed/feature-monster-pattern-so.md) (몬스터 패턴 SO·Factory)
- 연출 결정: [ADR-0002](./0002-combat-presentation-replay.md) — [`feature-combat-presentation`](../exec-plans/completed/feature-combat-presentation.md) (완료)
