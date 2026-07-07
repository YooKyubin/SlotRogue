using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Relics.Pool;
using SlotRogue.UI.RunGame;
using SlotRogue.UI.RunGame.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SlotRogue.UI.GameFlow
{
    internal static class AddressableSpriteCache
    {
        private static readonly Dictionary<string, AsyncOperationHandle<Sprite>> Handles = new();
        private static readonly Dictionary<string, Sprite> Sprites = new();
        private static readonly HashSet<string> FailedKeys = new();

        internal static bool TryGet(string key, out Sprite sprite)
        {
            if (string.IsNullOrEmpty(key))
            {
                sprite = null;
                return false;
            }

            return Sprites.TryGetValue(key, out sprite) && sprite != null;
        }

        internal static async UniTask PreloadAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken,
            IProgress<float> progress = null)
        {
            if (keys == null || keys.Count == 0)
            {
                progress?.Report(1f);
                return;
            }

            progress?.Report(0f);
            for (int index = 0; index < keys.Count; index++)
            {
                await LoadAsync(keys[index], cancellationToken);
                progress?.Report((index + 1f) / keys.Count);
            }
        }

        private static async UniTask<Sprite> LoadAsync(
            string key,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(key) || FailedKeys.Contains(key))
            {
                return null;
            }

            if (TryGet(key, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            if (!Handles.TryGetValue(key, out AsyncOperationHandle<Sprite> handle) ||
                !handle.IsValid())
            {
                // 잘못된/유실된 주소(예: dangling addressable, Sprite로 임포트되지 않은 시트)는
                // LoadAssetAsync가 동기적으로 InvalidKeyException을 던진다. 한 키의 실패가
                // preload 전체를 중단시키지 않도록 잡아서 실패 키로 기록하고 넘어간다.
                try
                {
                    handle = Addressables.LoadAssetAsync<Sprite>(key);
                }
                catch (Exception ex)
                {
                    if (FailedKeys.Add(key))
                    {
                        Debug.LogWarning(
                            $"[AddressableSpriteCache] Sprite '{key}' load threw: {ex.Message}");
                    }

                    return null;
                }

                Handles[key] = handle;
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
                Sprites[key] = handle.Result;
                return handle.Result;
            }

            if (FailedKeys.Add(key))
            {
                string reason = handle.OperationException?.Message ?? "unknown error";
                Debug.LogWarning(
                    $"[AddressableSpriteCache] Sprite '{key}' preload failed: {reason}");
            }

            return null;
        }
    }

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

            if (AddressableSpriteCache.TryGet(key, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            if (!_handles.TryGetValue(key, out AsyncOperationHandle<Sprite> handle))
            {
                // 잘못된/유실된 주소는 LoadAssetAsync가 동기적으로 예외를 던진다.
                // 호출부(아이콘 렌더)가 죽지 않도록 잡아서 실패 키로 기록하고 null을 반환한다.
                try
                {
                    handle = Addressables.LoadAssetAsync<Sprite>(key);
                }
                catch (Exception ex)
                {
                    if (_failedKeys.Add(key))
                    {
                        Debug.LogWarning(
                            $"[AddressableSpriteProvider] Sprite '{key}' load threw: {ex.Message}");
                    }

                    return null;
                }

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

    internal sealed class SlotSymbolTmpSpriteAssetProvider : IDisposable
    {
        private AsyncOperationHandle<TMP_SpriteAsset> _handle;
        private TMP_SpriteAsset _spriteAsset;
        private bool _failed;

        public async UniTask<TMP_SpriteAsset> LoadAsync(CancellationToken cancellationToken)
        {
            if (_spriteAsset != null)
            {
                return _spriteAsset;
            }

            if (_failed)
            {
                return null;
            }

            if (!_handle.IsValid())
            {
                _handle = Addressables.LoadAssetAsync<TMP_SpriteAsset>(
                    SlotSymbolIconKeys.TmpSpriteAssetAddress);
            }

            while (_handle.IsValid() && !_handle.IsDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (!_handle.IsValid())
            {
                return null;
            }

            if (_handle.Status == AsyncOperationStatus.Succeeded)
            {
                _spriteAsset = _handle.Result;
                return _spriteAsset;
            }

            if (!_failed)
            {
                _failed = true;
                string reason = _handle.OperationException?.Message ?? "unknown error";
                Debug.LogWarning(
                    $"[SlotSymbolTmpSpriteAssetProvider] TMP sprite asset '{SlotSymbolIconKeys.TmpSpriteAssetAddress}' load failed: {reason}");
            }

            return null;
        }

        public void Dispose()
        {
            if (_handle.IsValid())
            {
                Addressables.Release(_handle);
            }

            _spriteAsset = null;
            _failed = false;
        }
    }

    public sealed class RelicIconRenderer : IDisposable
    {
        private readonly AddressableSpriteProvider _spriteProvider;
        private readonly SlotSymbolTmpSpriteAssetProvider _descriptionSpriteAssetProvider;
        private readonly CancellationTokenSource _loadCts = new();
        private int _rewardRenderVersion;
        private bool _disposed;

        public RelicIconRenderer()
        {
            _spriteProvider = new AddressableSpriteProvider(RelicIconKeys.Default);
            _descriptionSpriteAssetProvider = new SlotSymbolTmpSpriteAssetProvider();
        }

        public void RenderRewardIcons(
            RunRewardView view,
            RunRewardViewState state)
        {
            if (_disposed || view == null || state == null)
            {
                return;
            }

            int renderVersion = ++_rewardRenderVersion;
            ApplyRewardIconsAsync(
                view,
                state,
                renderVersion,
                _loadCts.Token).Forget();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _rewardRenderVersion++;
            _loadCts.Cancel();
            _spriteProvider.Dispose();
            _descriptionSpriteAssetProvider.Dispose();
            _loadCts.Dispose();
        }

        private async UniTask ApplyRewardIconsAsync(
            RunRewardView view,
            RunRewardViewState state,
            int renderVersion,
            CancellationToken cancellationToken)
        {
            try
            {
                GameFlowOptionView[] views = view.RewardOptions;
                int count = Mathf.Min(views?.Length ?? 0, state.Options.Count);
                TMP_SpriteAsset descriptionSpriteAsset =
                    await _descriptionSpriteAssetProvider.LoadAsync(cancellationToken);

                if (renderVersion != _rewardRenderVersion)
                {
                    return;
                }

                for (int index = 0; index < count; index++)
                {
                    if (cancellationToken.IsCancellationRequested ||
                        renderVersion != _rewardRenderVersion)
                    {
                        return;
                    }

                    if (views[index] != null)
                    {
                        views[index].SetDescriptionSpriteAsset(descriptionSpriteAsset);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
