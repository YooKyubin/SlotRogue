using UnityEngine;
using UnityEngine.Purchasing;

namespace SlotRogue.UI.Iap
{
    public static class IapStoreConnectionCallbacks
    {
        private static IStoreService _storeService;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Unregister();
        }

        public static void Register()
        {
            IStoreService storeService = UnityIAPServices.DefaultStore();
            if (ReferenceEquals(_storeService, storeService))
            {
                return;
            }

            Unregister();

            _storeService = storeService;
            _storeService.OnStoreConnected += HandleStoreConnected;
            _storeService.OnStoreDisconnected += HandleStoreDisconnected;
        }

        private static void Unregister()
        {
            if (_storeService == null)
            {
                return;
            }

            _storeService.OnStoreConnected -= HandleStoreConnected;
            _storeService.OnStoreDisconnected -= HandleStoreDisconnected;
            _storeService = null;
        }

        private static void HandleStoreConnected()
        {
        }

        private static void HandleStoreDisconnected(
            StoreConnectionFailureDescription failure)
        {
            // 스토어 연결 실패는 무음으로 두면 구매/복원 무반응의 원인 추적이 어렵다.
            Debug.LogWarning(
                $"[Iap] Store disconnected: {failure.message}");
        }
    }

    public sealed class IapFulfillmentHandler : MonoBehaviour
    {
        public void OnPurchasePending(Product product)
        {
            FulfillProduct(product);
        }

        public void OnOrderPending(PendingOrder order)
        {
            FulfillOrder(order);
        }

        public void OnPurchaseFetched(Order order)
        {
            FulfillOrder(order);
        }

        public void OnRestoredProduct(Product product)
        {
            FulfillProduct(product);
        }

        private static void FulfillOrder(Order order)
        {
            if (order?.CartOrdered?.Items() == null)
            {
                return;
            }

            foreach (CartItem item in order.CartOrdered.Items())
            {
                FulfillProduct(item?.Product);
            }
        }

        private static void FulfillProduct(Product product)
        {
            if (product?.definition == null)
            {
                return;
            }

            bool fulfilled = IapEntitlementFulfillment.TryFulfill(
                product.definition.id,
                product.definition.type);
            if (!fulfilled &&
                product.definition.id == AdsRemoveState.ProductId)
            {
                Debug.LogError(
                    $"[IapFulfillmentHandler] {AdsRemoveState.ProductId} " +
                    "must be configured as Non-Consumable.");
            }
        }
    }
}
