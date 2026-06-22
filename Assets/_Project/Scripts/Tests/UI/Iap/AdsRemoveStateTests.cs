using NUnit.Framework;
using SlotRogue.UI.Iap;
using UnityEngine;
using UnityEngine.Purchasing;

namespace SlotRogue.UI.Tests.Iap
{
    public sealed class AdsRemoveStateTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(AdsRemoveState.LocalCacheKey);
            AdsRemoveState.ReloadLocalCache();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(AdsRemoveState.LocalCacheKey);
            AdsRemoveState.ReloadLocalCache();
        }

        [Test]
        public void Unlock_PersistsRemoveAdsLocalCache()
        {
            Assert.That(AdsRemoveState.IsRemoved, Is.False);

            AdsRemoveState.Unlock();

            Assert.That(AdsRemoveState.IsRemoved, Is.True);
            Assert.That(
                PlayerPrefs.GetInt(AdsRemoveState.LocalCacheKey, 0),
                Is.EqualTo(1));
        }

        [Test]
        public void ResetForDebug_RemovesLocalCache()
        {
            AdsRemoveState.Unlock();

            AdsRemoveState.ResetForDebug();

            Assert.That(AdsRemoveState.IsRemoved, Is.False);
            Assert.That(PlayerPrefs.HasKey(AdsRemoveState.LocalCacheKey), Is.False);
        }

        [Test]
        public void Fulfillment_RequiresMatchingNonConsumableProduct()
        {
            Assert.That(
                IapEntitlementFulfillment.TryFulfill(
                    "different_product",
                    ProductType.NonConsumable),
                Is.False);
            Assert.That(
                IapEntitlementFulfillment.TryFulfill(
                    AdsRemoveState.ProductId,
                    ProductType.Consumable),
                Is.False);
            Assert.That(AdsRemoveState.IsRemoved, Is.False);

            Assert.That(
                IapEntitlementFulfillment.TryFulfill(
                    AdsRemoveState.ProductId,
                    ProductType.NonConsumable),
                Is.True);
            Assert.That(AdsRemoveState.IsRemoved, Is.True);
        }
    }
}
