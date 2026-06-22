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
            CancellationToken cancellationToken)
        {
            if (keys == null || keys.Count == 0)
            {
                return;
            }

            for (int index = 0; index < keys.Count; index++)
            {
                await LoadAsync(keys[index], cancellationToken);
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
                handle = Addressables.LoadAssetAsync<Sprite>(key);
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
        private readonly AddressableSpriteProvider _modifierSpriteProvider;
        private readonly SlotSymbolTmpSpriteAssetProvider _descriptionSpriteAssetProvider;
        private readonly CancellationTokenSource _loadCts = new();
        private int _startRelicRenderVersion;
        private int _rewardRenderVersion;
        private bool _disposed;

        public RelicIconRenderer()
        {
            _spriteProvider = new AddressableSpriteProvider(RelicIconKeys.Default);
            _modifierSpriteProvider = new AddressableSpriteProvider(string.Empty);
            _descriptionSpriteAssetProvider = new SlotSymbolTmpSpriteAssetProvider();
        }

        public void RenderStartRelicIcons(
            StartArtifactSelectionView view,
            StartRelicSelectViewState state)
        {
            if (_disposed || view == null || state == null)
            {
                return;
            }

            int renderVersion = ++_startRelicRenderVersion;
            ApplyStartRelicIconsAsync(
                view,
                state,
                renderVersion,
                _loadCts.Token).Forget();
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
            _startRelicRenderVersion++;
            _rewardRenderVersion++;
            _loadCts.Cancel();
            _spriteProvider.Dispose();
            _modifierSpriteProvider.Dispose();
            _descriptionSpriteAssetProvider.Dispose();
            _loadCts.Dispose();
        }

        private async UniTask ApplyStartRelicIconsAsync(
            StartArtifactSelectionView view,
            StartRelicSelectViewState state,
            int renderVersion,
            CancellationToken cancellationToken)
        {
            try
            {
                GameFlowOptionView[] views = view.ArtifactOptions;
                int count = Mathf.Min(views?.Length ?? 0, state.Options.Count);
                TMP_SpriteAsset descriptionSpriteAsset =
                    await _descriptionSpriteAssetProvider.LoadAsync(cancellationToken);

                if (renderVersion != _startRelicRenderVersion)
                {
                    return;
                }

                for (int index = 0; index < count; index++)
                {
                    if (views[index] != null)
                    {
                        views[index].SetDescriptionSpriteAsset(descriptionSpriteAsset);
                    }

                    Sprite icon = await _spriteProvider.LoadAsync(
                        state.Options[index].IconKey,
                        cancellationToken);

                    if (renderVersion != _startRelicRenderVersion)
                    {
                        return;
                    }

                    if (views[index] != null)
                    {
                        views[index].SetIcon(icon);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
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

                    string iconKey = state.Options[index].IconKey;
                    if (string.IsNullOrEmpty(iconKey))
                    {
                        if (views[index] != null)
                        {
                            views[index].SetIcon(null);
                            views[index].SetModifierIcon(null);
                            views[index].SetDescriptionSpriteAsset(descriptionSpriteAsset);
                        }

                        continue;
                    }

                    if (views[index] != null)
                    {
                        views[index].SetDescriptionSpriteAsset(descriptionSpriteAsset);
                    }

                    Sprite icon = await _spriteProvider.LoadAsync(
                        iconKey,
                        cancellationToken);

                    if (renderVersion != _rewardRenderVersion)
                    {
                        return;
                    }

                    if (views[index] != null)
                    {
                        views[index].SetIcon(icon);
                    }

                    string modifierIconKey = state.Options[index].ModifierIconKey;
                    Sprite modifierIcon = string.IsNullOrEmpty(modifierIconKey)
                        ? null
                        : await _modifierSpriteProvider.LoadAsync(
                            modifierIconKey,
                            cancellationToken);

                    if (renderVersion != _rewardRenderVersion)
                    {
                        return;
                    }

                    if (views[index] != null)
                    {
                        views[index].SetModifierIcon(modifierIcon);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
