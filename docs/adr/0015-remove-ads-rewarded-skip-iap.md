# ADR-0015: 광고 제거 IAP를 보상형 광고 시청 스킵으로 정의한다

**Status**: accepted
**Date**: 2026-06-14

## Context

SlotRogue에는 전면 광고와 배너 광고가 없고 LevelPlay 보상형 광고만 있다. 일반적인 광고 제거 상품처럼 광고 노출 자체를 제거하면 플레이어가 부활, 추가 보상, 보상 2배 같은 보상 기회를 잃는다.

Non-Consumable 구매는 로컬 캐시만으로 영구 소유권을 판정할 수 없다. Google Play에서 재설치하거나 다른 기기로 이동한 경우 Store 구매 내역을 다시 가져와 로컬 캐시를 복구할 수 있어야 한다.

## Decision

1. 상품 ID는 `remove_ads`, 상품 타입은 `Non-Consumable`로 고정한다.
2. 구매 전에는 기존 LevelPlay 보상형 광고를 끝까지 시청한 뒤 동일한 보상을 지급한다.
3. 구매 후에는 광고를 요청하지 않고 동일한 보상을 즉시 지급한다.
4. 광고 스킵은 보상 횟수를 추가하지 않는다. 부활, 리롤, 추가 보상, 보상 2배의 사용 제한은 구매 전후 동일하게 적용한다.
5. `AdsRemoveState`는 PlayerPrefs를 로컬 캐시로 사용하며 런타임 상태 변경 이벤트를 제공한다.
6. `IapFulfillmentHandler`는 Unity IAP Codeless 이벤트를 수신하고 상품 판정 계층에 전달한다.
7. Store 복원 또는 구매 내역 조회는 동일한 상품 판정 계층을 재사용한다. 로컬 캐시는 Store 소유권을 대체하지 않고 오프라인 실행을 위한 캐시 역할만 한다.

## Consequences

- 보상 지급 코드는 광고 시청 완료와 광고 스킵에서 동일한 callback을 사용한다.
- `remove_ads` 구매자는 보상형 광고 UI를 계속 사용할 수 있지만 문구는 "광고 없이"로 바뀐다.
- Google Play 구매 내역을 fulfillment 계층에 전달하면 로컬 캐시와 UI가 같은 경로로 갱신된다.
- 구매 취소, 환불, 소유권 철회 동기화는 Store 검증 정책이 정해질 때 별도 결정이 필요하다.

## References

- [ADR-0013](./0013-levelplay-rewarded-ads.md)
- [보상형 광고 설계](../design-docs/rewarded-ads.md)
- [광고 제거 IAP 설계](../design-docs/remove-ads-iap.md)
