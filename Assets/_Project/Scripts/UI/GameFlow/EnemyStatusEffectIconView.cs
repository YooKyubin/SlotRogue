using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.UI.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyStatusEffectIconView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Text _valueText;
        [SerializeField] private StatusEffectIconSet _iconSet;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float _showSlideDistance = 24f;
        [SerializeField, Min(0f)] private float _showDuration = 0.22f;
        [SerializeField, Min(0f)] private float _activationShakeDuration = 0.28f;
        [SerializeField, Range(0f, 45f)] private float _activationShakeAngle = 12f;

        private Quaternion _restingLocalRotation;
        private Tween _showTween;
        private Tween _activationTween;

        private void Awake()
        {
            _restingLocalRotation = transform.localRotation;
        }

        private void OnDisable()
        {
            _showTween?.Kill();
            _showTween = null;
            _activationTween?.Kill();
            _activationTween = null;
            transform.localRotation = _restingLocalRotation;
            SetAlpha(1f);
        }

        public void Set(StatusEffectViewData status)
        {
            if (_icon == null || _valueText == null || _iconSet == null)
            {
                Debug.LogError(
                    "[EnemyStatusEffectIconView] Icon, value text, and icon set references are required.",
                    this);
                return;
            }

            _icon.sprite = _iconSet.GetIcon(status.Kind);
            _valueText.text = status.ShowValue
                ? status.DisplayValue.ToString()
                : string.Empty;
        }

        public async UniTask ShowAsync(
            StatusEffectViewData status,
            CancellationToken cancellationToken)
        {
            gameObject.SetActive(true);
            Set(status);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken);

            _showTween?.Kill();
            RectTransform rectTransform = (RectTransform)transform;
            Vector2 restingPosition = rectTransform.anchoredPosition;
            rectTransform.anchoredPosition = restingPosition + Vector2.right * _showSlideDistance;
            SetAlpha(0f);

            var sequence = DOTween.Sequence()
                .Join(DOTween.To(
                    () => rectTransform.anchoredPosition,
                    position => rectTransform.anchoredPosition = position,
                    restingPosition,
                    _showDuration))
                .Join(DOTween.To(
                    () => _icon.color.a,
                    SetAlpha,
                    1f,
                    _showDuration))
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
            _showTween = sequence;
            await CombatPresentationTweens.AwaitTweenAsync(_showTween, cancellationToken);
            _showTween = null;
        }

        public UniTask UpdateValueAsync(
            StatusEffectViewData status,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Set(status);
            return UniTask.CompletedTask;
        }

        public async UniTask PlayActivationAsync(CancellationToken cancellationToken)
        {
            _activationTween?.Kill();
            transform.localRotation = _restingLocalRotation;
            _activationTween = transform
                .DOPunchRotation(
                    new Vector3(0f, 0f, _activationShakeAngle),
                    _activationShakeDuration,
                    vibrato: 6,
                    elasticity: 0.5f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
            await CombatPresentationTweens.AwaitTweenAsync(_activationTween, cancellationToken);
            _activationTween = null;
        }

        public UniTask HideAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }

        private void SetAlpha(float alpha)
        {
            Color iconColor = _icon.color;
            iconColor.a = alpha;
            _icon.color = iconColor;

            Color valueColor = _valueText.color;
            valueColor.a = alpha;
            _valueText.color = valueColor;
        }
    }
}
