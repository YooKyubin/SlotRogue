using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class FinalResultView : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image _panelImage;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _summaryText;
        [SerializeField] private Color _panelColor = new Color(0.1f, 0.15f, 0.2f, 0.98f);
        [SerializeField] private float _introDuration = 0.14f;
        [SerializeField] private float _holdDuration = 0.48f;

        public void Bind(
            RectTransform panel,
            Image panelImage,
            Text titleText,
            Text summaryText)
        {
            _panel = panel;
            _panelImage = panelImage;
            _titleText = titleText;
            _summaryText = summaryText;
        }

        public IEnumerator Play(SlotFinalPresentationResult result, Func<bool> shouldSkip)
        {
            if (result == null)
            {
                yield break;
            }

            EnsurePanel();
            SetText(_titleText, "FINAL RESULT");
            SetText(_summaryText, result.SummaryText);

            if (_panelImage != null)
            {
                _panelImage.color = _panelColor;
            }

            gameObject.SetActive(true);
            yield return PlayScaleTween(new Vector3(0.92f, 0.92f, 1f), Vector3.one, _introDuration, Ease.OutCubic, shouldSkip);
            yield return WaitOrSkip(_holdDuration, shouldSkip);

            HideImmediate();
        }

        public void HideImmediate()
        {
            EnsurePanel();
            KillActiveTween();

            if (_panel != null)
            {
                _panel.localScale = Vector3.one;
            }

            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            KillActiveTween();
        }

        private void EnsurePanel()
        {
            if (_panel == null)
            {
                _panel = transform as RectTransform;
            }
        }

        private IEnumerator PlayScaleTween(Vector3 from, Vector3 to, float duration, Ease ease, Func<bool> shouldSkip)
        {
            EnsurePanel();

            if (_panel == null)
            {
                yield break;
            }

            if (duration <= 0f || IsSkipped(shouldSkip))
            {
                _panel.localScale = to;
                yield break;
            }

            _panel.localScale = from;
            yield return PlayTween(_panel.DOScale(to, duration).SetEase(ease).SetUpdate(true), shouldSkip);
        }

        private IEnumerator WaitOrSkip(float duration, Func<bool> shouldSkip)
        {
            if (duration <= 0f || IsSkipped(shouldSkip))
            {
                yield break;
            }

            yield return PlayTween(DOVirtual.DelayedCall(duration, NoOp).SetUpdate(true), shouldSkip);
        }

        private IEnumerator PlayTween(Tween tween, Func<bool> shouldSkip)
        {
            if (tween == null)
            {
                yield break;
            }

            _activeTween = tween;

            while (tween.IsActive() && !tween.IsComplete() && !IsSkipped(shouldSkip))
            {
                yield return null;
            }

            if (tween.IsActive() && !tween.IsComplete())
            {
                tween.Complete();
            }

            if (_activeTween == tween)
            {
                _activeTween = null;
            }
        }

        private static bool IsSkipped(Func<bool> shouldSkip)
        {
            return shouldSkip != null && shouldSkip();
        }

        private void KillActiveTween()
        {
            if (_activeTween != null && _activeTween.IsActive())
            {
                _activeTween.Kill();
            }

            _activeTween = null;

            if (_panel != null)
            {
                _panel.DOKill();
            }
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void NoOp()
        {
        }

        private Tween _activeTween;
    }
}
