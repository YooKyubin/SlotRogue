using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SlotRogue.UI.GameFlow
{
    public sealed class AddressableSpriteProvider : IDisposable
    {
        private readonly string _fallbackKey;
        private readonly Dictionary<string, AsyncOperationHandle<Sprite>> _handles = new();
        private readonly HashSet<string> _failedKeys = new();

        public AddressableSpriteProvider(string fallbackKey)
        {
            _fallbackKey = fallbackKey ?? string.Empty;
        }

        public async UniTask<Sprite> LoadAsync(
            string key,
            CancellationToken cancellationToken)
        {
            Sprite sprite = await LoadCoreAsync(key, cancellationToken);
            if (sprite != null ||
                string.IsNullOrEmpty(_fallbackKey) ||
                string.Equals(key, _fallbackKey, StringComparison.Ordinal))
            {
                return sprite;
            }

            return await LoadCoreAsync(_fallbackKey, cancellationToken);
        }

        public void Dispose()
        {
            foreach (AsyncOperationHandle<Sprite> handle in _handles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            _handles.Clear();
            _failedKeys.Clear();
        }

        private async UniTask<Sprite> LoadCoreAsync(
            string key,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(key) || _failedKeys.Contains(key))
            {
                return null;
            }

            if (!_handles.TryGetValue(key, out AsyncOperationHandle<Sprite> handle))
            {
                handle = Addressables.LoadAssetAsync<Sprite>(key);
                _handles.Add(key, handle);
            }

            while (handle.IsValid() && !handle.IsDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (!handle.IsValid())
            {
                return null;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }

            if (_failedKeys.Add(key))
            {
                string reason = handle.OperationException?.Message ?? "unknown error";
                Debug.LogWarning(
                    $"[AddressableSpriteProvider] Sprite '{key}' load failed: {reason}");
            }

            return null;
        }
    }
}
