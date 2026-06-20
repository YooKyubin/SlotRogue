# 튜토리얼 전투 흐름 확장

**Status**: completed  
**Started**: 2026-06-19  
**Finished**: 2026-06-19  
**Owner**: Codex  
**Contributors**: -  
**Related design-docs**: [`../../design-docs/first-run-tutorial.md`](../../design-docs/first-run-tutorial.md)

## Goal

최초 튜토리얼을 단순 첫 스핀 안내에서 2마리 몬스터, 인벤토리 확인, 튜토리얼 유물 반응, 고정 1·2번째 스핀, 몬스터 방어 후 다음 의도 갱신, 승리 후 로비 복귀까지 포함하는 전투 흐름으로 확장한다.

## Checklist

- [x] 기존 전투 시작/종료, 슬롯 결과, 유물 발동, 몬스터 Intent, 인벤토리, 로비 이동 흐름 확인 — Codex
- [x] 튜토리얼 유물과 플레이어/몬스터 수치 정의 — Codex
- [x] 튜토리얼 전용 2마리 roster와 2회 고정 스핀 연결 — Codex
- [x] 인벤토리 확인 전 SPIN 잠금과 튜토리얼 단계 안내 연결 — Codex
- [x] 승리 후 튜토리얼 완료 처리 및 로비 복귀 연결 — Codex
- [x] EditMode 빌드/테스트 검증 및 문서 상태 갱신 — Codex

## Notes

- Unity MCP와 Unity Editor 직접 조작 없이 프로젝트 파일 기준으로만 작업했다.
- 일반 전투의 랜덤 슬롯 결과와 기존 reward/starter relic 흐름은 `GameFlowSession.IsTutorialRun`일 때만 분기한다.
- 튜토리얼 유물 `훈련용 배터리`는 일반 `RelicCatalog` 보상풀에 넣지 않고 튜토리얼 런 세션에만 지급한다.

## Completion

- **Outcome**: 최초/임시 튜토리얼 진입 시 플레이어 HP 20, 왼쪽 공격 몬스터 HP 6, 오른쪽 방어 몬스터 HP 8로 전투가 시작된다. 인벤토리 확인 뒤 첫 스핀은 체리 3·레몬 3 족보와 유물 반응을 보여주고, 오른쪽 몬스터는 방어 후 다음 공격 의도를 표시한다. 두 번째 스핀은 강한 체리 족보와 유물 추가 피해로 남은 몬스터를 처치하고 로비로 복귀한다.
- **Validation**: `dotnet build SlotRogue.UI.Tests.csproj`, `dotnet build SlotRogue.Slot.Tests.csproj`, `dotnet test SlotRogue.UI.Tests.csproj --no-build`, `dotnet test SlotRogue.Slot.Tests.csproj --no-build` 종료 성공.
