using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 보상 슬롯 아트 1장에 등급색을 단색 tint로 입힌다.
    /// 아트 스프라이트는 카드/안쪽/테두리를 밝기 차이로 그린 회색 이미지이고, 색은 Image.color로 곱한다.
    /// 레퍼런스가 비어 있으면 자식에서 이름으로 자동 해석한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RewardSlotRarity : MonoBehaviour
    {
        public const string ArtChildName = "RarityArt";
        public const string ModifierArtChildName = "CountImage";

        [SerializeField] private RewardRarityPalette _palette;
        [SerializeField] private Image _art;
        [SerializeField] private Image _modifierArt;

#if UNITY_EDITOR
        [Header("Editor Preview")]
        [Tooltip("플레이하지 않아도 이 등급 색으로 씬에 미리 칠해 본다(빌드에는 영향 없음).")]
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
            ApplyTint(_modifierArt, color);
        }

        private void Awake()
        {
            EnsureReferences();
        }

#if UNITY_EDITOR
        public RewardRarityPalette EditorPalette => _palette;

        /// <summary>팔레트 에셋이 바뀔 때 외부(RewardRarityPalette.OnValidate)에서 호출해 미리보기를 갱신.</summary>
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
            if (_art != null && _modifierArt != null)
            {
                return;
            }

            Image[] images = GetComponentsInChildren<Image>(true);
            for (int index = 0; index < images.Length; index++)
            {
                Image image = images[index];
                if (image == null)
                {
                    continue;
                }

                if (_art == null &&
                    string.Equals(image.name, ArtChildName, System.StringComparison.Ordinal))
                {
                    _art = image;
                    continue;
                }

                if (_modifierArt == null &&
                    string.Equals(image.name, ModifierArtChildName, System.StringComparison.Ordinal))
                {
                    _modifierArt = image;
                }
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
