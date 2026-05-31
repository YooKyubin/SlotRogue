using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunMapEdgeView : MonoBehaviour
    {
        [SerializeField] private string _fromNodeId;
        [SerializeField] private string _toNodeId;
        [SerializeField] private Image _image;

        public string FromNodeId => _fromNodeId;

        public string ToNodeId => _toNodeId;

        public void Bind(string fromNodeId, string toNodeId, Image image)
        {
            _fromNodeId = fromNodeId;
            _toNodeId = toNodeId;
            _image = image;
        }

        public void SetColor(Color32 color)
        {
            if (_image != null)
            {
                _image.color = color;
            }
        }
    }
}
