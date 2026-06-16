# UGS 런 리더보드

**Status**: completed
**Started**: 2026-06-12
**Owner**: _(슬롯 담당)_
**Related design-docs**: [`leaderboard.md`](../../design-docs/leaderboard.md)

## Goal

`Slot_Rogue_Leaderboard`에 런 최고기록과 국가·wave·유물 metadata를 제출하고, GameStart에서 이름 변경과 Top 10 조회가 가능하게 한다.

## Checklist

- [x] ADR-0010과 leaderboard design-doc 작성
- [x] UGS 초기화·익명 인증·Player Name 연동
- [x] 패배 시 score와 metadata 자동 제출
- [x] GameStart Top 10 조회·이름 변경 UI
- [x] metadata 변환 EditMode 테스트
- [x] 컴파일 검증과 Cloud Project 연결 상태 기록

## Notes

- `ProjectSettings/ProjectSettings.asset`의 `cloudProjectId`가 현재 비어 있어 실제 네트워크 검증 전 Unity Services 프로젝트 연결이 필요하다.
- 2026-06-13: [ADR-0012](../../adr/0012-leaderboard-nickname-only-profile.md)에서 국가 metadata를 제거했다.

## Completion

- **Finished**: 2026-06-12
- **Outcome**: 익명 인증, 최고기록 제출, metadata 변환, Player Name 변경, GameStart Top 10 런타임 UI를 추가했다. Unity 컴파일과 leaderboard EditMode 테스트 6건이 통과했다.
- **Follow-ups**: Unity Services에서 Dashboard 프로젝트를 연결한 뒤 실데이터 제출·조회 플레이테스트가 필요하다. 오프라인 재제출과 플랫폼 계정 연결은 세이브/계정 작업에서 다룬다.
