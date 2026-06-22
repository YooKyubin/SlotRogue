# 최초 튜토리얼

**Status**: completed
**Started**: 2026-06-19
**Owner**: Codex
**Contributors**: -
**Related design-docs**: [`../../design-docs/first-run-tutorial.md`](../../design-docs/first-run-tutorial.md)

## Goal

최초 플레이어가 `GameStart`의 시작 버튼을 누르면 시작 유물 없이 `RunGame` 전투로 들어가고, 첫 슬롯 결과가 확정 족보로 나오며, 전투 중 안내 오버레이가 몬스터 의도, 슬롯 결과, 실제 피격, 다음 턴 확인 포인트를 설명한다. 전투가 끝나면 완료 플래그를 저장해 이후 시작은 일반 런으로 진입한다.

## Checklist

- [x] 튜토리얼 모드 결정과 흐름 문서화 — Codex
- [x] 최초 1회 완료 플래그와 런 모드 분기 추가 — Codex
- [x] 시작 유물 스킵과 튜토리얼 전투 진입 연결 — Codex
- [x] 첫 스핀 확정 결과와 전투 단계 신호 연결 — Codex
- [x] 비차단 안내 오버레이 추가 — Codex
- [x] EditMode 수준 검증 추가 및 컴파일 확인 — Codex

## Notes

- Unity MCP 없이 코드와 문서만 수정한다.
- 기존 씬에 연결된 몬스터 정의를 튜토리얼에서도 재사용하고, 튜토리얼 런에서는 HP만 낮춰 첫 확정 족보 뒤 몬스터 턴을 볼 수 있게 한다.
- 전투 시작 안내는 몬스터 의도 아이콘을 먼저 보게 하고, 첫 확정 족보 피해는 튜토리얼 몬스터를 즉시 처치하지 않도록 유지한다. 몬스터가 플레이어에게 피해를 주면 별도 안내를 표시한다.
- `SlotRogue.Slot.Tests.csproj`와 `SlotRogue.UI.Tests.csproj` 빌드는 통과했다.

## Completion

- **Finished**: 2026-06-19
- **Outcome**: 최초 1회 `RunGame` 튜토리얼 모드, 시작 유물 스킵, 확정 첫 스핀, 몬스터 의도/피격/다음 턴 안내 오버레이, 완료 플래그 저장을 연결했다.
- **Follow-ups**: 설정/옵션에서 튜토리얼 다시 보기 진입점을 추가할지 결정한다.
