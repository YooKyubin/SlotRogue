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
        private const string PlayerHpFillSlotId = "battle/player-hp-fill";
        private const string PlayerShieldFillSlotId = "battle/player-shield-fill";
        private const string PlayerHpGaugeName = "Player HP Gauge";
        private const string PlayerShieldGaugeName = "Player Shield Gauge";
        private const string PlayerHpFillName = "Player HP Gauge Fill";
        private const string PlayerShieldFillName = "Player Shield Gauge Fill";
        private const string PlayerHpTextName = "Player HP Text";
        private const string PlayerShieldTextName = "Player Shield Text";
        private const float FillTweenDuration = 0.35f;
        private const float HitFeedbackDistance = 10f;
        private const float HitFeedbackDuration = 0.22f;

        [SerializeField] private Text _hudText;
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private TMP_Text _shieldText;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _shieldFill;
        [SerializeField] private RectTransform _hpGaugeRoot;
        [SerializeField] private RectTransform _shieldGaugeRoot;

        private Vector2 _hpGaugeDefaultPosition;
        private float _hpSingleRowYOffset;
        private bool _layoutCached;
        private bool _hpFillRendered;
        private bool _shieldFillRendered;
#if DOTWEEN
        private Tween _hpFillTween;
        private Tween _shieldFillTween;
        private Tween _hitFeedbackTween;
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
            _hitFeedbackTween?.Kill();
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

        public UniTask PlayHitFeedbackAsync(CancellationToken cancellationToken)
        {
#if DOTWEEN
            EnsureReferences();
            RectTransform target = _hpGaugeRoot != null
                ? _hpGaugeRoot
                : transform as RectTransform;
            if (target == null)
            {
                return UniTask.CompletedTask;
            }

            Vector2 restingPosition = target.anchoredPosition;
            _hitFeedbackTween?.Kill();
            _hitFeedbackTween = DOVirtual
                .Float(
                    0f,
                    1f,
                    HitFeedbackDuration,
                    progress =>
                    {
                        float damping = 1f - progress;
                        float offset = Mathf.Sin(progress * Mathf.PI * 6f) * HitFeedbackDistance * damping;
                        target.anchoredPosition = restingPosition + new Vector2(offset, 0f);
                    })
                .SetEase(Ease.Linear)
                .SetLink(gameObject)
                .OnComplete(() => target.anchoredPosition = restingPosition)
                .OnKill(() => target.anchoredPosition = restingPosition);
            return SlotRogue.UI.Combat.Presentation.CombatPresentationTweens.AwaitTweenAsync(
                _hitFeedbackTween,
                cancellationToken);
#else
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
#endif
        }

        private void EnsureReferences()
        {
            _hpFill ??= FindImageSlot(PlayerHpFillSlotId, PlayerHpFillName);
            _hpFill ??= FindImageByName(PlayerHpFillName);
            _shieldFill ??= FindImageSlot(PlayerShieldFillSlotId, PlayerShieldFillName);
            _shieldFill ??= FindImageByName(PlayerShieldFillName);
            _hpGaugeRoot ??= FindRectTransform(PlayerHpGaugeName);
            _hpGaugeRoot ??= _hpFill != null ? _hpFill.rectTransform.parent as RectTransform : null;
            _shieldGaugeRoot ??= FindRectTransform(PlayerShieldGaugeName);
            _shieldGaugeRoot ??= _shieldFill != null ? _shieldFill.rectTransform.parent as RectTransform : null;
            _hpText ??= FindTmpText(PlayerHpTextName);
            _shieldText ??= FindTmpText(PlayerShieldTextName);
        }

        private Image FindImageSlot(string slotId, string preferredObjectName)
        {
            Transform searchRoot = transform.root != null ? transform.root : transform;
            GameFlowImageSlot[] slots = searchRoot.GetComponentsInChildren<GameFlowImageSlot>(true);
            Image fallback = null;
            for (int index = 0; index < slots.Length; index++)
            {
                if (slots[index].SlotId == slotId)
                {
                    Image image = slots[index].Image != null
                        ? slots[index].Image
                        : slots[index].GetComponent<Image>();

                    if (image != null && image.name == preferredObjectName)
                    {
                        return image;
                    }

                    fallback ??= image;
                }
            }

            return fallback;
        }

        private Image FindImageByName(string objectName)
        {
            Transform found = FindChild(objectName);
            return found != null ? found.GetComponent<Image>() : null;
        }

        private TMP_Text FindTmpText(string objectName)
        {
            Transform found = FindChild(objectName);
            return found != null ? found.GetComponent<TMP_Text>() : null;
        }

        private RectTransform FindRectTransform(string objectName)
        {
            Transform found = FindChild(objectName);
            return found as RectTransform;
        }

        private Transform FindChild(string objectName)
        {
            Transform searchRoot = transform.root != null ? transform.root : transform;
            return SceneComponentResolver.FindDeepChild(searchRoot, objectName);
        }

        private void ApplyShieldVisibility(bool hasShield)
        {
            CacheLayoutIfNeeded();

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
