using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class FloatingCombatTextLayerView : MonoBehaviour
    {
        [SerializeField] private RectTransform _floatingTextRoot;
        [SerializeField] private FloatingDamageTextView _floatingDamageTextPrefab;
        [SerializeField] private MonoBehaviour _damageAnchorRegistrySource;

        [Header("플레이어 고정 데미지 텍스트 (지정 시 플레이어 피격만 이 텍스트를 흔들며 표시; 몬스터는 기존 플로팅)")]
        [Tooltip("플레이어가 피해 입을 때 표시. 평소엔 자동으로 숨김.")]
        [SerializeField] private TMP_Text _playerDamageText;
        [SerializeField] private float _fixedTextHoldDuration = 0.5f;
        [SerializeField] private float _shakeMagnitude = 7f;
        [SerializeField] private float _shakeDuration = 0.25f;

        private ICombatDamageAnchorRegistry _damageAnchorRegistry;

        private void Awake()
        {
            ResolveDamageAnchorRegistry();
            // 평소엔 숨김.
            if (_playerDamageText != null) _playerDamageText.gameObject.SetActive(false);
        }

        public async UniTask ShowFloatingCombatTextAsync(
            FloatingCombatTextRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Amount <= 0)
            {
                return;
            }

            // 플레이어 피격만 고정 텍스트로 표시(지정된 경우). 몬스터는 기존 플로팅 경로를 탄다.
            if (request.Kind == FloatingCombatTextKind.Damage &&
                request.IsPlayerTarget &&
                _playerDamageText != null)
            {
                await ShowPlayerFixedDamageAsync(request, cancellationToken);
                return;
            }

            if (_floatingDamageTextPrefab == null)
            {
                Debug.LogWarning("[Presentation] FloatingDamageText prefab is not assigned.");
                return;
            }

            if (_floatingTextRoot == null)
            {
                Debug.LogWarning("[Presentation] FloatingTextRoot is not assigned.");
                return;
            }

            ResolveDamageAnchorRegistry();
            RectTransform anchor = _damageAnchorRegistry != null
                ? _damageAnchorRegistry.ResolveDamageAnchor(request.TargetParticipantId, request.IsPlayerTarget)
                : null;
            if (anchor == null)
            {
                Debug.LogError(
                    $"[Presentation] Missing {(request.IsPlayerTarget ? "player" : "monster")} damage anchor " +
                    $"for participant {request.TargetParticipantId.Value}.");
                return;
            }

            FloatingDamageTextView damageText = Instantiate(
                _floatingDamageTextPrefab,
                _floatingTextRoot);
            if (damageText.transform is RectTransform textTransform)
            {
                AlignFloatingTextToAnchor(textTransform, anchor, _floatingTextRoot);
            }

            CombatAnchorKind anchorKind = request.IsPlayerTarget
                ? CombatAnchorKind.Player
                : CombatAnchorKind.Monster;
            await damageText.Play(request, anchorKind, cancellationToken);
        }

        // 플레이어 고정 데미지 텍스트를 띄우고 약간 흔든 뒤 잠시 유지하고 다시 숨긴다.
        private async UniTask ShowPlayerFixedDamageAsync(
            FloatingCombatTextRequest request,
            CancellationToken cancellationToken)
        {
            TMP_Text target = _playerDamageText;
            RectTransform rect = target.rectTransform;
            Vector2 basePosition = rect.anchoredPosition;
            target.text = request.Amount.ToString();
            target.gameObject.SetActive(true);

            try
            {
                // 약간의 흔들림. 시간이 지날수록 진폭을 감쇠시켜 자연스럽게 멈춘다.
                float elapsed = 0f;
                while (elapsed < _shakeDuration)
                {
                    elapsed += Time.deltaTime;
                    float falloff = 1f - Mathf.Clamp01(elapsed / _shakeDuration);
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * (_shakeMagnitude * falloff);
                    rect.anchoredPosition = basePosition + offset;
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                rect.anchoredPosition = basePosition;
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_fixedTextHoldDuration),
                    cancellationToken: cancellationToken);
            }
            finally
            {
                // 취소/완료 무엇이든 위치 복원 + 숨김.
                rect.anchoredPosition = basePosition;
                target.gameObject.SetActive(false);
            }
        }

        private void ResolveDamageAnchorRegistry()
        {
            if (_damageAnchorRegistry != null)
            {
                return;
            }

            _damageAnchorRegistry = _damageAnchorRegistrySource as ICombatDamageAnchorRegistry;
        }

        private static void AlignFloatingTextToAnchor(
            RectTransform floatingText,
            RectTransform anchor,
            RectTransform floatingRoot)
        {
            Canvas floatingCanvas = floatingRoot.GetComponentInParent<Canvas>();
            Camera floatingCamera = null;
            if (floatingCanvas != null && floatingCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                floatingCamera = floatingCanvas.worldCamera;
            }

            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                ResolveAnchorCamera(anchor),
                worldCenter);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    floatingRoot,
                    screenPoint,
                    floatingCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            floatingText.anchorMin = new Vector2(0.5f, 0.5f);
            floatingText.anchorMax = new Vector2(0.5f, 0.5f);
            floatingText.pivot = new Vector2(0.5f, 0.5f);
            floatingText.anchoredPosition = localPoint;
        }

        private static Camera ResolveAnchorCamera(RectTransform anchor)
        {
            Canvas anchorCanvas = anchor.GetComponentInParent<Canvas>();
            if (anchorCanvas != null)
            {
                if (anchorCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return null;
                }

                return anchorCanvas.worldCamera;
            }

            return Camera.main;
        }
    }
}
