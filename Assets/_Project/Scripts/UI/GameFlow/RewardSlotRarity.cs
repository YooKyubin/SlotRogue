using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class RewardSlotRarity : MonoBehaviour
    {
        [SerializeField] private RewardRarityPalette _palette;
        [SerializeField] private Image _art;

#if UNITY_EDITOR
        [Header("Editor Preview")]
        [SerializeField] private RewardRarity _previewRarity = RewardRarity.Common;
#endif

        public void Apply(RewardRarity rarity)
        {
            EnsureReferences();

            if (_palette == null)
            {
                return;
            }

            Color color = _palette.ColorFor(rarity);
            ApplyTint(_art, color);
        }

        private void Awake()
        {
            EnsureReferences();
        }

#if UNITY_EDITOR
        public RewardRarityPalette EditorPalette => _palette;

        public void EditorApplyPreview()
        {
            if (!Application.isPlaying)
            {
                Apply(_previewRarity);
            }
        }

        private void OnValidate()
        {
            EditorApplyPreview();
        }
#endif

        private void EnsureReferences()
        {
            if (_art == null)
            {
                Debug.LogError(
                    "[RewardSlotRarity] UI references must be wired in the inspector. Missing: Rarity Art",
                    this);
            }
        }

        private static void ApplyTint(Image image, Color color)
        {
            if (image != null)
            {
                image.color = color;
            }
        }
    }
}
