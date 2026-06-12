# ADR-0007: Addressables 로컬 기준선으로 런타임 설정 자산을 공급한다

**Status**: accepted
**Date**: 2026-06-11
**Supersedes**: none
**Superseded by**: none
**Related design-docs**: [`game-flow.md`](../design-docs/game-flow.md), [`slot-core.md`](../design-docs/slot-core.md)

---

## Context

ADR-0006에서 런타임 코드의 `Resources.Load*` 의존을 제거했지만, Addressables 패키지와 설정 파일만 생성된 뒤 실제 Addressable 엔트리와 런타임 로드 경로는 연결되지 않았다. `SlotPatternCatalog.asset`은 `Resources` 밖으로 이동했으므로 씬 직렬화 참조 또는 별도 로더가 없으면 Player에서 공급되지 않는다.

한 달 스프린트에서는 원격 호스팅과 콘텐츠 업데이트까지 한 번에 확정하기보다, 로컬 번들로 동작하는 최소 기준선을 먼저 만들 필요가 있다. Prefab이 직접 참조하는 Sprite까지 개별 Addressable로 중복 등록하면 Player 내장 데이터와 번들에 함께 포함될 수 있으므로 적용 단위도 제한해야 한다.

## Decision

Addressables 2.9.1을 런타임 설정 자산의 로컬 공급 기준선으로 사용한다.

- 첫 Addressable 엔트리는 `SlotPatternCatalog.asset`이며 키는 `SlotPatternCatalog.Address` 상수의 `slot/catalog/patterns`를 사용한다.
- `BattleSceneCompositionRoot`가 카탈로그를 비동기로 로드하고 Core의 런타임 override 지점에 주입한다.
- 로드 핸들은 Composition Root가 소유하고 파괴 시 해제한다. 로드 실패 시 메모리 기본 카탈로그를 사용하며 다음 진입에서 재시도할 수 있다.
- Editor Play Mode는 `Use Asset Database (Fast Mode)`를 사용한다.
- Player 빌드는 `Build Addressables on Player Build`를 활성화해 로컬 콘텐츠를 함께 생성한다.
- 현재 그룹은 `Default Local Group` 하나를 사용한다. 원격 호스팅, 콘텐츠 업데이트, 그룹 세분화는 실제 배포 요구가 생길 때 후속 ADR로 결정한다.
- Prefab 직렬화 참조로 소유되는 Sprite와 AudioClip은 소유 Prefab 또는 자산 묶음의 이전 전략이 정해지기 전까지 개별 Addressable로 등록하지 않는다.

## Alternatives considered

- **패키지와 설정만 유지** — 엔트리와 런타임 호출이 없어 실제 도입이 아니므로 제외했다.
- **모든 UI 자산을 즉시 Addressable로 등록** — 기존 Prefab 직접 참조와 번들 의존성이 겹쳐 중복 포함과 수명 관리 범위가 커지므로 제외했다.
- **저수준 AssetBundle API 직접 사용** — 키 해석, 의존성, 비동기 핸들, 빌드 파이프라인을 프로젝트가 직접 관리해야 하므로 현재 일정에 맞지 않아 제외했다.

## Consequences

- `SlotPatternCatalog`은 씬 직렬화 상태와 무관하게 Addressables 키로 공급된다.
- Editor에서는 별도 콘텐츠 빌드 없이 Play 검증할 수 있고, Player 빌드에서는 Addressables 콘텐츠가 함께 생성된다.
- Addressables를 사용하는 asmdef는 `Unity.Addressables`와 `Unity.ResourceManager`를 명시적으로 참조해야 한다.
- 원격 카탈로그와 CDN 배포를 도입할 때 프로필, 그룹, 캐시, 업데이트 제한 정책을 추가로 결정해야 한다.
- Addressable 자산을 직접 참조하는 비 Addressable Prefab이 늘어나면 중복 번들 분석이 필요하다.
