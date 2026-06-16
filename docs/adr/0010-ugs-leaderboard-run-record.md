# ADR-0010: 런 최고기록은 UGS Leaderboards 점수와 metadata로 저장한다

**Status**: superseded
**Date**: 2026-06-12
**Supersedes**: none
**Superseded by**: [ADR-0011](./0011-leaderboard-profile-and-defeat-flow.md)
**Related design-docs**: [`leaderboard.md`](../design-docs/leaderboard.md), [`game-flow.md`](../design-docs/game-flow.md)

---

## Context

`Slot_Rogue_Leaderboard`에서 플레이어별 최고기록과 함께 이름, 국가, 도달 wave, 보유 유물을 조회해야 한다. UGS Leaderboards의 score는 숫자 하나만 직접 정렬할 수 있고, 나머지 런 정보는 score metadata 또는 별도 저장소에 보관해야 한다.

플레이어 이름은 UGS Authentication Player Name이 이미 리더보드 엔트리에 포함된다. 국가와 유물은 정렬 대상이 아니며, 유물 이름은 밸런스·현지화 과정에서 바뀔 수 있으므로 안정적인 ID를 저장해야 한다.

## Decision

- 리더보드 ID는 `Slot_Rogue_Leaderboard` 상수로 관리한다.
- score는 패배 시점의 클리어 wave 수인 `GameFlowSession.Victories`다.
- metadata schema version 1은 `SchemaVersion`, `CountryCode`, `Wave`, `RelicIds`를 저장한다.
- `Wave`는 패배한 전투를 포함한 도달 wave인 `CurrentBattleNumber`다.
- 이름은 metadata에 중복 저장하지 않고 UGS Authentication Player Name을 사용한다.
- 국가는 기기의 현재 지역 설정에서 ISO 3166-1 alpha-2 코드를 추정한다. 지역을 확인할 수 없으면 `ZZ`를 저장한다.
- 유물은 `RelicDefinition.Id` 배열로 저장한다. 표시 시 ID를 사용하며 이름·아이콘 확장은 로컬 카탈로그에서 해석한다.
- Boot에서 익명 인증을 준비하되 실패가 게임 시작을 막지 않는다. 조회·제출 시 다시 초기화를 시도한다.
- Dashboard 설정은 내림차순 정렬과 최고 점수 유지 방식이어야 한다.

## Alternatives considered

- **표시 정보를 score 문자열에 인코딩** — 숫자 정렬과 범위 조회를 깨뜨리므로 제외했다.
- **국가·wave·유물을 별도 Cloud Save에 저장** — 다른 플레이어 데이터를 조회하기 위한 접근 제어와 추가 왕복이 필요해 현재 범위에서는 제외했다.
- **유물 이름을 metadata에 저장** — 현지화와 이름 변경 뒤 오래된 엔트리가 stale 상태가 되므로 제외했다.
- **네트워크 초기화 실패 시 Boot 중단** — 오프라인에서도 본편 플레이는 가능해야 하므로 제외했다.

## Consequences

- 리더보드 엔트리 하나로 순위와 런 요약을 조회할 수 있다.
- 국가 코드는 실제 위치가 아니라 기기 지역 설정의 추정치다.
- 익명 계정을 플랫폼 계정에 연결하지 않으면 앱 데이터 삭제나 기기 변경 시 플레이어 ID가 바뀔 수 있다.
- 오프라인 제출 큐는 포함하지 않는다. 네트워크 실패 기록의 재제출은 세이브 시스템 도입 후 결정한다.
- `ProjectSettings/ProjectSettings.asset`의 Cloud Project ID와 Dashboard의 리더보드 설정이 일치해야 실제 요청이 성공한다.
