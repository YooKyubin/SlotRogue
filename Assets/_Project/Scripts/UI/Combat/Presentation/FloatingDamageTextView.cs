using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class FloatingDamageTextView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text = null!;
        [SerializeField] private RectTransform _rectTransform = null!;

        [Header("Normal")]
        [SerializeField] private int _normalFontSize = 50;
        [SerializeField] private Color _normalColor = new Color32(255, 120, 120, 255);

        [Header("Damage Font Scaling")]
        [SerializeField] private bool _damageScaledFontSizeEnabled = true;
        [SerializeField] private int _damageFontScaleMinAmount = 1;
        [SerializeField] private int _damageFontScaleMaxAmount = 50;
        [SerializeField] private float _damageFontScaleMinSize = 50f;
        [SerializeField] private float _damageFontScaleMaxSize = 86f;

        [Header("Heal")]
        [SerializeField] private int _healFontSize = 50;
        [SerializeField] private Color _healColor = new Color32(120, 255, 160, 255);
        [SerializeField] private string _healPrefix = "+";

        [Header("Motion")]
        [SerializeField] private float _duration = 0.55f;
        [SerializeField] private float _moveDistance = 22f;
        [SerializeField] private Ease _moveEase = Ease.OutQuad;
        [SerializeField] private Ease _fadeEase = Ease.Linear;

        private void Reset()
        {
            _text = GetComponent<TMP_Text>();
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnDisable()
        {
            transform.DOKill(true);
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
                await PlayMotionAsync(cancellationToken);
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
                    _text.fontSize = ResolveDamageFontSize(request);
                    _text.color = _normalColor;
                    _text.text = $"-{request.Amount}";
                    break;
            }

            _rectTransform.localScale = Vector3.one;
        }

        private float ResolveDamageFontSize(FloatingCombatTextRequest request)
        {
            if (!_damageScaledFontSizeEnabled || !request.UseDamageScaledFontSize)
            {
                return _normalFontSize;
            }

            if (_damageFontScaleMaxAmount <= _damageFontScaleMinAmount)
            {
                return Mathf.Max(_damageFontScaleMinSize, _damageFontScaleMaxSize);
            }

            float t = Mathf.InverseLerp(
                _damageFontScaleMinAmount,
                _damageFontScaleMaxAmount,
                request.Amount);
            return Mathf.Lerp(_damageFontScaleMinSize, _damageFontScaleMaxSize, t);
        }

        private async UniTask PlayMotionAsync(CancellationToken cancellationToken)
        {
            if (_duration <= 0f)
            {
                return;
            }

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

            sequence.SetLink(gameObject);
            await CombatPresentationTweens.AwaitTweenAsync(sequence, cancellationToken);
        }
    }
}
