# 슬롯 코어 (Slot Core)

**Status**: active  
**Started**: 2026-05-28  
**Owner**: _(슬롯 담당)_  
**Contributors**: _(없음)_  
**Related design-docs**: [`slot-core.md`](../../design-docs/slot-core.md)

## Goal

`Dev_Slot` 씬에서 Battle 코드 수정 없이 5 x 3 슬롯 보드를 독립 테스트한다. Spin 버튼으로 결과를 만들고, 패턴 판정과 전투 요청 데이터를 UI 및 Console에서 확인할 수 있으면 완료다.

## Checklist

- [x] `AGENT.md`, 문서 인덱스, 전투 계약 읽기
- [x] `slot-core.md` 설계 초안 작성
- [x] `Scripts_S/Slot` asmdef 및 Data/Core/ViewModel/View 스크립트 추가
- [x] 5 x 3 보드 생성, 패턴 판정, 수치 계산, 전투 요청 변환 구현
- [x] Slot EditMode 테스트 추가
- [x] `Dev_Slot` 씬에 테스트 UI 연결
- [ ] Unity refresh 후 Console/테스트 검증

## Notes

- Battle 파일, Battle 씬, Battle 프리팹은 수정하지 않는다.
- 현재 전투 계약은 `CombatSpinOutcome.Attack` / `Defense`만 받는다.
- `AttackCount`, `HealAmount`, `IsCritical`은 `SlotCombatRequest`에 보존하고 추후 Battle 계약 확장 시 사용한다.
- 2026-05-28: Unity Editor/MCP 인스턴스가 없어 Unity refresh와 Test Runner 실행은 보류. `dotnet build .\SlotRogue.slnx`는 경고/오류 없이 통과.

## Completion

_(completed/로 옮길 때 채움.)_

- **Finished**:
- **Outcome**:
- **Follow-ups**:
