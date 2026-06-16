using System;
using UnityEngine;

namespace SlotRogue.UI.Iap
{
    public static class AdsRemoveState
    {
        public const string ProductId = "remove_ads";
        public const string LocalCacheKey = "slotrogue.iap.remove_ads";

        private static bool _initialized;
        private static bool _isRemoved;

        public static event Action<bool> Changed;

        public static bool IsRemoved
        {
            get
            {
                Initialize();
                return _isRemoved;
            }
        }

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            ReloadLocalCache();
        }

        public static void ReloadLocalCache()
        {
            bool cachedValue = PlayerPrefs.GetInt(LocalCacheKey, 0) == 1;
            bool changed = _initialized && _isRemoved != cachedValue;

            _isRemoved = cachedValue;
            _initialized = true;

            if (changed)
            {
                Changed?.Invoke(_isRemoved);
            }
        }

        public static void Unlock()
        {
            Initialize();
            if (_isRemoved)
            {
                return;
            }

            _isRemoved = true;
            PlayerPrefs.SetInt(LocalCacheKey, 1);
            PlayerPrefs.Save();
            Changed?.Invoke(true);
        }
    }
}
