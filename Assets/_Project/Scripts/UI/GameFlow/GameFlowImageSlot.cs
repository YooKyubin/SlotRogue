using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class GameFlowImageSlot : MonoBehaviour
    {
        [SerializeField] private string _slotId;
        [SerializeField] private Image _image;

        public string SlotId => _slotId;

        public Image Image => _image;

        public void Bind(string slotId, Image image)
        {
            _slotId = slotId;
            _image = image;
        }

        public void SetSprite(Sprite sprite)
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }

            if (_image == null)
            {
                return;
            }

            _image.sprite = sprite;
            _image.preserveAspect = true;
        }
    }
}
