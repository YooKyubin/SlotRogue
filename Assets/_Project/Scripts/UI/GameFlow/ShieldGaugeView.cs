using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace SlotRogue.UI.GameFlow
{
    public sealed class ShieldGaugeView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Text _shieldText;
        [SerializeField] private Image _shieldImage;

        [Header("Gain / Expire")]
        [SerializeField] private float _gainDuration = 1f;
        [SerializeField] private float _gainStartYOffset = -10f;

        [Header("Hit")]
        [SerializeField] private float _hitDuration = 0.25f;
        [SerializeField] private float _hitShakeDistance = 8f;
        [SerializeField] private int _hitShakeCount = 4;
        [SerializeField] private float _hitTextScaleMultiplier = 1.25f;
        [SerializeField] private Color _hitTextColor = Color.red;

        [Header("Break")]
        [SerializeField] private float _breakDuration = 0.18f;
        [SerializeField] private float _breakScaleMultiplier = 1.18f;

        private Tween _shieldTween;

        private void OnDisable()
        {
            _shieldTween?.Kill();
        }

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

        public async UniTask PlayGainAsync(int amount, CancellationToken cancellationToken)
        {
            Image shieldImage = _shieldImage;
            if (shieldImage == null)
            {
                return;
            }

            gameObject.SetActive(true);

            RectTransform imageTransform = shieldImage.rectTransform;
            Vector2 targetPosition = imageTransform.anchoredPosition;
            Vector2 startPosition = targetPosition + new Vector2(0f, _gainStartYOffset);
            Color targetColor = shieldImage.color;
            targetColor.a = 1f;
            Color startColor = targetColor;
            startColor.a = 0f;

            imageTransform.anchoredPosition = startPosition;
            shieldImage.color = startColor;

            _shieldTween?.Kill();

            Sequence sequence = CreateGainSequence(
                shieldImage,
                imageTransform,
                targetPosition,
                targetColor.a);

            _shieldTween = sequence;

            await SlotRogue.UI.Combat.Presentation.CombatPresentationTweens.AwaitTweenAsync(sequence, cancellationToken);

            if (shieldImage != null && imageTransform != null)
            {
                shieldImage.color = targetColor;
                imageTransform.anchoredPosition = targetPosition;
            }
        }

        public async UniTask PlayHitAsync(int consumedAmount, CancellationToken cancellationToken)
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            Image shieldImage = _shieldImage;
            if (shieldImage == null)
            {
                return;
            }

            RectTransform imageTransform = shieldImage.rectTransform;
            Vector2 targetPosition = imageTransform.anchoredPosition;

            Text shieldText = _shieldText;
            RectTransform textTransform = shieldText != null ? shieldText.rectTransform : null;
            Color targetTextColor = shieldText != null ? shieldText.color : Color.white;
            Vector3 targetTextScale = textTransform != null ? textTransform.localScale : Vector3.one;

            if (shieldText != null)
            {
                UpdateShieldTextAfterHit(shieldText, consumedAmount);
                shieldText.color = _hitTextColor;
            }

            if (textTransform != null)
            {
                textTransform.localScale = targetTextScale * _hitTextScaleMultiplier;
            }

            _shieldTween?.Kill();

            Sequence sequence = CreateHitSequence(
                imageTransform,
                targetPosition,
                shieldText,
                textTransform,
                targetTextColor,
                targetTextScale);

            _shieldTween = sequence;

            await SlotRogue.UI.Combat.Presentation.CombatPresentationTweens.AwaitTweenAsync(sequence, cancellationToken);

            if (shieldImage != null && imageTransform != null)
            {
                imageTransform.anchoredPosition = targetPosition;
            }

            if (shieldText != null)
            {
                shieldText.color = targetTextColor;
            }

            if (textTransform != null)
            {
                textTransform.localScale = targetTextScale;
            }
        }

        public async UniTask PlayBreakAsync(CancellationToken cancellationToken)
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            Image shieldImage = _shieldImage;
            if (shieldImage == null)
            {
                return;
            }

            RectTransform imageTransform = shieldImage.rectTransform;
            Vector3 targetScale = imageTransform.localScale;
            Vector3 endScale = targetScale * _breakScaleMultiplier;
            Color targetColor = shieldImage.color;
            targetColor.a = 0f;

            _shieldTween?.Kill();

            Sequence sequence = CreateBreakSequence(
                shieldImage,
                imageTransform,
                endScale,
                targetColor.a);

            _shieldTween = sequence;

            await SlotRogue.UI.Combat.Presentation.CombatPresentationTweens.AwaitTweenAsync(sequence, cancellationToken);

            if (shieldImage != null && imageTransform != null)
            {
                shieldImage.color = targetColor;
                imageTransform.localScale = targetScale;
            }

            gameObject.SetActive(false);
        }

        public async UniTask PlayExpireAsync(CancellationToken cancellationToken)
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            Image shieldImage = _shieldImage;
            if (shieldImage == null)
            {
                return;
            }

            RectTransform imageTransform = shieldImage.rectTransform;
            Vector2 targetPosition = imageTransform.anchoredPosition;
            Vector2 endPosition = targetPosition + new Vector2(0f, _gainStartYOffset);
            Color targetColor = shieldImage.color;
            targetColor.a = 0f;

            _shieldTween?.Kill();

            Sequence sequence = CreateExpireSequence(
                shieldImage,
                imageTransform,
                endPosition,
                targetColor.a);

            _shieldTween = sequence;

            await SlotRogue.UI.Combat.Presentation.CombatPresentationTweens.AwaitTweenAsync(sequence, cancellationToken);

            if (shieldImage != null && imageTransform != null)
            {
                shieldImage.color = targetColor;
                imageTransform.anchoredPosition = targetPosition;
            }

            gameObject.SetActive(false);
        }

        private Sequence CreateGainSequence(
            Image shieldImage,
            RectTransform imageTransform,
            Vector2 targetPosition,
            float targetAlpha)
        {
            Tween fadeTween = CreateImageAlphaTween(shieldImage, targetAlpha, _gainDuration);
            Tween moveTween = CreateAnchoredPositionTween(imageTransform, targetPosition, _gainDuration);

            return DOTween.Sequence()
                .Join(fadeTween)
                .Join(moveTween)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }

        private Sequence CreateExpireSequence(
            Image shieldImage,
            RectTransform imageTransform,
            Vector2 endPosition,
            float targetAlpha)
        {
            Tween fadeTween = CreateImageAlphaTween(shieldImage, targetAlpha, _gainDuration);
            Tween moveTween = CreateAnchoredPositionTween(imageTransform, endPosition, _gainDuration);

            return DOTween.Sequence()
                .Join(fadeTween)
                .Join(moveTween)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject);
        }

        private Sequence CreateHitSequence(
            RectTransform imageTransform,
            Vector2 targetPosition,
            Text shieldText,
            RectTransform textTransform,
            Color targetTextColor,
            Vector3 targetTextScale)
        {
            Tween shakeTween = CreateShakeTween(
                imageTransform,
                targetPosition,
                _hitShakeDistance,
                _hitShakeCount,
                _hitDuration);

            Sequence sequence = DOTween.Sequence()
                .Join(shakeTween)
                .SetLink(gameObject);

            if (shieldText != null)
            {
                sequence.Join(CreateTextColorTween(shieldText, targetTextColor, _hitDuration));
            }

            if (textTransform != null)
            {
                sequence.Join(CreateLocalScaleTween(textTransform, targetTextScale, _hitDuration));
            }

            return sequence;
        }

        private Sequence CreateBreakSequence(
            Image shieldImage,
            RectTransform imageTransform,
            Vector3 endScale,
            float targetAlpha)
        {
            Tween fadeTween = CreateImageAlphaTween(shieldImage, targetAlpha, _breakDuration);
            Tween scaleTween = CreateLocalScaleTween(imageTransform, endScale, _breakDuration);

            return DOTween.Sequence()
                .Join(fadeTween)
                .Join(scaleTween)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }

        private static Tween CreateImageAlphaTween(Image image, float targetAlpha, float duration)
        {
            return DOTween.To(
                () => image.color.a,
                alpha => SetImageAlpha(image, alpha),
                targetAlpha,
                duration);
        }

        private static Tween CreateTextColorTween(Text text, Color targetColor, float duration)
        {
            return DOTween.To(
                () => text.color,
                color => text.color = color,
                targetColor,
                duration);
        }

        private static Tween CreateShakeTween(
            RectTransform target,
            Vector2 targetPosition,
            float distance,
            int shakeCount,
            float duration)
        {
            int clampedShakeCount = Mathf.Max(1, shakeCount);
            float clampedDuration = Mathf.Max(0.01f, duration);

            return DOTween.To(
                () => 0f,
                progress =>
                {
                    float damping = 1f - progress;
                    float offset = Mathf.Sin(progress * clampedShakeCount * Mathf.PI * 2f) * distance * damping;
                    target.anchoredPosition = targetPosition + new Vector2(offset, 0f);
                },
                1f,
                clampedDuration);
        }

        private static void UpdateShieldTextAfterHit(Text shieldText, int consumedAmount)
        {
            if (!int.TryParse(shieldText.text, out int currentShield))
            {
                return;
            }

            int nextShield = Mathf.Max(0, currentShield - consumedAmount);
            shieldText.text = nextShield.ToString();
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private static Tween CreateAnchoredPositionTween(RectTransform target, Vector2 targetPosition, float duration)
        {
            return DOTween.To(
                () => target.anchoredPosition,
                position => target.anchoredPosition = position,
                targetPosition,
                duration);
        }

        private static Tween CreateLocalScaleTween(RectTransform target, Vector3 targetScale, float duration)
        {
            return DOTween.To(
                () => target.localScale,
                scale => target.localScale = scale,
                targetScale,
                duration);
        }
    }
}
