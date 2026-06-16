# 광고 제거 IAP와 보상형 광고 스킵

**Status**: active
**Started**: 2026-06-14
**Owner**: Codex
**Related design-docs**: [광고 제거 IAP](../../design-docs/remove-ads-iap.md)

## Goal

`remove_ads` Non-Consumable 구매 상태를 로컬에 캐시하고, 구매자는 기존 보상형 광고의 횟수 제한과 보상을 유지한 채 광고 시청만 건너뛴다.

## Checklist

- [x] 기존 LevelPlay 보상형 광고와 보상/부활 흐름 조사 — Codex
- [x] ADR-0015와 광고 제거 IAP 설계 문서 작성 — Codex
- [x] `AdsRemoveState`와 PlayerPrefs 로컬 캐시 구현 — Codex
- [x] Codeless IAP 및 Store 복원용 fulfillment 경계 구현 — Codex
- [x] 부활/리롤/추가 보상/보상 2배 광고 스킵과 문구 연결 — Codex
- [x] EditMode 테스트 추가와 UI/테스트 어셈블리 컴파일 검증 — Codex
- [x] Unity Product Catalog에 `remove_ads` Non-Consumable 등록 — Codex
- [x] GameStart 광고 제거 구매 버튼 씬 직렬화와 Codeless IAP Inspector 이벤트 연결 — Codex
- [x] 구매 pending 처리 중 Codeless 버튼 목록 변경 예외 방지 — Codex
- [x] Codeless Store 연결/해제 callback 사전 등록 — Codex
- [ ] Unity Test Runner에서 신규 EditMode 테스트 실행 — 팀
- [ ] Google Play 테스트 트랙 구매/재설치 복원 검증 — 팀

## Notes

- Google Play Store와 Android 실기기 검증이 남아 있으므로 검증 완료 전까지 plan을 active로 유지한다.
- 열린 Unity Editor가 새 파일을 아직 import하지 않은 상태라 Test Runner 실행은 보류했다. 임시 검증 프로젝트로 새 파일을 포함한 UI/테스트 어셈블리 빌드는 경고 0, 오류 0으로 확인했다.

## Completion

- **Finished**:
- **Outcome**:
- **Follow-ups**:
