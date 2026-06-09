using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class ShieldGaugeView : MonoBehaviour
    {
        [SerializeField] private Text _shieldText;

        public void Bind(Text shieldText)
        {
            _shieldText = shieldText;
        }

        public void Render(int shield)
        {
            gameObject.SetActive(shield > 0);

            if (_shieldText != null)
            {
                _shieldText.text = shield.ToString();
            }
        }

        public void Clear()
        {
            Render(0);
        }
    }
}
