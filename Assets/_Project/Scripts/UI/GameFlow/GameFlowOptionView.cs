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

        public Button Button => _button;

        public GameFlowImageSlot ImageSlot => _imageSlot;

        private void Awake()
        {
            EnsureTextReferences();
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
    }
}
