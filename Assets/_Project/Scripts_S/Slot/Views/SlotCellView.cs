using SlotRogue.Slot.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.Slot.Views
{
    public sealed class SlotCellView : MonoBehaviour
    {
        public void Bind(Text symbolText)
        {
            _symbolText = symbolText;
        }

        public void SetSymbol(SlotSymbolType symbol)
        {
            if (_symbolText != null)
            {
                _symbolText.text = ToDisplayText(symbol);
            }
        }

        private static string ToDisplayText(SlotSymbolType symbol)
        {
            switch (symbol)
            {
                case SlotSymbolType.Sword:
                    return "Sword";
                case SlotSymbolType.Shield:
                    return "Shield";
                case SlotSymbolType.Heart:
                    return "Heart";
                case SlotSymbolType.Coin:
                    return "Coin";
                case SlotSymbolType.Gem:
                    return "Gem";
                case SlotSymbolType.Skull:
                    return "Skull";
                default:
                    return symbol.ToString();
            }
        }

        [SerializeField] private Text _symbolText;
    }
}
