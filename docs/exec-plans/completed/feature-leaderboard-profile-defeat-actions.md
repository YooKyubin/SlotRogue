# 리더보드 프로필과 패배 후 선택

**Status**: completed
**Started**: 2026-06-12
**Owner**: _(슬롯 담당)_
**Related design-docs**: [`leaderboard.md`](../../design-docs/leaderboard.md)

## Goal

GameStart에서 닉네임·국가 프로필을 최초 1회 필수로 등록하고, 모든 패배 기록을 자동 제출하며, 패배 화면에서 재시작·랭킹·홈 이동을 선택할 수 있게 한다.

## Checklist

- [x] ADR-0011과 leaderboard design-doc 갱신
- [x] 닉네임·국가 프로필 저장과 필수 LOGIN UI
- [x] 저장 프로필 국가를 사용하는 패배 기록 자동 제출
- [x] 패배 화면 RESTART·RANKING·HOME 3버튼
- [x] EditMode 테스트와 Unity 컴파일 검증

## Notes

- 현재 튜토리얼이 없으므로 GameStart 진입 직후 프로필을 요구한다. 튜토리얼 도입 시 호출 시점만 완료 이벤트 뒤로 이동한다.
- `ProjectSettings/ProjectSettings.asset`의 `cloudProjectId`가 비어 있어 실제 UGS 네트워크 검증은 프로젝트 연결 뒤 진행한다.
- 2026-06-13: [ADR-0012](../../adr/0012-leaderboard-nickname-only-profile.md)에서 필수 프로필을 닉네임만으로 축소했다.

## Completion

- **Finished**: 2026-06-12
- **Outcome**: GameStart 최초 LOGIN에서 닉네임·국가를 필수 등록하고, 패배 기록을 자동 제출하며, 패배 화면에 RESTART·RANKING·HOME 선택을 추가했다. Unity 컴파일과 관련 EditMode 테스트 12건이 통과했다.
- **Follow-ups**: Unity Services Cloud Project 연결 뒤 실제 Player Name 저장, score 자동 제출, 패배 직후 Top 10 갱신을 플레이테스트한다. 튜토리얼 도입 시 LOGIN 요구 호출을 튜토리얼 완료 뒤로 이동한다.
