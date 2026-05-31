using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class GameFlowOptionView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private GameFlowImageSlot _imageSlot;

        public Button Button => _button;

        public GameFlowImageSlot ImageSlot => _imageSlot;

        public void Bind(
            Button button,
            Text titleText,
            Text descriptionText,
            GameFlowImageSlot imageSlot)
        {
            _button = button;
            _titleText = titleText;
            _descriptionText = descriptionText;
            _imageSlot = imageSlot;
        }

        public void SetText(string title, string description)
        {
            if (_titleText != null)
            {
                _titleText.text = title;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = description;
            }
        }
    }
}
