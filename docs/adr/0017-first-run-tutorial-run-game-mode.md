# ADR-0017: 최초 튜토리얼은 RunGame 튜토리얼 모드로 처리한다

**Status**: accepted
**Date**: 2026-06-19
**Supersedes**: none
**Superseded by**: none
**Related design-docs**: `docs/design-docs/first-run-tutorial.md`, `docs/design-docs/game-flow.md`

---

## Context

SlotRogue의 본편 루프는 `GameStart`에서 새 런을 시작한 뒤 `RunGame` 내부 상태(`StartRelicSelect`, `Battle`, `Reward`, `Defeat`)를 전환하는 구조다. 튜토리얼은 최초 1회만 등장해야 하며, 플레이어에게 시작 버튼, 슬롯 스핀, 족보 결과, 몬스터 턴 확인 포인트를 실제 플레이 흐름 안에서 보여줘야 한다.

별도 Unity Scene을 만들면 화면 복제와 직렬화 참조 관리가 늘어나고, 아직 `RunGame` 전투 UI와 슬롯 연출이 빠르게 바뀌는 상태에서 튜토리얼만 오래된 UI 계약을 가질 위험이 있다. 반대로 기존 `RunGame`을 재사용하면 실제 전투/슬롯/연출 경로를 그대로 검증할 수 있지만, 일반 런과 튜토리얼 런의 분기점이 명시적으로 필요하다.

## Decision

최초 튜토리얼은 별도 전용 Scene이 아니라 기존 `RunGame` Scene을 `TutorialMode`로 실행한다. `GameStart`의 시작 버튼은 `TutorialCompleted` 로컬 플래그를 확인해 최초 1회만 튜토리얼 런을 시작하고, 튜토리얼 런은 시작 유물 선택을 건너뛴 뒤 전투 화면으로 바로 진입한다.

## Alternatives considered

- **튜토리얼 전용 Unity Scene 복제** — 배치가 명확하지만 `RunGame` UI, 슬롯 연출, 몬스터 HUD, Addressables 참조를 중복 관리해야 한다. 본편 UI가 바뀔 때 튜토리얼 Scene이 쉽게 stale해지므로 거절한다.
- **GameStart 안에서 정적 설명만 표시** — 구현은 가볍지만 슬롯 스핀과 몬스터 턴을 실제 조작으로 학습시키지 못한다. 핵심 학습 목표가 전투 화면에 있으므로 거절한다.

## Consequences

- `GameFlowSession`은 일반 런과 튜토리얼 런을 구분하는 플래그를 가진다.
- 튜토리얼 런은 시작 유물 없이 시작하므로 `RunGameSceneRoot`의 최초 상태 선택이 런 모드에 따라 달라진다.
- 첫 스핀 결과는 튜토리얼용 고정 `SlotSpinResult`로 주입하고, 이후 턴은 일반 슬롯 시스템을 그대로 사용한다.
- 튜토리얼 설명 UI는 전투 화면 위에 비차단 오버레이로 표시한다. 실제 버튼/전투 View는 그대로 사용한다.
- 저장 플래그는 현재 로컬 프로필/구매 캐시와 같은 방식으로 `PlayerPrefs`에 둔다. 추후 세이브 시스템을 도입하면 마이그레이션 대상이 된다.

## Notes

첫 구현 범위는 최초 1회 자동 진입, 시작 유물 스킵, 첫 스핀 확정, 전투 중 안내 오버레이, 완료 플래그 저장까지로 제한한다. 튜토리얼 다시 보기 버튼은 후속 UI 작업으로 둔다.
