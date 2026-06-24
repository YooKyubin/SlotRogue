# ADR-0005: 유물 런타임 모델은 v23 RelicCatalog로 단일화한다

**Status**: accepted
**Date**: 2026-06-11
**Supersedes**: none
**Superseded by**: none
**Related design-docs**: [`docs/design-docs/relic-system.md`](../design-docs/relic-system.md), [`docs/design-docs/game-flow.md`](../design-docs/game-flow.md)

---

## Context

프로젝트에는 시작 유물용 `ArtifactDefinitionSO`, 범용 유물용 `RelicDataSO`, 코드 카탈로그용 `RelicDefinition`이 동시에 존재했다. 시작 선택 화면과 보상 화면은 `RelicCatalog`를 사용하지만, 전투는 구 `ArtifactDefinitionSO` 경로로 효과를 계산해 선택 데이터와 전투 데이터가 분리되어 있었다.

최신 첨부 기획 `relic_pool_v23_status_balance_patch.html`은 80종 유물과 시작 유물 6종을 기준으로 정의한다. v23의 화상·감염·취약·약화·가시는 당시 전투 코어의 화상·독·빙결 동작과 동일하지 않으므로, 카탈로그 데이터 반영과 실제 전투 활성화를 분리해야 한다.

## Decision

유물 선택, 런 인벤토리, 보상 풀, 스핀 조건 판정, 전투 효과 변환은 `SlotRogue.Relics.Pool.RelicCatalog`와 `RelicDefinition`을 단일 런타임 모델로 사용한다.

Phase 1에서 기획과 동일하게 동작하는 효과만 `RelicDefinition.Phase1`로 보상 풀과 실행 경로에 노출한다. 이름이 유사한 기존 전투 상태를 임의로 매핑하지 않으며, 전투 코어 계약이 v23과 다르면 해당 유물은 `All`에만 등록하고 보상 풀에서 제외한다. 구 `ArtifactDefinitionSO`와 `RelicDataSO` 계층은 신규 런타임 코드에서 참조하지 않는다.

## Alternatives considered

- **`ArtifactDefinitionSO`를 시작 유물 전용으로 병행 유지** — 시작 선택과 전투 효과의 식별자·수치가 다시 갈라지고, 신규 S-01~S-06과 구 화염·빙결·독 시작 유물이 충돌하므로 거절한다.
- **`RelicDataSO`로 v23 80종을 다시 생성** — 인스펙터 편집은 가능하지만 현재 한 달 스프린트에서 80개 자산 동기화와 마이그레이션 비용이 크고, 이미 코드 카탈로그와 테스트가 있으므로 거절한다.
- **당시 기존 화상/독을 v23 화상/감염으로 간주** — 발동 타이밍과 스택 감소 규칙이 달라 실제 플레이가 기획과 달라지므로 거절한다.

## Consequences

- 시작 유물과 보상 유물이 동일한 `OwnedRelics` 인벤토리와 `RelicEffectRunner`를 통과한다.
- v23 HTML의 시작 유물 수치가 실제 전투에 반영되고, 구 속성 시작 유물은 더 이상 실행되지 않는다.
- 2026-06-12 정적 참조 검증 후 구 Artifact/RelicDataSO 타입, Editor builder, `_Legacy` 자산을 삭제했다.
- Rare/Legendary/Curse와 전투 코어 계약이 필요한 상태이상 효과는 카탈로그 조회는 가능하지만 보상 풀과 실행 경로에서는 제외된다.
- 밸런스 데이터 편집이 잦아져 ScriptableObject나 외부 데이터로 이전할 때는 이 ADR을 supersede하고 단일 출처·검증 방식을 다시 결정해야 한다.

## Notes

- 기획 원본 포인터는 [`references/INDEX.md`](../../references/INDEX.md)에 기록한다.
- 런타임 흐름은 [`relic-system.md`](../design-docs/relic-system.md)에 설명한다.
