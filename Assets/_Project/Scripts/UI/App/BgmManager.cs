using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlotRogue.UI.App
{
    public sealed class BgmManager : MonoBehaviour
    {
        private static BgmManager _instance;

        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _titleClip;
        [SerializeField] private AudioClip _lobbyClip;
        [SerializeField] private AudioClip _battleClip;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.6f;
        [SerializeField, Min(0f)] private float _fadeSeconds = 0.5f;

        private Tween _fadeTween;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSource();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            PlayForScene(SceneManager.GetActiveScene().name, true);
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            _fadeTween?.Kill();
            _fadeTween = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            _instance = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayForScene(scene.name, false);
        }

        private void PlayForScene(string sceneName, bool immediate)
        {
            AudioClip nextClip = sceneName switch
            {
                SceneNames.Title => _titleClip,
                SceneNames.Lobby => _lobbyClip,
                SceneNames.RunGame => _battleClip,
                _ => null
            };

            if (nextClip == null)
            {
                return;
            }

            Play(nextClip, immediate);
        }

        private void Play(AudioClip nextClip, bool immediate)
        {
            EnsureAudioSource();
            if (_audioSource == null ||
                (_audioSource.clip == nextClip && _audioSource.isPlaying))
            {
                return;
            }

            if (immediate || !_audioSource.isPlaying || _fadeSeconds <= 0f)
            {
                _fadeTween?.Kill();
                ApplyClip(nextClip);
                _audioSource.volume = _volume;
                _audioSource.Play();
                return;
            }

            _fadeTween?.Kill();
            _fadeTween = DOTween.Sequence()
                .SetTarget(_audioSource)
                .Append(TweenVolume(0f))
                .AppendCallback(() =>
                {
                    ApplyClip(nextClip);
                    _audioSource.Play();
                })
                .Append(TweenVolume(_volume))
                .OnKill(() => _fadeTween = null);
        }

        private Tween TweenVolume(float targetVolume)
        {
            return DOTween.To(
                    () => _audioSource.volume,
                    value => _audioSource.volume = value,
                    targetVolume,
                    _fadeSeconds)
                .SetTarget(_audioSource);
        }

        private void ApplyClip(AudioClip clip)
        {
            _audioSource.clip = clip;
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
        }

        private void EnsureAudioSource()
        {
            if (_audioSource != null)
            {
                return;
            }

            if (!TryGetComponent(out _audioSource))
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

}
