using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SlotRogue.UI.App
{
    [RequireComponent(typeof(AudioSource))]
    [DefaultExecutionOrder(-9980)]
    public sealed class SceneButtonSfxPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _clickClip;
        [SerializeField] private bool _includeInactive = true;
        [SerializeField, Min(0.1f)] private float _refreshIntervalSeconds = 0.5f;

        private readonly List<Button> _buttons = new();
        private Scene _scene;
        private int _lastButtonCount = -1;
        private float _nextRefreshTime;

        private void Awake()
        {
            EnsureAudioSource();
            if (!_scene.IsValid())
            {
                _scene = gameObject.scene;
            }
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.unscaledTime + _refreshIntervalSeconds;
            if (CountSceneButtons() != _lastButtonCount)
            {
                Refresh();
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Refresh()
        {
            Unsubscribe();

            if (!_scene.IsValid())
            {
                _scene = gameObject.scene;
            }

            GameObject[] roots = _scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Button[] buttons = roots[rootIndex].GetComponentsInChildren<Button>(_includeInactive);
                for (int buttonIndex = 0; buttonIndex < buttons.Length; buttonIndex++)
                {
                    Button button = buttons[buttonIndex];
                    if (button == null || button.GetComponentInParent<SceneButtonSfxPlayer>() != null)
                    {
                        continue;
                    }

                    button.onClick.AddListener(PlayClick);
                    _buttons.Add(button);
                }
            }

            _lastButtonCount = _buttons.Count;
        }

        private void Unsubscribe()
        {
            for (int index = 0; index < _buttons.Count; index++)
            {
                Button button = _buttons[index];
                if (button != null)
                {
                    button.onClick.RemoveListener(PlayClick);
                }
            }

            _buttons.Clear();
            _lastButtonCount = -1;
        }

        private int CountSceneButtons()
        {
            if (!_scene.IsValid())
            {
                return 0;
            }

            int count = 0;
            GameObject[] roots = _scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                count += roots[rootIndex].GetComponentsInChildren<Button>(_includeInactive).Length;
            }

            return count;
        }

        private void PlayClick()
        {
            EnsureAudioSource();
            AudioClip clip = _clickClip != null ? _clickClip : _audioSource != null ? _audioSource.clip : null;
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        private void EnsureAudioSource()
        {
            if (_audioSource != null)
            {
                return;
            }

            TryGetComponent(out _audioSource);
            if (_audioSource != null)
            {
                _audioSource.playOnAwake = false;
                _audioSource.loop = false;
                _audioSource.spatialBlend = 0f;
            }
        }
    }
}
