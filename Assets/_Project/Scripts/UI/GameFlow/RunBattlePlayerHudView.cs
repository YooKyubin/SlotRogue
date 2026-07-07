using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattlePlayerHudView : MonoBehaviour
    {
        private const float FillTweenDuration = 0.35f;

        [SerializeField] private Text _hudText;
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private TMP_Text _shieldText;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _shieldFill;
        [SerializeField] private RectTransform _hpGaugeRoot;
        [SerializeField] private RectTransform _shieldGaugeRoot;
        [SerializeField] private TMP_Text _starText;
        [SerializeField] private TMP_SpriteAsset _currencySpriteAsset;

        private Vector2 _hpGaugeDefaultPosition;
        private float _hpSingleRowYOffset;
        private bool _layoutCached;
        private bool _hpFillRendered;
        private bool _shieldFillRendered;
        private bool _missingReferenceErrorLogged;
#if DOTWEEN
        private Tween _hpFillTween;
        private Tween _shieldFillTween;
#endif

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnDisable()
        {
#if DOTWEEN
            _hpFillTween?.Kill();
            _shieldFillTween?.Kill();
#endif
        }

        public void Bind(Text hudText, Image hpFill, Image shieldFill)
        {
            _hudText = hudText;
            _hpFill = hpFill;
            _shieldFill = shieldFill;
        }

        public void Render(RunBattleScreenState state)
        {
            EnsureReferences();
            bool shopVisible = state.RelicShop?.Visible == true;
            if (shopVisible)
            {
                ApplyShopCurrencyVisibility(state.RelicShop.RunCoins);
                return;
            }

            SetStarTextVisible(false);
            ApplyShieldVisibility(state.PlayerShield > 0);
            SetText(_hudText, state.PlayerHudText);
            SetText(_hpText, state.PlayerHp.ToString());
            SetText(_shieldText, state.PlayerShield.ToString());
#if DOTWEEN
            SetBarFill(_hpFill, state.PlayerHp, state.PlayerMaxHp, ref _hpFillRendered, ref _hpFillTween);
            SetBarFill(_shieldFill, state.PlayerShield, state.PlayerShieldMax, ref _shieldFillRendered, ref _shieldFillTween);
#else
            SetBarFill(_hpFill, state.PlayerHp, state.PlayerMaxHp, ref _hpFillRendered);
            SetBarFill(_shieldFill, state.PlayerShield, state.PlayerShieldMax, ref _shieldFillRendered);
#endif
        }

        public UniTask WaitHpFillAsync(CancellationToken cancellationToken)
        {
#if DOTWEEN
            return SlotRogue.UI.Combat.Presentation.CombatPresentationTweens.AwaitTweenAsync(
                _hpFillTween,
                cancellationToken);
#else
            return UniTask.CompletedTask;
#endif
        }

        private void EnsureReferences()
        {
            if (_missingReferenceErrorLogged ||
                (_hpText != null &&
                 _shieldText != null &&
                 _hpFill != null &&
                 _shieldFill != null &&
                 _hpGaugeRoot != null &&
                 _shieldGaugeRoot != null &&
                 _starText != null))
            {
                return;
            }

            _missingReferenceErrorLogged = true;
            Debug.LogError(
                "[RunBattlePlayerHudView] Player HUD references must be wired in the inspector. " +
                $"Missing: {BuildMissingReferenceSummary()}");
        }

        private string BuildMissingReferenceSummary()
        {
            var missing = new System.Collections.Generic.List<string>();
            if (_hpText == null) missing.Add("HP Text");
            if (_shieldText == null) missing.Add("Shield Text");
            if (_hpFill == null) missing.Add("HP Fill");
            if (_shieldFill == null) missing.Add("Shield Fill");
            if (_hpGaugeRoot == null) missing.Add("HP Gauge Root");
            if (_shieldGaugeRoot == null) missing.Add("Shield Gauge Root");
            if (_starText == null) missing.Add("Star Text");
            return missing.Count > 0 ? string.Join(", ", missing) : "None";
        }

        private void ApplyShieldVisibility(bool hasShield)
        {
            CacheLayoutIfNeeded();

            if (_hpGaugeRoot != null)
            {
                _hpGaugeRoot.gameObject.SetActive(true);
            }

            if (_shieldGaugeRoot != null)
            {
                _shieldGaugeRoot.gameObject.SetActive(hasShield);
            }

            if (_hpGaugeRoot != null)
            {
                Vector2 offset = hasShield ? Vector2.zero : new Vector2(0f, _hpSingleRowYOffset);
                _hpGaugeRoot.anchoredPosition = _hpGaugeDefaultPosition + offset;
            }
        }

        private void ApplyShopCurrencyVisibility(int runCoins)
        {
            if (_hpGaugeRoot != null)
            {
                _hpGaugeRoot.gameObject.SetActive(false);
            }

            if (_shieldGaugeRoot != null)
            {
                _shieldGaugeRoot.gameObject.SetActive(false);
            }

            SetStarTextVisible(true);
            if (_starText != null)
            {
                RunCurrencyText.ApplySpriteAsset(_starText, _currencySpriteAsset);
                _starText.text = RunCurrencyText.FormatAmount(runCoins, _currencySpriteAsset);
            }
        }

        private void SetStarTextVisible(bool visible)
        {
            if (_starText != null)
            {
                _starText.gameObject.SetActive(visible);
            }
        }

        private void CacheLayoutIfNeeded()
        {
            if (_layoutCached)
            {
                return;
            }

            _hpGaugeDefaultPosition = _hpGaugeRoot != null
                ? _hpGaugeRoot.anchoredPosition
                : Vector2.zero;
            _hpSingleRowYOffset = _hpGaugeRoot != null
                ? -ComputeAverageChildY(_hpGaugeRoot)
                : 0f;

            if (Mathf.Abs(_hpSingleRowYOffset) <= 0.001f)
            {
                _hpSingleRowYOffset = 6f;
            }

            _layoutCached = true;
        }

        private static float ComputeAverageChildY(RectTransform root)
        {
            if (root == null || root.childCount <= 0)
            {
                return 0f;
            }

            float sum = 0f;
            int count = 0;
            for (int index = 0; index < root.childCount; index++)
            {
                if (root.GetChild(index) is RectTransform child)
                {
                    sum += child.anchoredPosition.y;
                    count++;
                }
            }

            return count > 0 ? sum / count : 0f;
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

#if DOTWEEN
        private void SetBarFill(
            Image fill,
            int current,
            int max,
            ref bool hasRendered,
            ref Tween tween)
        {
            if (fill == null)
            {
                return;
            }

            RectTransform parent = fill.rectTransform.parent as RectTransform;
            float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            fill.type = Image.Type.Filled;
            fill.preserveAspect = false;

            if (parent != null && parent.sizeDelta.y > parent.sizeDelta.x * 1.4f)
            {
                fill.fillMethod = Image.FillMethod.Vertical;
                fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            }
            else
            {
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            }

            float targetRatio = ratio;
            if (!hasRendered)
            {
                fill.fillAmount = targetRatio;
                hasRendered = true;
                return;
            }

            tween?.Kill();
            tween = DOTween.To(
                    () => fill.fillAmount,
                    value => fill.fillAmount = value,
                    targetRatio,
                    FillTweenDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }
#else
        private static void SetBarFill(
            Image fill,
            int current,
            int max,
            ref bool hasRendered)
        {
            if (fill == null)
            {
                return;
            }

            RectTransform parent = fill.rectTransform.parent as RectTransform;
            float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            fill.type = Image.Type.Filled;
            fill.fillAmount = ratio;
            fill.preserveAspect = false;
            hasRendered = true;

            if (parent != null && parent.sizeDelta.y > parent.sizeDelta.x * 1.4f)
            {
                fill.fillMethod = Image.FillMethod.Vertical;
                fill.fillOrigin = (int)Image.OriginVertical.Bottom;
                return;
            }

            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
#endif
    }
}
