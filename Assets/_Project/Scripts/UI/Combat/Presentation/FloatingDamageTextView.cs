using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class FloatingDamageTextView : MonoBehaviour
    {
        [SerializeField] private Text _text = null!;
        [SerializeField] private RectTransform _rectTransform = null!;

        [Header("Normal")]
        [SerializeField] private int _normalFontSize = 50;
        [SerializeField] private Color _normalColor = new Color32(255, 120, 120, 255);

        [Header("Heal")]
        [SerializeField] private int _healFontSize = 50;
        [SerializeField] private Color _healColor = new Color32(120, 255, 160, 255);
        [SerializeField] private string _healPrefix = "+";

        [Header("Critical")]
        [SerializeField] private int _criticalFontSize = 34;
        [SerializeField] private Color _criticalColor = new Color32(255, 210, 64, 255);
        [SerializeField] private string _criticalPrefix = "[CRIT] ";
        [SerializeField] private float _criticalPeakScale = 1.15f;
        [SerializeField] private float _criticalScaleDuration = 0.15f;

        [Header("Motion")]
        [SerializeField] private float _duration = 0.55f;
        [SerializeField] private float _moveDistance = 22f;
#if DOTWEEN
        [SerializeField] private Ease _moveEase = Ease.OutQuad;
        [SerializeField] private Ease _fadeEase = Ease.Linear;
#endif

        private void Reset()
        {
            _text = GetComponent<Text>();
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnDisable()
        {
#if DOTWEEN
            transform.DOKill(true);
#endif
        }

        public async UniTask Play(
            FloatingCombatTextRequest request,
            CombatAnchorKind anchorKind,
            CancellationToken cancellationToken)
        {
            if (request.Amount <= 0)
            {
                return;
            }

            ApplyPresentation(request, anchorKind);

            try
            {
                await PlayMotionAsync(
                    request.Kind == FloatingCombatTextKind.Damage && request.IsCritical,
                    cancellationToken);
            }
            finally
            {
                if (this != null)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void ApplyPresentation(FloatingCombatTextRequest request, CombatAnchorKind anchorKind)
        {
            _ = anchorKind;
            switch (request.Kind)
            {
                case FloatingCombatTextKind.Heal:
                    _text.fontSize = _healFontSize;
                    _text.color = _healColor;
                    _text.text = $"{_healPrefix}{request.Amount}";
                    break;
                case FloatingCombatTextKind.Damage:
                default:
                    bool isCritical = request.IsCritical;
                    _text.fontSize = isCritical ? _criticalFontSize : _normalFontSize;
                    _text.color = isCritical ? _criticalColor : _normalColor;
                    _text.text = isCritical
                        ? $"{_criticalPrefix}-{request.Amount}"
                        : $"-{request.Amount}";
                    break;
            }

            _rectTransform.localScale = Vector3.one;
        }

        private async UniTask PlayMotionAsync(bool isCritical, CancellationToken cancellationToken)
        {
            if (_duration <= 0f)
            {
                return;
            }

#if DOTWEEN
            Vector2 startPosition = _rectTransform.anchoredPosition;
            Color startColor = _text.color;

            Vector2 endPosition = startPosition + new Vector2(0f, _moveDistance);
            Sequence sequence = DOTween.Sequence();
            sequence.Join(
                DOTween.To(
                    () => _rectTransform.anchoredPosition,
                    v => _rectTransform.anchoredPosition = v,
                    endPosition,
                    _duration)
                .SetEase(_moveEase));
            sequence.Join(
                DOTween.To(
                    () => _text.color.a,
                    a => { Color c = _text.color; c.a = a; _text.color = c; },
                    0f,
                    _duration)
                .SetEase(_fadeEase));

            if (isCritical && _criticalPeakScale > 1f && _criticalScaleDuration > 0f)
            {
                sequence.Join(
                    _rectTransform
                        .DOScale(_criticalPeakScale, _criticalScaleDuration)
                        .SetLoops(2, LoopType.Yoyo)
                        .SetEase(Ease.OutQuad));
            }

            sequence.SetLink(gameObject);
            await CombatPresentationTweens.AwaitTweenAsync(sequence, cancellationToken);
#else
            float elapsed = 0f;
            Vector2 startPosition = _rectTransform.anchoredPosition;
            Color startColor = _text.color;

            while (elapsed < _duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_text == null)
                {
                    return;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _duration);
                _rectTransform.anchoredPosition = startPosition + new Vector2(0f, _moveDistance * t);
                _text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
#endif
        }
    }
}
