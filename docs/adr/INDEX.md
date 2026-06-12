# ADR 인덱스

모든 Architecture Decision Records의 목록. 새 ADR 추가 또는 status 변경은 **이 표를 같은 커밋에 갱신**해야 한다.

> 형식·작성 규칙: [`../GOVERNANCE.md`](../GOVERNANCE.md) "ADR 규칙" 섹션. 템플릿: [`TEMPLATE.md`](./TEMPLATE.md).

---

## 결정 목록

| # | 제목 | Status | Date | Supersedes |
|---|------|--------|------|------------|
| [0001](./0001-combat-turn-effect-pipeline.md) | 전투 턴은 Effect 목록 파이프라인으로 처리한다 | proposed | 2026-05-30 | — |
| [0002](./0002-game-flow-is-scene-driven-ui-integration.md) | 게임 플로우는 씬 기반 UI 연동 계층에서 조립한다 | proposed | 2026-05-31 | — |
| [0003](./0003-combat-presentation-replay.md) | 전투 연출은 CombatEvent Replay로 처리한다 | proposed | 2026-05-31 | — |
| [0004](./0004-multi-participant-combat.md) | 전투는 ParticipantId 기반 다인전 구조로 확장한다 | accepted | 2026-06-02 | — |
| [0005](./0005-relic-v23-runtime-model.md) | 유물 런타임 모델은 v23 RelicCatalog로 단일화한다 | accepted | 2026-06-11 | — |
| [0006](./0006-runtime-asset-loading-boundary.md) | 런타임 자산은 Resources가 아닌 조립 계층에서 공급한다 | accepted | 2026-06-11 | — |
| [0007](./0007-addressables-local-runtime-assets.md) | Addressables 로컬 기준선으로 런타임 설정 자산을 공급한다 | accepted | 2026-06-11 | — |
| [0008](./0008-ui-strict-mvvm-boundary.md) | UI는 strict MVVM 경계를 따른다 | accepted | 2026-06-11 | — |
| [0009](./0009-relic-icon-addressable-keys.md) | 유물 아이콘은 논리 키로 Addressable Sprite를 참조한다 | accepted | 2026-06-12 | — |

---

## 후보 결정 (아직 작성 안 됨)

[`../../AGENTS.md`](../../AGENTS.md) §2의 미정 영역. 실제 결정이 내려질 때 ADR로 작성:

- 슬롯 RNG / 페이아웃 모델 (결정론적 시드 vs 비결정론, 고정 RTP vs 동적)
- 세이브 포맷 (JSON 평문 vs 암호화 binary, PlayerPrefs 사용 한계)
- Addressables 원격 호스팅 / 콘텐츠 업데이트 전략
- 광고 / IAP SDK 선택
- 타겟 해상도 / Safe Area 처리 방식
- 브랜치 / PR 워크플로 (trunk-based vs feature branch + PR)

위 목록은 가이드일 뿐이며, **결정이 실제로 내려지기 전에 미리 ADR 파일을 만들지 않는다**.
