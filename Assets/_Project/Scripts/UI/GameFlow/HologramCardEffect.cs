using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class HologramCardEffect : MonoBehaviour, IMaterialModifier
    {
        private static readonly int HologramColorId = Shader.PropertyToID("_HologramColor");
        private static readonly int SecondaryColorId = Shader.PropertyToID("_SecondaryColor");
        private static readonly int VariantId = Shader.PropertyToID("_Variant");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int ScanlineStrengthId = Shader.PropertyToID("_ScanlineStrength");
        private static readonly int ScanlineCountId = Shader.PropertyToID("_ScanlineCount");
        private static readonly int ScanlineSpeedId = Shader.PropertyToID("_ScanlineSpeed");
        private static readonly int GlitchStrengthId = Shader.PropertyToID("_GlitchStrength");
        private static readonly int GlitchRateId = Shader.PropertyToID("_GlitchRate");
        private static readonly int RgbSplitId = Shader.PropertyToID("_RgbSplit");
        private static readonly int FlickerStrengthId = Shader.PropertyToID("_FlickerStrength");

        [Header("Target")]
        [SerializeField] private Graphic _targetGraphic;
        [SerializeField] private Material _sourceMaterial;
        [SerializeField] private bool _overrideMaterialProperties = true;

        [Header("Variant")]
        [SerializeField, Range(0, 3)] private int _variant;

        [Header("Color")]
        [SerializeField] private Color _hologramColor = new(0.22f, 0.88f, 1f, 1f);
        [SerializeField] private Color _secondaryColor = new(0.92f, 0.35f, 1f, 1f);
        [SerializeField, Range(0f, 2f)] private float _intensity = 1f;

        [Header("Scanline")]
        [SerializeField, Range(0f, 1f)] private float _scanlineStrength = 0.28f;
        [SerializeField, Range(8f, 180f)] private float _scanlineCount = 96f;
        [SerializeField, Range(-8f, 8f)] private float _scanlineSpeed = 1.35f;

        [Header("Glitch")]
        [SerializeField, Range(0f, 0.04f)] private float _glitchStrength = 0.008f;
        [SerializeField, Range(0f, 32f)] private float _glitchRate = 11f;
        [SerializeField, Range(0f, 0.02f)] private float _rgbSplit = 0.003f;
        [SerializeField, Range(0f, 0.4f)] private float _flickerStrength = 0.08f;

        private Material _runtimeMaterial;
        private Material _runtimeSourceMaterial;

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            Material source = _sourceMaterial != null ? _sourceMaterial : baseMaterial;
            if (source == null)
            {
                return baseMaterial;
            }

            Material material = EnsureRuntimeMaterial(source);
            if (_overrideMaterialProperties)
            {
                ApplyProperties(material);
            }

            return material;
        }

        private void Reset()
        {
            ResolveTargetGraphic();
            SetMaterialDirty();
        }

        private void OnEnable()
        {
            ResolveTargetGraphic();
            SetMaterialDirty();
        }

        private void OnDisable()
        {
            DestroyRuntimeMaterial();
            SetMaterialDirty();
        }

        private void OnDestroy()
        {
            DestroyRuntimeMaterial();
        }

        private void OnValidate()
        {
            ResolveTargetGraphic();
            SetMaterialDirty();
        }

        private Material EnsureRuntimeMaterial(Material source)
        {
            if (_runtimeMaterial == null || _runtimeSourceMaterial != source)
            {
                DestroyRuntimeMaterial();
                _runtimeMaterial = new Material(source)
                {
                    name = $"{source.name} ({nameof(HologramCardEffect)})",
                    hideFlags = HideFlags.DontSave,
                };
                _runtimeSourceMaterial = source;
            }
            else
            {
                _runtimeMaterial.CopyPropertiesFromMaterial(source);
            }

            return _runtimeMaterial;
        }

        private void ApplyProperties(Material material)
        {
            SetFloat(material, VariantId, _variant);
            SetColor(material, HologramColorId, _hologramColor);
            SetColor(material, SecondaryColorId, _secondaryColor);
            SetFloat(material, IntensityId, _intensity);
            SetFloat(material, ScanlineStrengthId, _scanlineStrength);
            SetFloat(material, ScanlineCountId, _scanlineCount);
            SetFloat(material, ScanlineSpeedId, _scanlineSpeed);
            SetFloat(material, GlitchStrengthId, _glitchStrength);
            SetFloat(material, GlitchRateId, _glitchRate);
            SetFloat(material, RgbSplitId, _rgbSplit);
            SetFloat(material, FlickerStrengthId, _flickerStrength);
        }

        private static void SetColor(Material material, int propertyId, Color value)
        {
            if (material.HasProperty(propertyId))
            {
                material.SetColor(propertyId, value);
            }
        }

        private static void SetFloat(Material material, int propertyId, float value)
        {
            if (material.HasProperty(propertyId))
            {
                material.SetFloat(propertyId, value);
            }
        }

        private void ResolveTargetGraphic()
        {
            if (_targetGraphic == null)
            {
                _targetGraphic = GetComponent<Graphic>();
            }
        }

        private void SetMaterialDirty()
        {
            if (_targetGraphic != null)
            {
                _targetGraphic.SetMaterialDirty();
            }
        }

        private void DestroyRuntimeMaterial()
        {
            if (_runtimeMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_runtimeMaterial);
            }
            else
            {
                DestroyImmediate(_runtimeMaterial);
            }

            _runtimeMaterial = null;
            _runtimeSourceMaterial = null;
        }
    }
}
