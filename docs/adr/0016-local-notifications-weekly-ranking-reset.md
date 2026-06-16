# ADR-0016: 알림은 로컬 예약하고 주간 랭킹은 한국 수요일에 리셋한다

**Status**: accepted
**Date**: 2026-06-14

## Context

필요한 알림은 마지막 플레이 이후 24시간 경과와 주간 랭킹 마감처럼 기기에서 미리 계산할 수 있는 시간 조건이다. 별도 Push 서버를 도입하면 토큰, 발송 대상, 개인정보 및 운영 인프라가 추가된다.

현재 랭킹은 UGS Leaderboards의 단일 `Keep Best` 보드다. 클라이언트가 점수를 삭제하거나 주차를 임의 필터링하면 전체 플레이어가 같은 시점에 리셋되지 않으며 기기 시각 조작에도 취약하다.

## Decision

1. 알림은 Unity Mobile Notifications의 Android/iOS 로컬 알림으로 예약한다.
2. 앱이 백그라운드로 전환될 때 기존 복귀 알림을 취소하고 24시간 뒤로 다시 예약한다.
3. 주간 랭킹은 매주 수요일 00:00 KST에 리셋한다.
4. 랭킹 마감 알림은 기본적으로 리셋 3시간 전인 화요일 21:00 KST에 예약하며 lead time은 Inspector에서 조정할 수 있다.
5. 실제 랭킹 리셋은 UGS Dashboard의 Reset schedule로 설정한다. 클라이언트는 리셋 시각 계산과 알림만 담당한다.
6. Android 알림 채널과 권한 요청, iOS 알림 권한 요청은 앱의 영속 알림 컴포넌트가 담당한다.

## Consequences

- 시간 기반 알림에 별도 Push 서버가 필요하지 않다.
- 사용자가 알림 권한을 거부하면 게임은 정상 동작하지만 알림은 표시되지 않는다.
- KST에는 DST가 없으므로 주간 경계가 고정된다.
- UGS Dashboard의 Reset schedule이 누락되면 알림만 동작하고 실제 랭킹은 초기화되지 않는다.
- 운영자가 원격으로 문구나 발송 대상을 바꾸는 기능은 후속 원격 Push 시스템 범위다.

## References

- [리더보드 설계](../design-docs/leaderboard.md)
- [알림과 주간 랭킹 설계](../design-docs/notifications-weekly-ranking.md)
