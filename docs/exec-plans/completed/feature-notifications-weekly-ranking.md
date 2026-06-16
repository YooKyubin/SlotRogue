# 로컬 알림과 주간 랭킹

**Status**: completed
**Started**: 2026-06-14
**Owner**: Codex
**Related design-docs**: [로컬 알림과 주간 랭킹](../../design-docs/notifications-weekly-ranking.md)

## Goal

마지막 플레이 24시간 뒤 복귀 알림과 한국 수요일 주간 랭킹 마감 알림을 예약하고, UGS 리셋 운영 설정과 클라이언트 시간 계산을 분리한다.

## Checklist

- [x] 기존 리더보드와 앱 생명주기 구조 조사 — Codex
- [x] ADR-0016과 설계 문서 작성 — Codex
- [x] Unity Mobile Notifications 패키지 및 플랫폼 예약 구현 — Codex
- [x] KST 주간 리셋/마감 시간 계산과 테스트 추가 — Codex
- [x] BootScene 영속 컴포넌트와 Inspector 기본값 연결 — Codex
- [x] UI/테스트 어셈블리 컴파일 검증 — Codex

## Notes

- 서버 Push가 아니라 기기 로컬 알림을 사용한다.
- UGS Dashboard Reset schedule은 화요일 15:00 UTC가 수요일 00:00 KST다.

## Completion

- **Finished**: 2026-06-14
- **Outcome**: 앱 이탈 24시간 뒤 복귀 알림과 매주 화요일 21:00 KST 랭킹 마감 알림을 로컬 예약하고, 수요일 00:00 KST 리셋 기준을 공통 시간 계산으로 분리했다.
- **Follow-ups**: 팀에서 UGS Dashboard Reset schedule을 화요일 15:00 UTC로 설정하고 Android/iOS 실기기에서 알림 권한과 예약 수신을 확인한다.
