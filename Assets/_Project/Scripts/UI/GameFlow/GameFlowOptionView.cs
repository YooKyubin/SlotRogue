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
        [SerializeField] private Image _modifierIcon;

        public Button Button => _button;

        public GameFlowImageSlot ImageSlot => _imageSlot;

        private void Awake()
        {
            EnsureTextReferences();
            EnsureModifierIconReference();
            SetModifierIcon(null);
        }

        public void Bind(
            Button button,
            TMP_Text titleText,
            TMP_Text descriptionText,
            GameFlowImageSlot imageSlot,
            Image modifierIcon = null)
        {
            _button = button;
            _titleText = titleText;
            _descriptionText = descriptionText;
            _imageSlot = imageSlot;
            _modifierIcon = modifierIcon;
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

        public void SetModifierIcon(Sprite sprite)
        {
            EnsureModifierIconReference();

            if (_modifierIcon == null)
            {
                return;
            }

            _modifierIcon.sprite = sprite;
            _modifierIcon.preserveAspect = true;
            _modifierIcon.enabled = sprite != null;
            _modifierIcon.gameObject.SetActive(sprite != null);
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

        private void EnsureModifierIconReference()
        {
            if (_modifierIcon != null)
            {
                return;
            }

            Image[] images = GetComponentsInChildren<Image>(true);
            for (int index = 0; index < images.Length; index++)
            {
                Image image = images[index];
                if (image != null &&
                    image.name.IndexOf("CountImage", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _modifierIcon = image;
                    return;
                }
            }
        }
    }
}
