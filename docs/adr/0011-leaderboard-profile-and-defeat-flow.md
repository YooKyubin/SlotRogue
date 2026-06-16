# ADR-0011: 리더보드 프로필은 필수이며 패배 기록은 자동 제출한다

**Status**: superseded
**Date**: 2026-06-12
**Supersedes**: [ADR-0010](./0010-ugs-leaderboard-run-record.md)
**Superseded by**: [ADR-0012](./0012-leaderboard-nickname-only-profile.md)
**Related design-docs**: [`leaderboard.md`](../design-docs/leaderboard.md), [`game-flow.md`](../design-docs/game-flow.md)

---

## Context

리더보드는 플레이어가 매 런마다 참가 여부를 선택하는 기능이 아니라 모든 정상 런의 최고기록을 비교하는 기본 게임 기능이다. 따라서 기록 제출 전에 플레이어가 직접 닉네임과 국가를 지정해야 하며, 패배 직후에도 강제 재시작 대신 기록 확인이나 홈 이동을 선택할 수 있어야 한다.

현재 튜토리얼은 없지만 이후 추가될 수 있으므로 최초 프로필 요구 시점은 "튜토리얼 완료 뒤 GameStart"로 정의한다. 현재 버전에서는 GameStart 진입 직후가 그 시점이다.

## Decision

- 정상 게임 시작 전에 닉네임과 ISO 3166-1 alpha-2 국가 코드를 직접 지정한 리더보드 프로필이 반드시 존재해야 한다.
- 최초 프로필이 없으면 GameStart에서 닫을 수 없는 `LOGIN` 팝업을 자동 표시한다. 프로필 저장 전 Start 요청도 같은 팝업을 표시하고 게임을 시작하지 않는다.
- `LOGIN`은 이메일 계정 로그인이 아니라 UGS 익명 인증 계정의 Player Name과 리더보드 공개 프로필을 등록하는 절차다.
- 닉네임은 UGS Authentication Player Name에 저장하고, 국가 코드는 로컬 프로필에 저장해 런 metadata에 사용한다.
- 명시적으로 입력한 프로필 여부와 국가 코드는 현재 단계에서 `PlayerPrefs`에 저장한다. 일반 게임 세이브 포맷이 결정되면 해당 저장 계층으로 이전한다.
- 패배 기록은 참가 확인 없이 항상 `Slot_Rogue_Leaderboard`에 제출한다. Dashboard는 내림차순과 Keep Best를 사용한다.
- 패배 화면은 즉시 재시작하지 않고 `RESTART`, `RANKING`, `HOME` 버튼을 제공한다.
- `RANKING`은 RunGame 위에 리더보드 패널을 열고, `HOME`은 GameStart로 이동하며, `RESTART`만 새 런을 시작한다.
- score와 metadata 계약은 ADR-0010의 클리어 wave, 도달 wave, 유물 ID 구조를 유지하되 국가는 기기 추정값이 아니라 저장된 사용자 선택값을 사용한다.

## Alternatives considered

- **기기 지역에서 국가 자동 추정** — 실제 사용자 선택과 다를 수 있고 프로필 정체성이 불명확하므로 제외했다.
- **패배 때마다 리더보드 참가 여부 확인** — 리더보드 기록 누락과 선택 편향이 생기므로 제외했다.
- **패배 즉시 새 런 시작** — 결과 확인과 홈 이동을 막으므로 제외했다.
- **이메일·플랫폼 계정 로그인을 즉시 도입** — 현재 스프린트 범위와 SDK 구성이 커지므로 익명 인증 프로필 등록으로 제한했다.

## Consequences

- 정상 GameStart 경로에서는 프로필 저장 전 런을 시작할 수 없다.
- 최초 프로필 저장에는 UGS Authentication 연결이 필요하다.
- 국가는 사용자가 선택한 공개 정보이며 실제 위치를 검증하지 않는다.
- `PlayerPrefs`는 민감정보를 저장하지 않으며 닉네임과 국가 코드만 캐시한다.
- RunGame 씬을 단독 실행하는 개발 경로에서는 미완성 프로필이 있을 수 있으므로 제출 metadata는 `ZZ` fallback을 유지한다.
- 튜토리얼 도입 시 GameStart의 프로필 요구 호출을 튜토리얼 완료 이벤트 뒤로 이동해야 한다.
