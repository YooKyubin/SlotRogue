using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunMapNodeView : MonoBehaviour
    {
        [SerializeField] private string _nodeId;
        [SerializeField] private Button _button;
        [SerializeField] private Image _image;
        [SerializeField] private Text _labelText;

        public string NodeId => _nodeId;

        public Button Button => _button;

        public void Bind(string nodeId, Button button, Image image, Text labelText)
        {
            _nodeId = nodeId;
            _button = button;
            _image = image;
            _labelText = labelText;
        }

        public void SetState(string label, Color32 color, bool selectable)
        {
            if (_labelText != null)
            {
                _labelText.text = label;
            }

            if (_image != null)
            {
                _image.color = color;
            }

            if (_button == null)
            {
                return;
            }

            _button.interactable = selectable;
            ColorBlock colors = _button.colors;
            Color nodeColor = color;
            colors.normalColor = nodeColor;
            colors.disabledColor = nodeColor;
            colors.highlightedColor = new Color32(255, 220, 128, 255);
            colors.pressedColor = new Color32(245, 185, 82, 255);
            _button.colors = colors;
        }
    }
}
