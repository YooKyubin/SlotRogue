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
        private const float FillTweenDuration = 0.35f;

        [SerializeField] private Text _hudText;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _shieldFill;

        private bool _hpFillRendered;
        private bool _shieldFillRendered;
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
            SetText(_hudText, state.PlayerHudText);
#if DOTWEEN
            SetBarFill(_hpFill, state.PlayerHp, state.PlayerMaxHp, ref _hpFillRendered, ref _hpFillTween);
            SetBarFill(_shieldFill, state.PlayerShield, state.PlayerShieldMax, ref _shieldFillRendered, ref _shieldFillTween);
#else
            SetBarFill(_hpFill, state.PlayerHp, state.PlayerMaxHp, ref _hpFillRendered);
            SetBarFill(_shieldFill, state.PlayerShield, state.PlayerShieldMax, ref _shieldFillRendered);
#endif
        }

        private void EnsureReferences()
        {
            _hpFill ??= FindImageSlot(PlayerHpFillSlotId);
            _shieldFill ??= FindImageSlot(PlayerShieldFillSlotId);
        }

        private Image FindImageSlot(string slotId)
        {
            Transform searchRoot = transform.root != null ? transform.root : transform;
            GameFlowImageSlot[] slots = searchRoot.GetComponentsInChildren<GameFlowImageSlot>(true);
            for (int index = 0; index < slots.Length; index++)
            {
                if (slots[index].SlotId == slotId)
                {
                    return slots[index].Image != null
                        ? slots[index].Image
                        : slots[index].GetComponent<Image>();
                }
            }

            return null;
        }

        private static void SetText(Text text, string value)
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
