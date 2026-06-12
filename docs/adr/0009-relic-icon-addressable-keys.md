# ADR-0009: 유물 아이콘은 논리 키로 Addressable Sprite를 참조한다

**Status**: accepted
**Date**: 2026-06-12
**Supersedes**: none
**Superseded by**: none
**Related design-docs**: [`relic-system.md`](../design-docs/relic-system.md), [`game-flow.md`](../design-docs/game-flow.md)

---

## Context

유물은 v23 기준 80종이지만 현재 아트는 16개 Sprite가 들어 있는 시트만 준비되어 있다. 유물 수와 아이콘 수가 계속 늘어날 예정이므로 View나 Prefab에 유물 ID별 Sprite 참조를 직접 늘리면 화면마다 매핑이 중복되고, Addressables 또는 AssetBundle로 이전할 때 로드 책임이 분산된다.

현재 시작 유물과 보상 View에는 `GameFlowImageSlot`이 있지만 ViewState에는 아이콘 식별자가 없고, 모든 카드가 같은 직렬화 Sprite를 표시한다. UI strict MVVM 경계와 런타임 자산 로드 경계를 유지하면서 데이터에서 아이콘을 선택할 방법이 필요하다.

## Decision

유물 아이콘은 `RelicDefinition.IconKey`의 논리 키로 식별하고 조립 계층이 Addressables에서 로드한다.

- 현재 16개 아이콘 시트는 `relic/icons/base` 주소 하나로 등록한다.
- 시트 내부 Sprite는 Addressables subasset 문법인 `relic/icons/base[SpriteName]`으로 참조한다.
- 키 문자열은 `RelicIconKeys`에 모은다. `RelicCatalog.R()`은 역할별 기본 키를 넣고, 개별 유물은 `iconKey:` 인자로 덮어쓸 수 있다.
- ViewModel은 `IconKey` 문자열만 ViewState에 전달한다. View는 Addressables를 직접 호출하지 않는다.
- `RunGameSceneRoot`가 `AddressableSpriteProvider`의 생성·캐시·해제를 소유하고 로드된 Sprite를 `GameFlowImageSlot`에 전달한다.
- 새 아이콘이 개별 Sprite이면 고유 Addressable 주소를, 새 시트이면 시트 주소와 subasset 이름을 키 상수로 추가한다.

## Alternatives considered

- **유물마다 Prefab에 Sprite 직접 연결** — 화면별 직렬화 매핑이 중복되고 콘텐츠 증가 시 Prefab 수정 범위가 커져 제외했다.
- **View에서 Addressables 직접 호출** — View가 키 해석과 핸들 수명을 소유해 ADR-0006과 ADR-0008 경계를 위반하므로 제외했다.
- **80종 아이콘이 완성될 때까지 단일 기본 Sprite 유지** — 현재 아트 16종을 활용할 수 없고 이후 데이터 계약을 다시 바꿔야 하므로 제외했다.

## Consequences

- 현재는 역할별로 4개 기본 아이콘을 공유하며, 나머지 시트 아이콘은 `iconKey:` 설정으로 즉시 선택할 수 있다.
- 아이콘을 추가해도 View·ViewModel·Prefab 구조는 바뀌지 않는다.
- Addressables 키가 누락되면 공용 기본 아이콘으로 대체되고 경고가 한 번 기록된다.
- 현재 Prefab이 직접 참조하는 강조 시트는 Editor에서 참조 제거와 시각 검증을 마치기 전까지 직렬화 fallback으로 남는다. 이 시트는 코드에서 `Resources.Load*`로 읽지 않는다.
- Unity Editor에서 `Default Local Group` 엔트리와 Fast Mode 로드를 확인하고, Player Addressables 콘텐츠를 다시 빌드해야 한다.
