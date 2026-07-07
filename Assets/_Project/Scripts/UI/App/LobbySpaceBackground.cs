using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.App
{
    /// <summary>
    /// 로비 행성 부유 애니메이션 프리셋(위치·속도·투명도).
    /// GameStartSceneRoot가 배열로 노출해 인스펙터에서 조절할 수 있도록 한다.
    /// </summary>
    [Serializable]
    public sealed class LobbyPlanetPreset
    {
        [Tooltip("행성 시작 위치(레이어 중심 기준, px)")]
        public Vector2 startPosition;

        [Tooltip("가로 드리프트 속도(px/s). 음수=왼쪽, 절댓값 클수록 빠름")]
        public float driftSpeed;

        [Tooltip("좌우 흔들림 진폭(px)")]
        public float bobX;

        [Tooltip("상하 흔들림 진폭(px)")]
        public float bobY;

        [Tooltip("흔들림 속도(빈도). 클수록 빨리 출렁인다")]
        public float bobFrequency;

        [Tooltip("흔들림/회전 위상 오프셋(행성마다 다르게 주면 자연스러움)")]
        public float phase;

        [Tooltip("자전 속도(도/s). 클수록 빨리 회전, 음수=반대 방향")]
        public float rotationSpeed;

        [Range(0f, 1f)]
        [Tooltip("행성 투명도")]
        public float alpha = 1f;
    }

    /// <summary>
    /// 로비 우주 배경의 행성 부유(드리프트/보브/회전) 애니메이션을 담당하는 순수 헬퍼입니다.
    /// 하이어라키에 미리 배치된 "Animated Planet Layer / Floating Planet NN"을 바인딩만 하고
    /// (런타임 생성 금지 — UI는 에디터 저작), 소유자(GameStartSceneRoot)가 매 프레임 Tick합니다.
    /// 프리셋은 인스펙터에서 주입하며, 비어 있으면 <see cref="CreateDefaultPresets"/>를 사용합니다.
    /// </summary>
    public sealed class LobbySpaceBackground
    {
        private const float PlanetScale = 4f;

        private RectTransform _planetLayer;
        private Instance[] _instances = Array.Empty<Instance>();
        private float _elapsed;

        /// <summary>인스펙터 미설정 시 사용할 기본 프리셋 4종.</summary>
        public static LobbyPlanetPreset[] CreateDefaultPresets()
        {
            return new[]
            {
                new LobbyPlanetPreset
                {
                    startPosition = new Vector2(-300f, 58f), driftSpeed = -10.0f,
                    bobX = 16f, bobY = 10f, bobFrequency = 0.34f, phase = 0.2f,
                    rotationSpeed = 2.0f, alpha = 0.72f,
                },
                new LobbyPlanetPreset
                {
                    startPosition = new Vector2(-86f, -46f), driftSpeed = -7.5f,
                    bobX = 12f, bobY = 8f, bobFrequency = 0.48f, phase = 1.5f,
                    rotationSpeed = -6.0f, alpha = 0.82f,
                },
                new LobbyPlanetPreset
                {
                    startPosition = new Vector2(148f, 38f), driftSpeed = -5.5f,
                    bobX = 14f, bobY = 9f, bobFrequency = 0.40f, phase = 2.7f,
                    rotationSpeed = 4.0f, alpha = 0.72f,
                },
                new LobbyPlanetPreset
                {
                    startPosition = new Vector2(340f, -22f), driftSpeed = -4.0f,
                    bobX = 18f, bobY = 11f, bobFrequency = 0.30f, phase = 4.0f,
                    rotationSpeed = -1.5f, alpha = 0.65f,
                },
            };
        }

        /// <summary>씬에 배치된 행성 레이어를 프리셋대로 구성합니다. presets가 비면 기본값을 씁니다.</summary>
        public void Bind(
            RectTransform planetLayer,
            RectTransform[] planets,
            LobbyPlanetPreset[] presets = null)
        {
            _planetLayer = planetLayer;
            if (_planetLayer == null)
            {
                Debug.LogError(
                    "[LobbySpaceBackground] Animated Planet Layer must be wired in the inspector.");
                _instances = Array.Empty<Instance>();
                return;
            }

            LobbyPlanetPreset[] effectivePresets =
                presets != null && presets.Length > 0 ? presets : CreateDefaultPresets();

            var instances = new Instance[effectivePresets.Length];
            int count = 0;
            for (int index = 0; index < effectivePresets.Length; index++)
            {
                LobbyPlanetPreset preset = effectivePresets[index];
                RectTransform planet = planets != null && index < planets.Length
                    ? planets[index]
                    : null;
                if (planet == null)
                {
                    Debug.LogError(
                        $"[LobbySpaceBackground] Floating Planet {index + 1:00} must be wired in the inspector.");
                    continue;
                }

                if (preset == null)
                {
                    continue;
                }

                ConfigurePlanet(planet, preset);
                instances[count] = new Instance(planet, preset, preset.phase * 13f);
                count++;
            }

            _instances = new Instance[count];
            Array.Copy(instances, _instances, count);
            _elapsed = 0f;
        }

        /// <summary>매 프레임 호출해 행성 위치/회전을 갱신합니다.</summary>
        public void Tick(float unscaledDeltaTime)
        {
            if (_instances == null || _instances.Length == 0)
            {
                return;
            }

            _elapsed += unscaledDeltaTime;
            Vector2 layerSize = ResolveLayerSize();
            float wrapWidth = layerSize.x + 192f;

            for (int index = 0; index < _instances.Length; index++)
            {
                Instance planet = _instances[index];
                if (planet.Rect == null || planet.Preset == null)
                {
                    continue;
                }

                LobbyPlanetPreset preset = planet.Preset;
                float wrappedX = Wrap(
                    preset.startPosition.x + (preset.driftSpeed * _elapsed),
                    -wrapWidth * 0.5f,
                    wrapWidth * 0.5f);
                float bobX = Mathf.Sin(
                    (_elapsed * preset.bobFrequency * 0.73f) + preset.phase) *
                    preset.bobX;
                float bobY = Mathf.Sin(
                    (_elapsed * preset.bobFrequency) + preset.phase) *
                    preset.bobY;

                planet.Rect.anchoredPosition =
                    new Vector2(wrappedX + bobX, preset.startPosition.y + bobY);
                planet.Rect.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        planet.InitialRotation + (preset.rotationSpeed * _elapsed));
            }
        }

        private Vector2 ResolveLayerSize()
        {
            if (_planetLayer == null)
            {
                return new Vector2(820f, 260f);
            }

            Rect rect = _planetLayer.rect;
            return rect.size.sqrMagnitude > 0f ? rect.size : new Vector2(820f, 260f);
        }

        private static void ConfigurePlanet(RectTransform planet, LobbyPlanetPreset preset)
        {
            planet.anchorMin = new Vector2(0.5f, 0.5f);
            planet.anchorMax = new Vector2(0.5f, 0.5f);
            planet.pivot = new Vector2(0.5f, 0.5f);
            planet.anchoredPosition = preset.startPosition;
            planet.localScale = new Vector3(PlanetScale, PlanetScale, PlanetScale);

            Image image = planet.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.raycastTarget = false;
            image.preserveAspect = true;
            image.color = new Color(image.color.r, image.color.g, image.color.b, preset.alpha);
            if (image.sprite != null)
            {
                planet.sizeDelta = image.sprite.rect.size;
            }
        }

        private static float Wrap(float value, float min, float max)
        {
            float length = max - min;
            if (length <= 0f)
            {
                return value;
            }

            return min + Mathf.Repeat(value - min, length);
        }

        private readonly struct Instance
        {
            internal Instance(RectTransform rect, LobbyPlanetPreset preset, float initialRotation)
            {
                Rect = rect;
                Preset = preset;
                InitialRotation = initialRotation;
            }

            internal RectTransform Rect { get; }

            internal LobbyPlanetPreset Preset { get; }

            internal float InitialRotation { get; }
        }
    }
}
