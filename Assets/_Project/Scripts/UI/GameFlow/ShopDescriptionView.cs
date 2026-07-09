using TMPro;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 상점 유물 설명 팝업(정보 전용). UI_popUpCanvas 하위의 씬 오브젝트에 붙는다.
    /// 선택된 오퍼의 등급/이름/효과/가격만 표시하고 구매는 하지 않는다(구매는 셀의 가격 버튼).
    /// 표시/숨김은 BattleScreenController가 셀 선택·리롤·구매·상점 닫힘에 맞춰 호출한다.
    /// </summary>
    public sealed class ShopDescriptionView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _tierText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private TMP_SpriteAsset _currencySpriteAsset;

        private readonly SlotSymbolTmpSpriteAssetBinder _symbolSpriteAssetBinder = new();

        private void Awake()
        {
            _symbolSpriteAssetBinder.ApplyTo(_bodyText);
            Hide();
        }

        private void OnDestroy()
        {
            _symbolSpriteAssetBinder.Dispose();
        }

        public void Show(RunBattleRelicShopOfferState offer)
        {
            if (string.IsNullOrEmpty(offer.RelicId))
            {
                Hide();
                return;
            }

            SetPanelActive(true);
            transform.SetAsLastSibling();
            SetText(_tierText, offer.Grade);
            SetText(_nameText, offer.Name);
            _symbolSpriteAssetBinder.ApplyTo(_bodyText);
            SetText(_bodyText, offer.Description);

            if (_priceText != null)
            {
                RunCurrencyText.ApplySpriteAsset(_priceText, _currencySpriteAsset);
                _priceText.text = offer.Purchased
                    ? "구매완료"
                    : RunCurrencyText.FormatAmount(offer.Cost, _currencySpriteAsset);
            }
        }

        public void Hide()
        {
            SetPanelActive(false);
        }

        private void SetPanelActive(bool active)
        {
            if (_panel != null)
            {
                _panel.SetActive(active);
            }
            else
            {
                gameObject.SetActive(active);
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }
    }
}
