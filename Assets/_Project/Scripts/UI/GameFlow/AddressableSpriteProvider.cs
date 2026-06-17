using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Relics.Pool;
using SlotRogue.UI.RunGame;
using SlotRogue.UI.RunGame.ViewModels;
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

    public sealed class RelicIconRenderer : IDisposable
    {
        private readonly AddressableSpriteProvider _spriteProvider;
        private readonly CancellationTokenSource _loadCts = new();
        private int _startRelicRenderVersion;
        private int _rewardRenderVersion;
        private bool _disposed;

        public RelicIconRenderer()
        {
            _spriteProvider = new AddressableSpriteProvider(RelicIconKeys.Default);
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

                for (int index = 0; index < count; index++)
                {
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
                        }

                        continue;
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
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
