using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SlotRogue.UI.App
{
    /// <summary>
    /// 로비 우주 배경의 행성 부유(드리프트/보브/회전) 애니메이션을 담당하는 순수 헬퍼입니다.
    /// 하이어라키에 미리 배치된 "Animated Planet Layer / Floating Planet NN"을 바인딩만 하고
    /// (런타임 생성 금지 — UI는 에디터 저작), 소유자(GameStartSceneRoot)가 매 프레임 Tick합니다.
    /// </summary>
    public sealed class LobbySpaceBackground
    {
        private const string PlanetLayerName = "Animated Planet Layer";
        private const string PlanetNamePrefix = "Floating Planet ";
        private const float PlanetScale = 4f;

        private static readonly Preset[] Presets =
        {
            new(new Vector2(-300f, 58f), -10.0f, 16f, 10f, 0.34f, 0.2f, 2.0f, 0.72f),
            new(new Vector2(-86f, -46f), -7.5f, 12f, 8f, 0.48f, 1.5f, -6.0f, 0.82f),
            new(new Vector2(148f, 38f), -5.5f, 14f, 9f, 0.40f, 2.7f, 4.0f, 0.72f),
            new(new Vector2(340f, -22f), -4.0f, 18f, 11f, 0.30f, 4.0f, -1.5f, 0.65f),
        };

        private RectTransform _planetLayer;
        private Instance[] _instances = Array.Empty<Instance>();
        private float _elapsed;

        /// <summary>씬에 배치된 행성 레이어를 찾아 프리셋대로 구성합니다.</summary>
        public void Bind(Scene scene)
        {
            _planetLayer = FindSceneChild(scene, PlanetLayerName) as RectTransform;
            if (_planetLayer == null)
            {
                Debug.LogWarning(
                    "[LobbySpaceBackground] Animated Planet Layer must be placed in the lobby scene hierarchy.");
                _instances = Array.Empty<Instance>();
                return;
            }

            var instances = new Instance[Presets.Length];
            int count = 0;
            for (int index = 0; index < Presets.Length; index++)
            {
                string objectName = $"{PlanetNamePrefix}{index + 1:00}";
                RectTransform planet = _planetLayer.Find(objectName) as RectTransform;
                if (planet == null)
                {
                    Debug.LogWarning(
                        $"[LobbySpaceBackground] {objectName} must be placed under {PlanetLayerName}.");
                    continue;
                }

                Preset preset = Presets[index];
                ConfigurePlanet(planet, preset);
                instances[count] = new Instance(planet, preset, preset.Phase * 13f);
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
                if (planet.Rect == null)
                {
                    continue;
                }

                Preset preset = planet.Preset;
                float wrappedX = Wrap(
                    preset.StartPosition.x + (preset.DriftSpeed * _elapsed),
                    -wrapWidth * 0.5f,
                    wrapWidth * 0.5f);
                float bobX = Mathf.Sin(
                    (_elapsed * preset.BobFrequency * 0.73f) + preset.Phase) *
                    preset.BobX;
                float bobY = Mathf.Sin(
                    (_elapsed * preset.BobFrequency) + preset.Phase) *
                    preset.BobY;

                planet.Rect.anchoredPosition =
                    new Vector2(wrappedX + bobX, preset.StartPosition.y + bobY);
                planet.Rect.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        planet.InitialRotation + (preset.RotationSpeed * _elapsed));
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

        private static void ConfigurePlanet(RectTransform planet, Preset preset)
        {
            planet.anchorMin = new Vector2(0.5f, 0.5f);
            planet.anchorMax = new Vector2(0.5f, 0.5f);
            planet.pivot = new Vector2(0.5f, 0.5f);
            planet.anchoredPosition = preset.StartPosition;
            planet.localScale = new Vector3(PlanetScale, PlanetScale, PlanetScale);

            Image image = planet.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.raycastTarget = false;
            image.preserveAspect = true;
            image.color = new Color(image.color.r, image.color.g, image.color.b, preset.Alpha);
            if (image.sprite != null)
            {
                planet.sizeDelta = image.sprite.rect.size;
            }
        }

        private static Transform FindSceneChild(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                Transform found = FindDeepChild(roots[index].transform, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindDeepChild(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == objectName)
            {
                return parent;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform found = FindDeepChild(parent.GetChild(index), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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

        private readonly struct Preset
        {
            internal Preset(
                Vector2 startPosition,
                float driftSpeed,
                float bobX,
                float bobY,
                float bobFrequency,
                float phase,
                float rotationSpeed,
                float alpha)
            {
                StartPosition = startPosition;
                DriftSpeed = driftSpeed;
                BobX = bobX;
                BobY = bobY;
                BobFrequency = bobFrequency;
                Phase = phase;
                RotationSpeed = rotationSpeed;
                Alpha = alpha;
            }

            internal Vector2 StartPosition { get; }

            internal float DriftSpeed { get; }

            internal float BobX { get; }

            internal float BobY { get; }

            internal float BobFrequency { get; }

            internal float Phase { get; }

            internal float RotationSpeed { get; }

            internal float Alpha { get; }
        }

        private readonly struct Instance
        {
            internal Instance(RectTransform rect, Preset preset, float initialRotation)
            {
                Rect = rect;
                Preset = preset;
                InitialRotation = initialRotation;
            }

            internal RectTransform Rect { get; }

            internal Preset Preset { get; }

            internal float InitialRotation { get; }
        }
    }
}
