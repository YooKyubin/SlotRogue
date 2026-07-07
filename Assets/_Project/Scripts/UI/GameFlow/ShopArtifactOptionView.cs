using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Relics.Pool;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 상점 오퍼 한 칸(행)을 스스로 관리하는 셀이다. 아이콘(비동기 로딩)·등급 프레임·가격(별 스프라이트)·
    /// 구매 버튼을 소유하고, 선택/구매 입력만 이벤트로 노출한다. RunBattleShopView는 오퍼를 넘겨 Render만 시킨다.
    /// </summary>
public sealed class ShopArtifactOptionView : MonoBehaviour
{
        private const int RarityFrameSpriteCount = 6;
        private const float RarityFramePixelsPerUnit = 100f;

        [SerializeField] private Button _button;
        [SerializeField] private Image _rarityFrameImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _purchaseButton;
        [SerializeField] private TMP_Text _priceTmpText;

        private Texture2D _rarityFrameSheet;
        private Sprite[] _rarityFrameSprites;
        private AddressableSpriteProvider _iconProvider;
        private CancellationTokenSource _iconCts;
        private int _iconVersion;
        private string _iconKey = string.Empty;
        private bool _subscribed;
        private bool _missingReferenceErrorLogged;
        private RunBattleRelicShopOfferState _offer;

        public event Action<RunBattleRelicShopOfferState> Selected;

        public event Action PurchaseRequested;

        public Button Button => _button;

        private void Awake()
        {
            ValidateRequiredReferences();
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _iconCts?.Cancel();
            _iconCts?.Dispose();
            _iconCts = null;
            DestroyRarityFrameSprites();
            _iconProvider?.Dispose();
            _iconProvider = null;
        }

        public void SetRarityFrameSheet(Texture2D sheet)
        {
            if (_rarityFrameSheet == sheet)
            {
                return;
            }

            _rarityFrameSheet = sheet;
            DestroyRarityFrameSprites();
        }

        public void SetRarity(RewardRarity rarity)
        {
            if (_rarityFrameImage == null)
            {
                return;
            }

            Sprite sprite = SpriteFor(rarity);
            _rarityFrameImage.sprite = sprite;
            _rarityFrameImage.enabled = sprite != null;
        }

        /// <summary>이 셀이 자기 오퍼를 렌더한다: 아이콘·등급·가격·구매 상호작용.</summary>
        public void Render(
            RunBattleRelicShopOfferState offer,
            bool canBuy,
            TMP_SpriteAsset currencySprite)
        {
            Subscribe();
            _offer = offer;
            SetRarity(offer.Rarity);
            ApplyIcon(offer.IconKey);

            if (_priceTmpText != null)
            {
                RunCurrencyText.ApplySpriteAsset(_priceTmpText, currencySprite);
                _priceTmpText.text = offer.Purchased
                    ? "구매완료"
                    : RunCurrencyText.FormatAmount(offer.Cost, currencySprite);
            }

            if (_purchaseButton != null)
            {
                _purchaseButton.gameObject.SetActive(true);
                _purchaseButton.interactable =
                    canBuy && !offer.Purchased && offer.CanPurchase;
            }
        }

        private void ApplyIcon(string key)
        {
            key ??= string.Empty;
            if (_iconImage == null)
            {
                return;
            }

            // 같은 아이콘이면 재로딩을 생략한다(오퍼가 그대로일 때 깜빡임 방지).
            if (string.Equals(_iconKey, key, StringComparison.Ordinal) && _iconImage.sprite != null)
            {
                return;
            }

            _iconKey = key;
            _iconImage.sprite = null;
            _iconImage.enabled = false;
            _iconVersion++;
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            _iconCts ??= new CancellationTokenSource();
            LoadIconAsync(key, _iconVersion, _iconCts.Token).Forget();
        }

        private async UniTaskVoid LoadIconAsync(string key, int version, CancellationToken cancellationToken)
        {
            _iconProvider ??= new AddressableSpriteProvider(RelicIconKeys.Default);
            Sprite sprite;
            try
            {
                sprite = await _iconProvider.LoadAsync(key, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (version != _iconVersion || _iconImage == null)
            {
                return;
            }

            _iconImage.sprite = sprite;
            _iconImage.enabled = sprite != null;
            _iconImage.preserveAspect = true;
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            if (_button != null)
            {
                _button.onClick.AddListener(HandleSelectClicked);
            }

            if (_purchaseButton != null)
            {
                _purchaseButton.onClick.AddListener(HandlePurchaseClicked);
            }

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleSelectClicked);
            }

            if (_purchaseButton != null)
            {
                _purchaseButton.onClick.RemoveListener(HandlePurchaseClicked);
            }

            _subscribed = false;
        }

        private void HandleSelectClicked()
        {
            Selected?.Invoke(_offer);
        }

        private void HandlePurchaseClicked()
        {
            PurchaseRequested?.Invoke();
        }

        private void ValidateRequiredReferences()
        {
            // 등급 프레임만 필수. select 버튼/아이콘/가격/구매 버튼은 미배선이어도 각자 null-guard된다.
            if (_missingReferenceErrorLogged || _rarityFrameImage != null)
            {
                return;
            }

            _missingReferenceErrorLogged = true;
            Debug.LogError(
                "[ShopArtifactOptionView] Rarity frame image must be wired in the inspector.");
        }

        private Sprite SpriteFor(RewardRarity rarity)
        {
            if (!EnsureRarityFrameSprites())
            {
                return null;
            }

            return _rarityFrameSprites[IndexFor(rarity)];
        }

        private bool EnsureRarityFrameSprites()
        {
            if (_rarityFrameSprites != null)
            {
                return true;
            }

            if (_rarityFrameSheet == null)
            {
                return false;
            }

            int spriteWidth = _rarityFrameSheet.width / RarityFrameSpriteCount;
            int spriteHeight = _rarityFrameSheet.height;
            if (spriteWidth <= 0 || spriteHeight <= 0)
            {
                return false;
            }

            _rarityFrameSprites = new Sprite[RarityFrameSpriteCount];
            for (int index = 0; index < RarityFrameSpriteCount; index++)
            {
                var rect = new Rect(spriteWidth * index, 0, spriteWidth, spriteHeight);
                Sprite sprite = Sprite.Create(
                    _rarityFrameSheet,
                    rect,
                    new Vector2(0.5f, 0.5f),
                    RarityFramePixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
                sprite.name = $"{_rarityFrameSheet.name}_{index}";
                _rarityFrameSprites[index] = sprite;
            }

            return true;
        }

        private void DestroyRarityFrameSprites()
        {
            if (_rarityFrameSprites == null)
            {
                return;
            }

            for (int index = 0; index < _rarityFrameSprites.Length; index++)
            {
                if (_rarityFrameSprites[index] != null)
                {
                    Destroy(_rarityFrameSprites[index]);
                }
            }

            _rarityFrameSprites = null;
        }

        private static int IndexFor(RewardRarity rarity)
        {
            return rarity switch
            {
                RewardRarity.Uncommon => 1,
                RewardRarity.Rare => 2,
                RewardRarity.Epic => 3,
                RewardRarity.Legendary => 4,
                RewardRarity.Curse => 5,
                _ => 0,
            };
        }
    }
}
