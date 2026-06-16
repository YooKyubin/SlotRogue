# 광고 제거 IAP

**Status**: accepted
**Last updated**: 2026-06-14

## Purpose

`remove_ads` Non-Consumable 상품을 구매한 플레이어가 보상형 광고를 시청하지 않고도 기존과 같은 부활, 리롤, 추가 보상, 보상 2배를 받을 수 있게 한다.

## Decisions

- [ADR-0015](../adr/0015-remove-ads-rewarded-skip-iap.md): 광고 제거 구매는 보상 자체가 아니라 보상형 광고 시청을 제거한다.
- [ADR-0013](../adr/0013-levelplay-rewarded-ads.md): 구매 전 광고 표시는 기존 LevelPlay `AdsManager` 단일 경로를 사용한다.

## 런타임 구성

| 구성 요소 | 책임 |
|----------|------|
| `AdsRemoveState` | PlayerPrefs 로컬 캐시 읽기/쓰기, `IsRemoved`, 상태 변경 이벤트 |
| `IapStoreConnectionCallbacks` | Codeless 초기화 전에 Store 연결/해제 콜백 등록 |
| `IapEntitlementFulfillment` | 상품 ID와 타입 검증, 광고 제거 권한 해금 |
| `IapFulfillmentHandler` | Codeless IAP 구매 pending 및 Store 구매 내역 이벤트를 런타임 권한으로 변환 |
| `RunGameSceneRoot` | 구매 상태를 먼저 확인하고 광고 시청 또는 즉시 보상 경로 선택 |
| `RunRewardViewModel`, `RunDefeatViewModel` | 구매 상태와 광고 준비 상태를 조합해 버튼 활성화 및 문구 계산 |

## 보상 지급 흐름

```text
보상형 버튼 입력
  -> 기존 횟수 제한 검사
  -> AdsRemoveState.IsRemoved 검사
     -> true: 동일 보상 callback 즉시 실행
     -> false: LevelPlay 보상형 광고 표시
        -> 보상 획득 callback에서 동일 보상 callback 실행
```

광고 제거 구매는 횟수 제한을 우회하지 않는다. 각 기능은 구매 여부와 무관하게 기존의 단일 사용 상태를 유지한다.

## 로컬 캐시와 복원

- PlayerPrefs 키는 `slotrogue.iap.remove_ads`를 사용한다.
- 구매 성공 또는 Store 구매 내역 확인 시 `IapEntitlementFulfillment`가 `AdsRemoveState.Unlock()`을 호출한다.
- 재설치 후 Google Play 구매 복원은 Store 조회 결과의 `Product` 또는 `Order`를 `IapFulfillmentHandler`에 전달해 같은 경로로 처리한다.
- 로컬 캐시는 빠른 부팅과 오프라인 실행을 위한 값이다. 영수증 검증과 환불 반영 정책은 별도 Store 정책에서 다룬다.

## Codeless IAP 연결

`Assets/Resources/IAPProductCatalog.json`에 다음 상품을 등록한다.

| 항목 | 값 |
|------|----|
| Product ID | `remove_ads` |
| Product Type | `Non-Consumable` |

GameStart 씬의 `00_GameStartArea/Remove Ads Button`은 기존 `Start Button` 스타일을 복제한 직렬화 오브젝트다. Hierarchy와 Inspector에서 위치, 문구, `Button`, `CodelessIAPButton`, `IapFulfillmentHandler` 구성을 바로 확인하고 조정할 수 있다.

`CodelessIAPButton`의 `On Order Pending (PendingOrder)`는 `IapFulfillmentHandler.OnOrderPending(PendingOrder)`에, `On Purchase Fetched (Order)`는 `IapFulfillmentHandler.OnPurchaseFetched(Order)`에 Inspector 영구 이벤트로 연결한다. Product 인자를 제공하는 기존 Codeless 이벤트에서는 요청한 `OnPurchasePending(Product)`도 사용할 수 있다.

Store 구매 내역 조회와 복원 결과는 `OnPurchaseFetched(Order)` 또는 `OnRestoredProduct(Product)`로 전달한다. 이 함수들은 구매 성공과 같은 `IapEntitlementFulfillment` 경로를 사용한다.

`IapStoreConnectionCallbacks`는 Boot와 GameStart 직행 실행에서 Codeless 초기화보다 먼저 동일 Store service의 `OnStoreConnected`, `OnStoreDisconnected` 콜백을 등록한다. Unity IAP 5.3.1의 callback 미등록 경고를 막고 후속 연결 상태 처리 지점을 제공한다.

구매 전 버튼 문구는 `광고 제거 구매`, 구매 완료 후 문구는 `광고 제거 구매 완료`다. 구매 완료 상태에서는 `Button.interactable`만 비활성화한다. `CodelessIAPButton` 컴포넌트를 pending 이벤트 처리 중 비활성화하면 Unity IAP 5.3.1이 순회 중인 내부 버튼 목록이 변경되므로 컴포넌트는 활성 상태를 유지한다.

## Open questions

- Google Play 영수증 서버 검증과 환불/소유권 철회 반영 정책
- iOS Restore Purchases UI가 필요할 때의 화면 위치

## 수동 검증

1. GameStart에서 Start 버튼 아래에 `광고 제거 구매` 버튼이 표시되는지 확인한다.
2. 로컬 캐시가 없는 상태에서 각 보상 버튼이 "광고 보고" 문구를 표시하는지 확인한다.
3. 광고 완료 전에는 보상이 지급되지 않고 완료 후 한 번만 지급되는지 확인한다.
4. `remove_ads` 구매 처리 후 GameStart 문구가 `광고 제거 구매 완료`로 바뀌고 버튼이 잠기는지 확인한다.
5. 구매 처리 후 보상형 버튼 문구가 화면 전환 없이 "광고 없이"로 바뀌는지 확인한다.
6. 구매 후 버튼 입력 시 LevelPlay 광고가 열리지 않고 같은 보상이 즉시 지급되는지 확인한다.
7. 구매 전후 부활, 리롤, 추가 보상, 보상 2배의 횟수 제한이 동일한지 확인한다.
8. 앱 재실행 후 PlayerPrefs 캐시로 구매 상태가 유지되는지 확인한다.
9. Google Play 구매 내역 조회 결과를 fulfillment handler에 전달했을 때 재설치 환경에서도 해금되는지 확인한다.
