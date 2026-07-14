using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using SlotRogue.UI.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyHealthHudView : MonoBehaviour
    {
        private const float ShieldedHpBarOffsetX = 7f;
        private const float ShieldedHpBarMoveDuration = 0.2f;
        private const float HpFillDuration = 0.5f;

        [SerializeField] private Canvas _hudRoot;
        [SerializeField] private Text _hpText;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _hpBarFrame;
        [SerializeField] private RectTransform _hpBarRoot;
        [SerializeField] private ShieldGaugeView _shieldGauge;

        [Header("Shielded HP Bar")]
        [SerializeField] private Sprite _normalHpFillSprite;
        [SerializeField] private Sprite _shieldedHpFillSprite;
        [SerializeField] private Sprite _normalHpBarFrameSprite;
        [SerializeField] private Sprite _shieldedHpBarFrameSprite;

        private float _hpFillMaxWidth;
        private bool _hpFillLayoutInitialized;
        private bool _hpFillRendered;
        private bool _shieldedHpBarLayoutRendered;
        private Tween _hpFillTween;
        private Tween _hpBarRootTween;

        public Canvas HudRoot => _hudRoot;

        public ShieldGaugeView ShieldGauge => _shieldGauge;

        private void OnDisable()
        {
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                KillTweens();
            }

            if (_hudRoot != null)
            {
                _hudRoot.gameObject.SetActive(visible);
            }
        }

        public void PrepareForReuse()
        {
            KillTweens();
        }

        public void SetHpText(string value)
        {
            if (_hpText != null)
            {
                _hpText.text = value;
            }
        }

        public void Render(string hpText, int currentHp, int maxHp, int shield)
        {
            SetHpText(hpText);
            SetHpFill(currentHp, maxHp);
            SetShield(shield);
        }

        public void SetHpFill(int current, int max)
        {
            if (_hpFill == null)
            {
                return;
            }

            RectTransform fillRect = _hpFill.rectTransform;
            InitializeHpFillLayout(fillRect);

            float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            _hpFill.type = Image.Type.Simple;
            _hpFill.preserveAspect = false;
            float targetWidth = _hpFillMaxWidth * ratio;
            if (!_hpFillRendered)
            {
                fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                _hpFillRendered = true;
                return;
            }

            _hpFillTween?.Kill();
            _hpFillTween = DOTween.To(
                    () => fillRect.rect.width,
                    width => fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width),
                    targetWidth,
                    HpFillDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }

        public UniTask WaitHpFillAsync(CancellationToken cancellationToken)
        {
            return UniTask.WhenAll(
                CombatPresentationTweens.AwaitTweenAsync(_hpFillTween, cancellationToken),
                CombatPresentationTweens.AwaitTweenAsync(_hpBarRootTween, cancellationToken));
        }

        public void SetShield(int shield)
        {
            if (_shieldGauge != null)
            {
                _shieldGauge.Render(shield);
            }

            bool shielded = shield > 0;
            ApplyShieldedHealthBarSprites(shielded);
            ApplyShieldedHealthBarLayout(shielded);
        }

        private void InitializeHpFillLayout(RectTransform fillRect)
        {
            if (_hpFillLayoutInitialized)
            {
                return;
            }

            float currentWidth = Mathf.Max(0f, fillRect.rect.width);
            _hpFillMaxWidth = currentWidth > 0f
                ? currentWidth
                : Mathf.Max(0f, fillRect.sizeDelta.x);

            RectTransform parent = fillRect.parent as RectTransform;
            float leftInset = 0f;
            if (parent != null)
            {
                float parentWidth = parent.rect.width;
                float pivotPosition = (parentWidth * fillRect.anchorMin.x) + fillRect.anchoredPosition.x;
                leftInset = pivotPosition - (_hpFillMaxWidth * fillRect.pivot.x);
            }

            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(leftInset, fillRect.anchoredPosition.y);
            _hpFillLayoutInitialized = true;
        }

        private void ApplyShieldedHealthBarSprites(bool shielded)
        {
            CaptureDefaultHealthBarSprites();

            if (_hpFill != null)
            {
                Sprite fillSprite = shielded && _shieldedHpFillSprite != null
                    ? _shieldedHpFillSprite
                    : _normalHpFillSprite;
                _hpFill.sprite = fillSprite;
            }

            if (_hpBarFrame != null)
            {
                Sprite frameSprite = shielded && _shieldedHpBarFrameSprite != null
                    ? _shieldedHpBarFrameSprite
                    : _normalHpBarFrameSprite;
                _hpBarFrame.sprite = frameSprite;
            }
        }

        private void CaptureDefaultHealthBarSprites()
        {
            if (_hpFill != null && _normalHpFillSprite == null)
            {
                _normalHpFillSprite = _hpFill.sprite;
            }

            if (_hpBarFrame != null && _normalHpBarFrameSprite == null)
            {
                _normalHpBarFrameSprite = _hpBarFrame.sprite;
            }
        }

        private void ApplyShieldedHealthBarLayout(bool shielded)
        {
            if (_hpBarRoot == null)
            {
                return;
            }

            float targetX = shielded ? ShieldedHpBarOffsetX : 0f;
            Vector2 targetPosition = new(targetX, _hpBarRoot.anchoredPosition.y);
            if (!_shieldedHpBarLayoutRendered)
            {
                _hpBarRoot.anchoredPosition = targetPosition;
                _shieldedHpBarLayoutRendered = true;
                return;
            }

            _hpBarRootTween?.Kill();
            _hpBarRootTween = DOTween.To(
                    () => _hpBarRoot.anchoredPosition,
                    position => _hpBarRoot.anchoredPosition = position,
                    targetPosition,
                    ShieldedHpBarMoveDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }

        private void KillTweens()
        {
            _hpFillTween?.Kill();
            _hpFillTween = null;
            _hpBarRootTween?.Kill();
            _hpBarRootTween = null;
        }
    }
}
