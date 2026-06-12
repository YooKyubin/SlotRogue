# ADR-0006: 런타임 자산은 Resources가 아닌 조립 계층에서 공급한다

**Status**: accepted
**Date**: 2026-06-11
**Supersedes**: none
**Superseded by**: none
**Related design-docs**: [`game-flow.md`](../design-docs/game-flow.md), [`slot-core.md`](../design-docs/slot-core.md)

---

## Context

슬롯 패턴 카탈로그와 전투 UI Sprite가 `Resources.Load` 또는 `Resources.LoadAll` fallback에 의존했다. 이 방식은 호출부가 자산 경로와 로드 방식을 직접 알고, `Resources` 폴더의 자산이 Player 빌드에 항상 포함되므로 추후 AssetBundle로 콘텐츠를 분리할 때 중복 포함과 교체 비용이 발생한다.

이 결정 당시에는 Unity AssetBundle 모듈만 있고 Addressables 패키지는 설치되어 있지 않았다. 이후 로컬 런타임 자산 공급 기준선은 [ADR-0007](./0007-addressables-local-runtime-assets.md)에서 결정했다. 이 ADR은 특정 로더와 무관한 런타임 코드 경계를 계속 정의한다.

## Decision

런타임 게임 코드와 View에서는 `Resources.Load*`를 호출하지 않는다.

- Prefab이 소유하는 Sprite와 AudioClip은 `[SerializeField] private` 참조로 보관한다.
- Composition Root가 소유하는 ScriptableObject는 직렬화 참조로 받아 Core의 명시적 설정 지점에 주입한다.
- `Bind`의 선택 인자가 null 또는 빈 배열이면 기존 직렬화 참조를 보존한다.
- 테스트와 개발용 순수 로직은 메모리 기본값을 사용할 수 있지만, 이 기본값은 런타임 자산 경로를 탐색하지 않는다.
- AssetBundle 도입 시에는 Bundle에서 Composition Root 또는 설정 자산을 먼저 로드한 뒤 같은 직렬화/주입 경계를 사용한다.

AssetBundle 이름, 의존성 분할, 로컬·원격 배포 방식과 Addressables 도입 여부는 별도 ADR에서 결정한다.

## Alternatives considered

- **Resources fallback 유지**: 구현은 간단하지만 Player 상시 포함, 문자열 경로 결합, Bundle 중복 가능성을 남기므로 제외했다.
- **각 View에서 AssetBundle 직접 로드**: 화면 코드가 Bundle 이름, 수명, 해제 정책을 소유하게 되어 책임이 분산되므로 제외했다.
- **Addressables 즉시 도입**: 이 결정 시점에는 패키지와 그룹 정책이 없어 보류했으며, 후속 [ADR-0007](./0007-addressables-local-runtime-assets.md)에서 로컬 기준선을 채택했다.

## Consequences

- 자산 누락은 암묵적 경로 탐색 대신 Prefab 또는 Composition Root 설정 오류로 드러난다.
- Prefab과 ScriptableObject를 AssetBundle에 넣으면 Unity 의존성으로 직렬화 참조가 함께 추적된다.
- 런타임 동적 콘텐츠가 필요해지면 로드·캐시·해제 수명을 관리하는 상위 서비스가 추가되어야 한다.
- `Resources` 폴더에 남은 기존 자산은 사용처를 확인한 뒤 단계적으로 일반 자산 폴더로 이동해야 한다.
