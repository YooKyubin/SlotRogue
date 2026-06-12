# 유물 시스템

**Status**: draft
**Last updated**: 2026-06-12

## Purpose

v23 유물 80종을 하나의 카탈로그로 식별하고, 시작 선택·전투 보상·런 인벤토리·스핀 효과 계산을 같은 데이터 모델로 연결한다. 미구현 효과나 전투 코어 계약이 다른 효과가 현재 플레이에 섞이지 않도록 카탈로그 등록과 Phase 1 실행 가능 여부를 분리한다.

## Decisions

| # | 결정 | 요약 |
|---|------|------|
| R1 | [ADR-0005](../adr/0005-relic-v23-runtime-model.md) | 런타임 유물 모델은 `RelicCatalog`와 `RelicDefinition`으로 단일화한다. |
| R2 | 카탈로그와 보상 풀 분리 | `All`은 80종 전체, `Starters`와 `RewardPool`은 `Phase1` 구현분만 제공한다. |
| R3 | 전투 코어 비침범 | `RelicEffectRunner`는 이번 턴의 피해·방어·회복·상태이상 요청만 만들고 실제 적용은 기존 `CombatEffect[]` 파이프라인이 담당한다. |
| R4 | [ADR-0009](../adr/0009-relic-icon-addressable-keys.md) | 유물 아이콘은 `IconKey`로 식별하고 SceneRoot가 Addressables에서 로드한다. |

## Source data

기획 원본은 `relic_pool_v23_status_balance_patch.html`의 `RELICS` 배열이다. 총 80종이며 등급별 개수는 시작 6, 일반 22, 비일반 20, 레어 16, 전설 8, 저주 8이다. 런타임 카탈로그는 ID, 이름, 조건, 효과 수치를 보존하고, 확률군과 상세 설계 의도는 기획 원본을 참조한다.

시작 유물은 다음 6종으로 고정한다.

| ID | 이름 | 조건 | 효과 |
|----|------|------|------|
| `S-01` | 체리 단검 | 체리 3개 이상 족보 | 피해 +3 |
| `S-02` | 클로버 방패 | 클로버 3개 이상 족보 | 방어도 +3 |
| `S-03` | 종 치료제 | 종 3개 이상 족보 | HP +2 회복 |
| `S-04` | 레몬 칼날 | 레몬 3개 이상 족보 | 피해 +5 |
| `S-05` | 다이아 갑옷 | 다이아 3개 이상 족보 | 방어도 +5 |
| `S-06` | 세븐 붕대 | 7 3개 이상 족보 | HP +4 회복 |

시작 등급에는 상태이상, 저장, 소모, 충전, 영구 성장 효과를 두지 않는다.

## Runtime flow

```text
StartRelicSelectViewModel
→ RelicCatalog.Starters 중 중복 없는 3종 추첨
→ GameFlowSession.OwnedRelics
→ SlotMachineViewModel.Spin()
→ RelicTurnResolver.Resolve()
→ CombatTurnRequestBuilder.Build()
→ SlotCombatRequestToCombatEffectsConverter.Convert()
→ BattleSystem
```

`RelicTurnResolver`는 보유 유물, 전체 패턴 목록, 현재 플레이어/선택 적의 HP·상태 스냅샷을 받아 내부 `RelicEffectRunner`로 계산한다. 여러 유물이 동시에 발동하면 피해·방어·회복은 합산하고 상태이상 요청은 목록으로 전달한다.

시작 선택 프리팹은 카드 3개를 제공하므로, 화면에 진입할 때 S-01~S-06 중 3종을 중복 없이 추첨한다. 카탈로그 순서의 앞 3종만 고정 노출하지 않는다.

## Icon flow

```text
RelicDefinition.IconKey
→ StartRelicOptionViewState / RunRewardOptionViewState
→ RunGameSceneRoot
→ AddressableSpriteProvider
→ GameFlowImageSlot
```

현재 시트 16종의 키는 `RelicIconKeys.Slot00`~`Slot15`다. `RelicCatalog.R()`은 역할별 기본 키를 자동 지정하며, 유물별 전용 아이콘이 추가되면 해당 항목에 `iconKey: RelicIconKeys.SlotXX` 또는 새 Addressable 키 상수를 지정한다. 아이콘 로드 실패 시 `RelicIconKeys.Default`를 사용한다.

