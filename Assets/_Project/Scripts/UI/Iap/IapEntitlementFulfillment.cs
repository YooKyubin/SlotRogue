using System;
using UnityEngine.Purchasing;

namespace SlotRogue.UI.Iap
{
    public static class IapEntitlementFulfillment
    {
        public static bool TryFulfill(
            string productId,
            ProductType productType)
        {
            if (!string.Equals(
                    productId,
                    AdsRemoveState.ProductId,
                    StringComparison.Ordinal) ||
                productType != ProductType.NonConsumable)
            {
                return false;
            }

            AdsRemoveState.Unlock();
            return true;
        }
    }
}
