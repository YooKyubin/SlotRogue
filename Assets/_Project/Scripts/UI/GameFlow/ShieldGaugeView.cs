using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace SlotRogue.UI.GameFlow
{
    public sealed class ShieldGaugeView : MonoBehaviour
    {
        [SerializeField] private Text _shieldText;
        [SerializeField] private Image _shieldImage;

        [SerializeField] private float _gainDuration = 1f;
        [SerializeField] private float _gainStartYOffset = -10f;

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

        public UniTask PlayHitAsync(int consumedAmount, CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public UniTask PlayBreakAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
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

        private static Tween CreateImageAlphaTween(Image image, float targetAlpha, float duration)
        {
            return DOTween.To(
                () => image.color.a,
                alpha => SetImageAlpha(image, alpha),
                targetAlpha,
                duration);
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
    }
}
