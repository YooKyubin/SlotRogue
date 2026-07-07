using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class FinalResultDirector : MonoBehaviour
    {
        private const string StatSpriteAssetAddress = "staticon-Sheet-TMP";
        private const int DefenseSpriteIndex = 0;
        private const int AttackSpriteIndex = 1;
        private const int HealSpriteIndex = 4;

        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image _panelImage;
        [SerializeField] private TMP_Text _resultTmpText;
        [SerializeField] private string _statSpriteAssetAddress = StatSpriteAssetAddress;

        [Header("Pop Presentation")]
        [Tooltip("Slides from the authored rest position by this many anchored Y pixels.")]
        [SerializeField] private float _slideDistance = 40f;
        [SerializeField] private float _introDuration = 0.18f;
        [SerializeField] private float _holdDuration = 1.2f;
        [SerializeField] private float _outroDuration = 0.14f;
        [SerializeField] private float _valueCountDuration = 0.18f;
        [SerializeField] private float _valueStepDuration = 0.035f;
        [SerializeField] private float _pulseScale = 1.04f;
        [SerializeField] private float _impactScale = 1.08f;
        [SerializeField] private float _impactFlashDuration = 0.16f;

        public RectTransform ImpactAnchor
        {
            get
            {
                EnsurePanel();
                return _panel;
            }
        }

        private void Awake()
        {
            EnsurePanel();
            CaptureRestPosition();
            ResolveOptionalReferences();
            BeginStatSpriteAssetLoad();
            ApplyDisplayValues(0, 0, 0, 0);
            StartCoroutine(ApplyStatSpriteAssetWhenReady());
        }

        public IEnumerator Play(SlotFinalPresentationResult result, Func<bool> shouldSkip)
        {
            if (result == null)
            {
                yield break;
            }

            yield return ShowLive(result, shouldSkip);
            yield return WaitOrSkip(_holdDuration, shouldSkip);
            yield return HideLive(shouldSkip);
        }

        public IEnumerator ShowLive(SlotFinalPresentationResult result, Func<bool> shouldSkip)
        {
            if (result == null)
            {
                yield break;
            }

            EnsurePanel();
            CaptureRestPosition();
            ResolveOptionalReferences();
            yield return EnsureStatSpriteAsset(shouldSkip);
            CaptureDisplayedResult(result);

            gameObject.SetActive(true);

            yield return PlaySlideTween(
                _restAnchoredY,
                _restAnchoredY + _slideDistance,
                _introDuration,
                Ease.OutBack,
                shouldSkip);
        }

        public void UpdateLive(SlotFinalPresentationResult result, bool pulse)
        {
            UpdateLive(result, pulse, null);
        }

        public void UpdateLive(
            SlotFinalPresentationResult result,
            bool pulse,
            SlotRelicTriggerPresentationResult impactRelic)
        {
            if (result == null)
            {
                return;
            }

            EnsurePanel();
            CaptureRestPosition();
            ResolveOptionalReferences();
            BeginStatSpriteAssetLoad();
            ApplyStatSpriteAsset();

            if (_panel != null && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                _panel.anchoredPosition =
                    new Vector2(_panel.anchoredPosition.x, _restAnchoredY + _slideDistance);
            }

            if (!_hasDisplayedResult || _valueCountDuration <= 0f)
            {
                CaptureDisplayedResult(result);
            }
            else
            {
                PlayValueTween(result);
            }

            if (pulse)
            {
                PlayPulseTween(ImpactColorFor(impactRelic), IsStrongImpact(impactRelic));
            }
        }

        public IEnumerator CompleteLive(SlotFinalPresentationResult result, Func<bool> shouldSkip)
        {
            UpdateLive(result, true, null);
            yield return WaitOrSkip(_holdDuration, shouldSkip);
            yield return HideLive(shouldSkip);
        }

        public IEnumerator HideLive(Func<bool> shouldSkip)
        {
            EnsurePanel();
            CaptureRestPosition();

            if (_panel == null)
            {
                gameObject.SetActive(false);
                yield break;
            }

            yield return PlaySlideTween(
                _panel.anchoredPosition.y,
                _restAnchoredY,
                _outroDuration,
                Ease.InCubic,
                shouldSkip);

            HideImmediate();
        }

        public void HideImmediate()
        {
            EnsurePanel();
            KillActiveTween();

            if (_panel != null && _hasRestAnchoredY)
            {
                _panel.anchoredPosition = new Vector2(_panel.anchoredPosition.x, _restAnchoredY);
                _panel.localScale = Vector3.one;
            }

            _hasDisplayedResult = false;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            KillActiveTween();
        }

        private void OnDestroy()
        {
            ReleaseStatSpriteAsset();
        }

        private void EnsurePanel()
        {
            if (_panel == null)
            {
                _panel = transform as RectTransform;
            }
        }

        private void CaptureRestPosition()
        {
            if (_hasRestAnchoredY || _panel == null)
            {
                return;
            }

            _restAnchoredY = _panel.anchoredPosition.y;
            _hasRestAnchoredY = true;
        }

        private void ResolveOptionalReferences()
        {
            if (_panel == null)
            {
                return;
            }

            _panelImage ??= _panel.GetComponent<Image>();

            if (_resultTmpText == null)
            {
                ResolveTmpTextReferences(_panel.GetComponentsInChildren<TMP_Text>(true));
            }

            ApplyStatSpriteAsset();
        }

        private void ResolveTmpTextReferences(TMP_Text[] texts)
        {
            if (texts == null || texts.Length == 0)
            {
                return;
            }

            for (int index = 0; index < texts.Length; index++)
            {
                TMP_Text text = texts[index];
                if (text == null)
                {
                    continue;
                }

                string objectName = text.gameObject.name;
   
                if (_resultTmpText == null &&
                    (ContainsIgnoreCase(objectName, "summary") ||
                     ContainsIgnoreCase(objectName, "result") ||
                     ContainsIgnoreCase(objectName, "value")))
                {
                    _resultTmpText = text;
                }
            }

            if (_resultTmpText == null)
            {
                _resultTmpText = texts[texts.Length - 1];
            }
        }

        private void ApplyStatSpriteAsset()
        {
            if (_resultTmpText == null || _statSpriteAsset == null)
            {
                return;
            }

            _resultTmpText.spriteAsset = _statSpriteAsset;
        }

        private void PlayValueTween(SlotFinalPresentationResult result)
        {
            if (_valueTween != null && _valueTween.IsActive())
            {
                _valueTween.Kill();
            }

            int startDamage = _displayDamage;
            int startDefense = _displayDefense;
            int startHeal = _displayHeal;
            int startAttackCount = _displayAttackCount;
            int targetDamage = Mathf.Max(0, result.Damage);
            int targetDefense = Mathf.Max(0, result.Defense);
            int targetHeal = Mathf.Max(0, result.HealAmount);
            int targetAttackCount = Mathf.Max(1, result.AttackCount);

            int steps = Mathf.Max(
                Mathf.Abs(targetDamage - startDamage),
                Mathf.Abs(targetDefense - startDefense),
                Mathf.Abs(targetHeal - startHeal),
                Mathf.Abs(targetAttackCount - startAttackCount));

            if (steps <= 0)
            {
                CaptureDisplayedResult(result);
                return;
            }

            float stepDuration = Mathf.Max(0.01f, _valueStepDuration);
            Sequence sequence = DOTween.Sequence().SetTarget(this).SetUpdate(true);
            for (int step = 1; step <= steps; step++)
            {
                int capturedStep = step;
                sequence.AppendInterval(stepDuration);
                sequence.AppendCallback(() =>
                {
                    ApplyDisplayValues(
                        MoveTowardsInt(startDamage, targetDamage, capturedStep),
                        MoveTowardsInt(startDefense, targetDefense, capturedStep),
                        MoveTowardsInt(startAttackCount, targetAttackCount, capturedStep),
                        MoveTowardsInt(startHeal, targetHeal, capturedStep));
                });
            }

            _valueTween = sequence.OnComplete(() =>
            {
                CaptureDisplayedResult(result);
            });
        }

        private void ApplyDisplayValues(
            int damage,
            int defense,
            int attackCount,
            int healAmount)
        {
            _displayDamage = Mathf.Max(0, damage);
            _displayDefense = Mathf.Max(0, defense);
            _displayAttackCount = Mathf.Max(1, attackCount);
            _displayHeal = Mathf.Max(0, healAmount);
            _hasDisplayedResult = true;

            SetSummaryValues(
                _displayDamage,
                _displayDefense,
                _displayAttackCount,
                _displayHeal);
        }

        private void CaptureDisplayedResult(SlotFinalPresentationResult result)
        {
            _displayDamage = Mathf.Max(0, result.Damage);
            _displayDefense = Mathf.Max(0, result.Defense);
            _displayAttackCount = Mathf.Max(1, result.AttackCount);
            _displayHeal = Mathf.Max(0, result.HealAmount);
            _hasDisplayedResult = true;

            SetSummaryValues(
                _displayDamage,
                _displayDefense,
                _displayAttackCount,
                _displayHeal);
        }

        private void SetSummaryValues(
            int damage,
            int defense,
            int attackCount,
            int healAmount)
        {
            if (_resultTmpText != null)
            {
                _resultTmpText.text = !_statSpriteAssetLoadFailed
                    ? BuildRichSummaryText(damage, defense, attackCount, healAmount)
                    : BuildPlainSummaryText(damage, defense, attackCount, healAmount);
            }
        }

        private void PlayPulseTween(Color impactColor, bool strongImpact)
        {
            if (_panel == null)
            {
                return;
            }

            if (_pulseTween != null && _pulseTween.IsActive())
            {
                _pulseTween.Kill();
            }

            _panel.localScale = Vector3.one;
            float targetScale = strongImpact ? _impactScale : _pulseScale;
            _pulseTween = DOTween.Sequence()
                .SetTarget(_panel)
                .SetUpdate(true)
                .Append(_panel.DOScale(new Vector3(targetScale, targetScale, 1f), 0.08f).SetEase(Ease.OutBack))
                .Append(_panel.DOScale(Vector3.one, 0.12f).SetEase(Ease.OutCubic));

            if (_panelImage == null)
            {
                return;
            }

            if (_flashTween != null && _flashTween.IsActive())
            {
                _flashTween.Kill();
                RestorePanelBaseColor();
            }

            CapturePanelBaseColor();
            Color flashColor = impactColor;
            flashColor.a = _panelBaseColor.a;

            float duration = Mathf.Max(0.01f, _impactFlashDuration);
            _flashTween = DOTween.Sequence()
                .SetTarget(_panelImage)
                .SetUpdate(true)
                .Append(TweenPanelImageColor(flashColor, duration * 0.35f).SetEase(Ease.OutCubic))
                .Append(TweenPanelImageColor(_panelBaseColor, duration * 0.65f).SetEase(Ease.InCubic))
                .OnComplete(RestorePanelBaseColor);
        }

        private Tween TweenPanelImageColor(Color targetColor, float duration)
        {
            Image image = _panelImage;
            return DOTween.To(
                    () => image != null ? image.color : targetColor,
                    value =>
                    {
                        if (image != null)
                        {
                            image.color = value;
                        }
                    },
                    targetColor,
                    duration)
                .SetTarget(image)
                .SetUpdate(true);
        }

        private void CapturePanelBaseColor()
        {
            if (_panelImage == null)
            {
                return;
            }

            _panelBaseColor = _panelImage.color;
            _hasPanelBaseColor = true;
        }

        private void RestorePanelBaseColor()
        {
            if (_panelImage != null && _hasPanelBaseColor)
            {
                _panelImage.color = _panelBaseColor;
            }
        }

        private static Color ImpactColorFor(SlotRelicTriggerPresentationResult impactRelic)
        {
            if (impactRelic == null)
            {
                return Color.white;
            }

            if (impactRelic.DamagePerHit > 0)
            {
                return new Color(1f, 0.45f, 0.25f, 1f);
            }

            if (impactRelic.Block > 0)
            {
                return new Color(0.45f, 0.7f, 1f, 1f);
            }

            if (impactRelic.Heal > 0)
            {
                return new Color(0.45f, 1f, 0.6f, 1f);
            }

            return Color.white;
        }

        private static bool IsStrongImpact(SlotRelicTriggerPresentationResult impactRelic)
        {
            return impactRelic != null &&
                (impactRelic.DamagePerHit > 0 ||
                 impactRelic.Block > 0 ||
                 impactRelic.Heal > 0);
        }

        private IEnumerator EnsureStatSpriteAsset(Func<bool> shouldSkip)
        {
            if (_resultTmpText == null || _statSpriteAsset != null || _statSpriteAssetLoadFailed)
            {
                ApplyStatSpriteAsset();
                yield break;
            }

            BeginStatSpriteAssetLoad();
            while (_statSpriteAssetHandle.IsValid() &&
                !_statSpriteAssetHandle.IsDone &&
                !IsSkipped(shouldSkip))
            {
                yield return null;
            }

            CaptureLoadedStatSpriteAsset();
            ApplyStatSpriteAsset();
        }

        private IEnumerator ApplyStatSpriteAssetWhenReady()
        {
            if (_resultTmpText == null || _statSpriteAsset != null || _statSpriteAssetLoadFailed)
            {
                ApplyStatSpriteAsset();
                RefreshDisplayedSummary();
                yield break;
            }

            BeginStatSpriteAssetLoad();
            while (_statSpriteAssetHandle.IsValid() &&
                !_statSpriteAssetHandle.IsDone)
            {
                yield return null;
            }

            CaptureLoadedStatSpriteAsset();
            ApplyStatSpriteAsset();
            RefreshDisplayedSummary();
        }

        private void BeginStatSpriteAssetLoad()
        {
            if (_resultTmpText == null || _statSpriteAsset != null || _statSpriteAssetLoadFailed)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_statSpriteAssetAddress))
            {
                _statSpriteAssetLoadFailed = true;
                return;
            }

            if (!_statSpriteAssetHandle.IsValid())
            {
                _statSpriteAssetHandle =
                    Addressables.LoadAssetAsync<TMP_SpriteAsset>(_statSpriteAssetAddress);
            }

            CaptureLoadedStatSpriteAsset();
        }

        private void CaptureLoadedStatSpriteAsset()
        {
            if (!_statSpriteAssetHandle.IsValid() ||
                !_statSpriteAssetHandle.IsDone ||
                _statSpriteAsset != null ||
                _statSpriteAssetLoadFailed)
            {
                return;
            }

            if (_statSpriteAssetHandle.Status == AsyncOperationStatus.Succeeded)
            {
                _statSpriteAsset = _statSpriteAssetHandle.Result;
                return;
            }

            _statSpriteAssetLoadFailed = true;
            string reason = _statSpriteAssetHandle.OperationException?.Message ?? "unknown error";
            Debug.LogWarning(
                $"[FinalResultDirector] TMP sprite asset '{_statSpriteAssetAddress}' load failed: {reason}");
        }

        private void RefreshDisplayedSummary()
        {
            SetSummaryValues(
                _displayDamage,
                _displayDefense,
                _displayAttackCount,
                _displayHeal);
        }

        private void ReleaseStatSpriteAsset()
        {
            if (_statSpriteAssetHandle.IsValid())
            {
                Addressables.Release(_statSpriteAssetHandle);
            }

            _statSpriteAssetHandle = default;
            _statSpriteAsset = null;
            _statSpriteAssetLoadFailed = false;
        }

        private static string BuildPlainSummaryText(
            int damage,
            int defense,
            int attackCount,
            int healAmount)
        {
            string summary = $"ATK {damage} / DEF {defense} / HEAL {healAmount}";
            if (attackCount > 1)
            {
                summary += $" / HIT {attackCount}";
            }

            return summary;
        }

        private static string BuildRichSummaryText(
            int damage,
            int defense,
            int attackCount,
            int healAmount)
        {
            string summary =
                $"{BuildStatSpriteTag(AttackSpriteIndex)} {damage}   " +
                $"{BuildStatSpriteTag(DefenseSpriteIndex)} {defense}   " +
                $"{BuildStatSpriteTag(HealSpriteIndex)} {healAmount}";
            if (attackCount > 1)
            {
                summary += $"   HIT {attackCount}";
            }

            return summary;
        }

        private static string BuildStatSpriteTag(int spriteIndex)
        {
            return $"<sprite index={spriteIndex}>";
        }

        private static int MoveTowardsInt(int start, int target, int step)
        {
            if (start == target)
            {
                return target;
            }

            return start < target
                ? Mathf.Min(start + step, target)
                : Mathf.Max(start - step, target);
        }

        private IEnumerator PlaySlideTween(
            float fromY, float toY, float duration, Ease ease, Func<bool> shouldSkip)
        {
            EnsurePanel();

            if (_panel == null)
            {
                yield break;
            }

            float x = _panel.anchoredPosition.x;

            if (duration <= 0f || IsSkipped(shouldSkip))
            {
                _panel.anchoredPosition = new Vector2(x, toY);
                yield break;
            }

            _panel.anchoredPosition = new Vector2(x, fromY);
            RectTransform panel = _panel;
            Tween tween = DOTween.To(
                    () => panel.anchoredPosition.y,
                    value => panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, value),
                    toY,
                    duration)
                .SetTarget(panel)
                .SetEase(ease)
                .SetUpdate(true);
            yield return PlayTween(tween, shouldSkip);
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

            if (_pulseTween != null && _pulseTween.IsActive())
            {
                _pulseTween.Kill();
            }

            _pulseTween = null;

            if (_valueTween != null && _valueTween.IsActive())
            {
                _valueTween.Kill();
            }

            _valueTween = null;

            if (_flashTween != null && _flashTween.IsActive())
            {
                _flashTween.Kill();
                RestorePanelBaseColor();
            }

            _flashTween = null;

            if (_panel != null)
            {
                _panel.DOKill();
            }
        }

        private static void SetText(Text text, TMP_Text tmpText, string value)
        {
            if (text != null)
            {
                text.text = value;
            }

            if (tmpText != null)
            {
                tmpText.text = value;
            }
        }

        private static bool ContainsIgnoreCase(string value, string part)
        {
            return value != null &&
                part != null &&
                value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void NoOp()
        {
        }

        private Tween _activeTween;
        private Tween _pulseTween;
        private Tween _valueTween;
        private Tween _flashTween;
        private AsyncOperationHandle<TMP_SpriteAsset> _statSpriteAssetHandle;
        private TMP_SpriteAsset _statSpriteAsset;
        private int _displayDamage;
        private int _displayDefense;
        private int _displayAttackCount = 1;
        private int _displayHeal;
        private Color _panelBaseColor;
        private float _restAnchoredY;
        private bool _hasRestAnchoredY;
        private bool _hasDisplayedResult;
        private bool _hasPanelBaseColor;
        private bool _statSpriteAssetLoadFailed;
    }
}
