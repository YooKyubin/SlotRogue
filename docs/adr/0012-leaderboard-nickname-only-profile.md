# ADR-0012: 리더보드 프로필은 닉네임만 저장한다

**Status**: accepted
**Date**: 2026-06-13
**Supersedes**: [ADR-0011](./0011-leaderboard-profile-and-defeat-flow.md)
**Superseded by**: none
**Related design-docs**: [`leaderboard.md`](../design-docs/leaderboard.md), [`game-flow.md`](../design-docs/game-flow.md)

---

## Context

리더보드에서 국가는 순위 계산이나 런 결과 해석에 사용되지 않는다. 국가 선택을 필수 프로필 입력으로 유지하면 최초 진입 단계와 `PlayerPrefs`, metadata, 조회 UI에 별도 계약이 필요하지만 현재 게임 경험에 주는 가치가 작다.

기존 구현은 닉네임과 국가를 필수로 저장하고 metadata schema version 1에 `CountryCode`를 포함한다. 이미 생성된 로컬 프로필과 리더보드 엔트리를 깨뜨리지 않으면서 새 기록부터 국가 데이터를 제거해야 한다.

## Decision

- 정상 게임 시작 전에 필요한 리더보드 프로필 값은 닉네임 하나다.
- `LOGIN` UI와 리더보드 프로필 편집 UI에서 국가 선택을 제거한다.
- 로컬 프로필은 버전 2로 올리고 닉네임만 `PlayerPrefs`에 저장한다.
- 기존 버전 1 프로필은 닉네임이 유효하면 유지하고, 마이그레이션 시 기존 국가 코드 키를 삭제한다.
- 리더보드 metadata schema version 2는 `SchemaVersion`, `Wave`, `RelicIds`만 저장한다.
- 기존 schema version 1 metadata의 `CountryCode`는 역직렬화 시 무시하고 나머지 필드는 계속 표시한다.
- 리더보드 목록에는 국가 코드를 표시하지 않는다.
- 패배 기록 자동 제출과 `RESTART`, `RANKING`, `HOME` 흐름은 ADR-0011의 결정을 유지한다.

## Alternatives considered

- **국가를 선택 입력으로 유지** — 필수 입력 문제는 줄지만 저장·metadata·UI 계약을 계속 유지해야 해 제외했다.
- **기기 지역에서 국가를 자동 추정** — 사용자에게 필요하지 않은 데이터를 계속 수집하며 실제 국가와 다를 수 있어 제외했다.
- **기존 metadata schema version 1을 그대로 사용하고 빈 국가를 저장** — 제거된 필드를 계약에 남기므로 제외했다.

## Consequences

- 최초 프로필 등록은 닉네임 입력만 요구한다.
- 새 리더보드 기록에는 국가 정보가 저장되지 않는다.
- 기존 엔트리의 국가 정보는 서비스에 남아 있을 수 있지만 클라이언트는 읽거나 표시하지 않는다.
- 기존 로컬 사용자는 닉네임을 다시 입력하지 않아도 되며, 저장돼 있던 국가 코드는 다음 프로필 로드 시 삭제된다.
