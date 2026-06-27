using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class GameFlowOptionView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private GameFlowImageSlot _imageSlot;
        [Tooltip("수치 배지 배경(CountImage). 등급색 tint는 RewardSlotRarity가 입힌다.")]
        [SerializeField] private Image _countImage;
        [Tooltip("CountImage 하위의 수치 텍스트(+1/-1 등).")]
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private RewardSlotRarity _rarity;

        public Button Button => _button;

        public GameFlowImageSlot ImageSlot => _imageSlot;

        private void Awake()
        {
            EnsureTextReferences();
            EnsureCountReferences();
            SetModifierLabel(null);
        }

        public void Bind(
            Button button,
            TMP_Text titleText,
            TMP_Text descriptionText,
            GameFlowImageSlot imageSlot)
        {
            _button = button;
            _titleText = titleText;
            _descriptionText = descriptionText;
            _imageSlot = imageSlot;
        }

        public void SetText(string title, string description)
        {
            EnsureTextReferences();

            if (_titleText != null)
            {
                _titleText.text = title;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = description;
            }

        }

        public void SetIcon(Sprite sprite)
        {
            if (_imageSlot != null)
            {
                _imageSlot.SetSprite(sprite);
            }
        }

        public void SetRarity(RewardRarity rarity)
        {
            if (_rarity == null)
            {
                _rarity = GetComponent<RewardSlotRarity>();
            }

            if (_rarity != null)
            {
                _rarity.Apply(rarity);
            }
        }

        /// <summary>
        /// 수치 배지 텍스트(+1/-1 등)를 설정한다. label이 비어 있으면 배지를 숨긴다.
        /// (과거에는 CountImage 스프라이트를 교체했으나, 이제 CountText만 갱신한다.)
        /// </summary>
        public void SetModifierLabel(string label)
        {
            EnsureCountReferences();

            bool show = !string.IsNullOrEmpty(label);

            if (_countText != null)
            {
                _countText.text = label ?? string.Empty;
            }

            if (_countImage != null)
            {
                _countImage.gameObject.SetActive(show);
            }
        }

        public void SetDescriptionSpriteAsset(TMP_SpriteAsset spriteAsset)
        {
            EnsureTextReferences();

            if (_descriptionText == null || _descriptionText.spriteAsset == spriteAsset)
            {
                return;
            }

            _descriptionText.spriteAsset = spriteAsset;
            _descriptionText.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
        }

        private void EnsureTextReferences()
        {
            if (_titleText != null && _descriptionText != null)
            {
                return;
            }

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                TMP_Text text = texts[index];
                if (text == null)
                {
                    continue;
                }

                if (_descriptionText == null &&
                    text.name.IndexOf("Description", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _descriptionText = text;
                    continue;
                }

                if (_titleText == null &&
                    (text.name.IndexOf("Name", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     text.name.IndexOf("Title", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    _titleText = text;
                }
            }
        }

        private void EnsureCountReferences()
        {
            if (_countImage == null)
            {
                Image[] images = GetComponentsInChildren<Image>(true);
                for (int index = 0; index < images.Length; index++)
                {
                    Image image = images[index];
                    if (image != null &&
                        image.name.IndexOf("CountImage", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _countImage = image;
                        break;
                    }
                }
            }

            if (_countText == null)
            {
                TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
                for (int index = 0; index < texts.Length; index++)
                {
                    TMP_Text text = texts[index];
                    if (text != null &&
                        text.name.IndexOf("CountText", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _countText = text;
                        break;
                    }
                }
            }
        }
    }
}
