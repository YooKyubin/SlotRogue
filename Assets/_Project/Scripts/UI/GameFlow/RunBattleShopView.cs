using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 상점 패널 뷰. 오퍼 셀 5개 + 리롤 + 보유 코인만 관리한다.
    /// open/close는 외부 상점 토글 버튼이, 유물 설명은 이 프리팹 밖의 별개 패널이 담당하므로
    /// 여기서는 그 어느 것도 참조하지 않는다.
    /// </summary>
    public sealed class RunBattleShopView : MonoBehaviour
    {
        private const int OfferCount = 5;

        [SerializeField] private GameObject _shopPanel;
        [SerializeField] private ShopArtifactOptionView[] _offerViews;
        [SerializeField] private Button _rerollButton;
        [SerializeField] private TMP_Text _rerollButtonTmpText;
        [SerializeField] private TMP_SpriteAsset _currencySpriteAsset;
        [SerializeField] private Texture2D _rarityFrameSheet;

        private ShopArtifactOptionView[] _subscribedCells;
        private Action[] _cellPurchaseHandlers;
        private Button _subscribedRerollButton;
        private bool _referencesResolved;

        public event Action<int> PurchaseRequested;

        public event Action RerollRequested;

        public event Action<RunBattleRelicShopOfferState> OfferSelected;

        public RectTransform PanelTransform
        {
            get
            {
                EnsureReferences();
                return _shopPanel != null ? _shopPanel.transform as RectTransform : null;
            }
        }

        private void Awake()
        {
            EnsureReferences();
            SubscribeButtons();
        }

        private void OnDestroy()
        {
            UnsubscribeButtons();
        }

        public bool EnsureReferences()
        {
            bool complete = HasRequiredReferences();
            if (!complete)
            {
                Debug.LogError(
                    "[RunBattleShopView] Shop UI references must be wired in the inspector. " +
                    $"Missing: {BuildMissingReferenceSummary()}");
                return false;
            }

            if (!_referencesResolved)
            {
                _referencesResolved = true;
                SubscribeButtons();
            }

            return true;
        }

        private bool HasRequiredReferences()
        {
            return _shopPanel != null &&
                HasEntries(_offerViews, OfferCount) &&
                _rerollButton != null &&
                _rerollButtonTmpText != null &&
                _rarityFrameSheet != null;
        }

        private string BuildMissingReferenceSummary()
        {
            var missing = new List<string>();
            if (_shopPanel == null) missing.Add("Shop Panel");
            if (!HasEntries(_offerViews, OfferCount)) missing.Add("Offer Views");
            if (_rerollButton == null) missing.Add("Reroll Button");
            if (_rerollButtonTmpText == null) missing.Add("Reroll Button Text");
            if (_rarityFrameSheet == null) missing.Add("Rarity Frame Sheet");
            return missing.Count > 0 ? string.Join(", ", missing) : "None";
        }

        private static bool HasEntries<T>(T[] entries, int requiredCount)
            where T : class
        {
            if (entries == null || entries.Length < requiredCount)
            {
                return false;
            }

            for (int index = 0; index < requiredCount; index++)
            {
                if (entries[index] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static T GetAt<T>(T[] entries, int index)
            where T : class
        {
            return entries != null && index >= 0 && index < entries.Length
                ? entries[index]
                : null;
        }

        public void Render(RunBattleScreenState state)
        {
            if (!EnsureReferences())
            {
                return;
            }

            RunBattleRelicShopState shop = state?.RelicShop ?? RunBattleRelicShopState.Empty;
            _shopPanel.SetActive(shop.Visible);
            ApplyCurrencySpriteAsset();
            if (!shop.Visible)
            {
                return;
            }

            for (int index = 0; index < OfferCount; index++)
            {
                RunBattleRelicShopOfferState? offer = index < shop.Offers.Count
                    ? shop.Offers[index]
                    : null;
                RenderOffer(index, offer, shop);
            }

            if (_rerollButton != null)
            {
                _rerollButton.interactable = shop.CanReroll;
                SetButtonLabel(_rerollButtonTmpText, BuildCurrencyLabel(shop.RerollCost));
            }
        }

        private void RenderOffer(
            int index,
            RunBattleRelicShopOfferState? nullableOffer,
            RunBattleRelicShopState shop)
        {
            ShopArtifactOptionView cell = GetAt(_offerViews, index);
            if (cell == null)
            {
                return;
            }

            bool hasOffer = nullableOffer.HasValue &&
                !string.IsNullOrEmpty(nullableOffer.Value.RelicId);
            cell.gameObject.SetActive(hasOffer);
            if (!hasOffer)
            {
                return;
            }

            cell.SetRarityFrameSheet(_rarityFrameSheet);
            cell.Render(nullableOffer.Value, shop.CanUseShop, _currencySpriteAsset);
        }

        private static void SetText(TMP_Text tmpText, string value)
        {
            if (tmpText != null)
            {
                tmpText.text = value ?? string.Empty;
            }
        }

        private void SubscribeButtons()
        {
            SubscribeCells();
            SubscribeRerollButton();
        }

        private void SubscribeCells()
        {
            if (_offerViews == null || _subscribedCells == _offerViews)
            {
                return;
            }

            UnsubscribeCells();
            _subscribedCells = _offerViews;
            _cellPurchaseHandlers = new Action[_subscribedCells.Length];

            for (int index = 0; index < _subscribedCells.Length; index++)
            {
                ShopArtifactOptionView cell = _subscribedCells[index];
                if (cell == null)
                {
                    continue;
                }

                int capturedIndex = index;
                Action purchaseHandler = () => PurchaseRequested?.Invoke(capturedIndex);
                _cellPurchaseHandlers[index] = purchaseHandler;
                cell.PurchaseRequested += purchaseHandler;
                cell.Selected += HandleCellSelected;
            }
        }

        private void HandleCellSelected(RunBattleRelicShopOfferState offer)
        {
            OfferSelected?.Invoke(offer);
        }

        private void SubscribeRerollButton()
        {
            if (_rerollButton == null || _subscribedRerollButton == _rerollButton)
            {
                return;
            }

            if (_subscribedRerollButton != null)
            {
                _subscribedRerollButton.onClick.RemoveListener(HandleRerollClicked);
            }

            _rerollButton.onClick.AddListener(HandleRerollClicked);
            _subscribedRerollButton = _rerollButton;
        }

        private void UnsubscribeButtons()
        {
            UnsubscribeCells();
            if (_subscribedRerollButton != null)
            {
                _subscribedRerollButton.onClick.RemoveListener(HandleRerollClicked);
                _subscribedRerollButton = null;
            }
        }

        private void UnsubscribeCells()
        {
            if (_subscribedCells == null)
            {
                _cellPurchaseHandlers = null;
                return;
            }

            for (int index = 0; index < _subscribedCells.Length; index++)
            {
                ShopArtifactOptionView cell = _subscribedCells[index];
                if (cell == null)
                {
                    continue;
                }

                if (_cellPurchaseHandlers != null &&
                    index < _cellPurchaseHandlers.Length &&
                    _cellPurchaseHandlers[index] != null)
                {
                    cell.PurchaseRequested -= _cellPurchaseHandlers[index];
                }

                cell.Selected -= HandleCellSelected;
            }

            _subscribedCells = null;
            _cellPurchaseHandlers = null;
        }

        private void HandleRerollClicked()
        {
            RerollRequested?.Invoke();
        }

        private string BuildCurrencyLabel(int amount)
        {
            return RunCurrencyText.FormatAmount(amount, _currencySpriteAsset);
        }

        private void SetButtonLabel(TMP_Text tmpText, string value)
        {
            RunCurrencyText.ApplySpriteAsset(tmpText, _currencySpriteAsset);
            SetText(tmpText, value);
        }

        private void ApplyCurrencySpriteAsset()
        {
            if (_currencySpriteAsset == null)
            {
                return;
            }

            RunCurrencyText.ApplySpriteAsset(_rerollButtonTmpText, _currencySpriteAsset);
        }
    }

    internal static class RunCurrencyText
    {
        private const string SpriteTag = "<sprite index=0>";

        public static string FormatAmount(int amount, TMP_SpriteAsset spriteAsset)
        {
            int safeAmount = Mathf.Max(0, amount);
            return spriteAsset != null
                ? $"{SpriteTag} {safeAmount}"
                : safeAmount.ToString();
        }

        public static void ApplySpriteAsset(TMP_Text text, TMP_SpriteAsset spriteAsset)
        {
            if (text == null || spriteAsset == null || text.spriteAsset == spriteAsset)
            {
                return;
            }

            text.spriteAsset = spriteAsset;
        }
    }
}