새 시트나 개별 Sprite는 `Resources`가 아닌 Addressables 그룹에 등록한다. View와 ViewModel에는 `Addressables.Load*` 호출을 추가하지 않는다.

## Legacy boundary

- 2026-06-12 정적 참조 검증 후 `ArtifactDefinitionSO`, `RelicDataSO`, 관련 Runtime 코드와 Editor builder를 삭제했다.
- 참조가 끊긴 `Assets/_Project/Data/_Legacy/`의 구 Artifact/Relic 자산도 함께 삭제했다.
- 현재 유물 런타임의 단일 진입점은 `RelicCatalog`, `RelicDefinition`, `RelicEffectRunner`, `RelicTurnResolver`다.

## Phase 1

Phase 1은 시작 6종과 기획대로 실행 가능한 Common/Uncommon 일부를 실행한다. 현재는 피해, 방어, 회복만 지원한다.

화상, 감염, 취약, 약화, 가시, 흡혈과 전투당 1회 상태, 전투 시작 훅, 패시브 배율, 보상 선택지 변경, 부활, 복합 저주 효과는 전투 계약이 준비될 때까지 Phase 2다. 해당 유물은 `All`에는 남지만 `RewardPool`과 실행에서는 제외한다.

## 전투 담당 요청사항

아래 항목은 `Yookyubin` 작성 전투 코어의 변경이 필요하므로 유물 계층에서 우회 구현하지 않는다.

1. 화상은 부여 즉시 수치만큼 피해를 주고 몬스터 턴 종료까지 상태를 유지해야 한다. 현재 Burn은 턴 시작에 피해를 준다.
2. 감염은 턴 종료에 현재 스택만큼 피해를 준 뒤 스택이 1 감소해야 한다. 현재 Poison은 턴 시작 피해이며 스택이 감소하지 않는다.
3. 감염 기본 최대 스택과 R-02의 최대 스택 +4 적용 방식을 확정해야 한다. 현재 Poison 최대 스택은 5로 고정돼 있다.
4. 취약은 다음 피해 N회에 +20%를 적용하고 피해를 받을 때마다 적용 횟수가 1 감소해야 한다.
5. 약화는 몬스터의 다음 공격 피해를 감소시키고 공격할 때마다 스택이 1 감소해야 하며 총 감소율은 40%를 넘지 않아야 한다.
6. 가시는 이번 턴에만 유지되고 피격 시 공격자에게 반격 피해를 준 뒤 턴 종료에 제거돼야 한다.
7. 유물 판정 계층이 화상·감염·취약·약화 상태를 구분해 조회할 수 있는 읽기 전용 상태 계약이 필요하다.
8. 피해 효과 발동, 상태 부여, 감염 피해, 방어도 획득, 가시 피해, 흡혈 회복을 구분하는 전투 이벤트가 필요하다.

## Open questions

| ID | 질문 | 비고 |
|----|------|------|
| Q1 | 감염 기본 최대 스택 | v23은 R-02에서 최대 스택 +4를 정의하지만 기본 상한 수치는 명시하지 않는다. |
| Q2 | 카탈로그 외부 데이터화 | 밸런스 반복 속도가 코드 카탈로그 유지 비용을 넘으면 ScriptableObject/JSON/CSV 중 하나를 ADR로 결정한다. |
| Q3 | 실제 릴 확률 기반 기대값 보정 | 기획 원본도 p(3+), p(4+) 계산 후 재보정을 요구한다. 슬롯 확률표 확정 뒤 검증한다. |
| Q4 | 강조 아이콘 시트 전환 | 현재 Prefab 직렬화 fallback으로 남은 강조 시트를 Addressable 상태 키로 사용할지는 버튼 선택 연출 확정 후 결정한다. |

## Alternatives considered

구 모델 병행과 v23 전체 ScriptableObject 재생성은 [ADR-0005](../adr/0005-relic-v23-runtime-model.md)에 기록한다.
