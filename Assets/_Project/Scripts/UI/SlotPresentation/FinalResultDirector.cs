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
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _summaryText;
        [SerializeField] private TMP_Text _titleTmpText;
        [SerializeField] private TMP_Text _summaryTmpText;
        [SerializeField] private string _statSpriteAssetAddress = StatSpriteAssetAddress;
        [SerializeField] private Color _panelColor = new Color(0.1f, 0.15f, 0.2f, 0.98f);
        [SerializeField] private Color _damageImpactColor = new Color(1f, 0.48f, 0.2f, 1f);
        [SerializeField] private Color _defenseImpactColor = new Color(0.36f, 0.72f, 1f, 1f);
        [SerializeField] private Color _healImpactColor = new Color(0.42f, 1f, 0.56f, 1f);

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
            _titleTmpText = null;
            _summaryTmpText = null;
            _hasRestAnchoredY = false;
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
            ApplyResultText(result);
            CaptureDisplayedResult(result);

            if (_panelImage != null)
            {
                _panelImage.color = _panelColor;
            }

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
                ApplyResultText(result);
                CaptureDisplayedResult(result);
            }
            else
            {
                PlayValueTween(result);
            }

            if (pulse)
            {
                PlayPulseTween(ResolveImpactColor(impactRelic), impactRelic != null);
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

            if (_panelImage != null)
            {
                _panelImage.color = _panelColor;
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

            if (_titleText == null || _summaryText == null)
            {
                ResolveTextReferences(_panel.GetComponentsInChildren<Text>(true));
            }

            if (_titleTmpText == null || _summaryTmpText == null)
            {
                ResolveTmpTextReferences(_panel.GetComponentsInChildren<TMP_Text>(true));
            }

            ApplyStatSpriteAsset();
        }

        private void ResolveTextReferences(Text[] texts)
        {
            if (texts == null || texts.Length == 0)
            {
                return;
            }

            for (int index = 0; index < texts.Length; index++)
            {
                Text text = texts[index];
                if (text == null)
                {
                    continue;
                }

                string objectName = text.gameObject.name;
                if (_titleText == null && ContainsIgnoreCase(objectName, "title"))
                {
                    _titleText = text;
                }
                else if (_summaryText == null &&
                    (ContainsIgnoreCase(objectName, "summary") ||
                     ContainsIgnoreCase(objectName, "result") ||
                     ContainsIgnoreCase(objectName, "value")))
                {
                    _summaryText = text;
                }
            }

            if (_summaryText == null)
            {
                _summaryText = texts[texts.Length - 1];
            }

            if (_titleText == null && texts.Length > 1)
            {
                _titleText = texts[0] != _summaryText ? texts[0] : null;
            }
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
                if (_titleTmpText == null && ContainsIgnoreCase(objectName, "title"))
                {
                    _titleTmpText = text;
                }
                else if (_summaryTmpText == null &&
                    (ContainsIgnoreCase(objectName, "summary") ||
                     ContainsIgnoreCase(objectName, "result") ||
                     ContainsIgnoreCase(objectName, "value")))
                {
                    _summaryTmpText = text;
                }
            }

            if (_summaryTmpText == null)
            {
                _summaryTmpText = texts[texts.Length - 1];
            }

            if (_titleTmpText == null && texts.Length > 1)
            {
                _titleTmpText = texts[0] != _summaryTmpText ? texts[0] : null;
            }
        }

        private void ApplyResultText(SlotFinalPresentationResult result)
        {
            SetText(_titleText, _titleTmpText, "FINAL RESULT");
            SetSummaryValues(
                result.Damage,
                result.Defense,
                result.AttackCount,
                result.HealAmount);
        }

        private void ApplyStatSpriteAsset()
        {
            if (_summaryTmpText == null || _statSpriteAsset == null)
            {
                return;
            }

            _summaryTmpText.spriteAsset = _statSpriteAsset;
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
                ApplyResultText(result);
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
                ApplyResultText(result);
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

            SetText(_titleText, _titleTmpText, "FINAL RESULT");
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
        }

        private void SetSummaryValues(
            int damage,
            int defense,
            int attackCount,
            int healAmount)
        {
            string plainText = BuildPlainSummaryText(damage, defense, attackCount, healAmount);
            if (_summaryText != null)
            {
                _summaryText.text = plainText;
            }

            if (_summaryTmpText != null)
            {
                _summaryTmpText.text = BuildRichSummaryText(damage, defense, attackCount, healAmount);
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
            }

            Color flashColor = Color.Lerp(_panelColor, impactColor, strongImpact ? 0.55f : 0.28f);
            _panelImage.color = flashColor;
            _flashTween = DOTween.To(
                    () => _panelImage.color,
                    value => _panelImage.color = value,
                    _panelColor,
                    Mathf.Max(0.01f, _impactFlashDuration))
                .SetEase(Ease.OutCubic)
                .SetTarget(_panelImage)
                .SetUpdate(true);
        }

        private Color ResolveImpactColor(SlotRelicTriggerPresentationResult impactRelic)
        {
            if (impactRelic == null)
            {
                return _panelColor;
            }

            if (impactRelic.Heal > 0)
            {
                return _healImpactColor;
            }

            if (impactRelic.Block > 0)
            {
                return _defenseImpactColor;
            }

            if (impactRelic.DamagePerHit > 0)
            {
                return _damageImpactColor;
            }

            return _panelColor;
        }

        private IEnumerator EnsureStatSpriteAsset(Func<bool> shouldSkip)
        {
            if (_summaryTmpText == null || _statSpriteAsset != null || _statSpriteAssetLoadFailed)
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

        private void BeginStatSpriteAssetLoad()
        {
            if (_summaryTmpText == null || _statSpriteAsset != null || _statSpriteAssetLoadFailed)
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
        private float _restAnchoredY;
        private bool _hasRestAnchoredY;
        private bool _hasDisplayedResult;
        private bool _statSpriteAssetLoadFailed;
    }
}
