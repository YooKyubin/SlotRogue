using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class FloatingCombatTextLayerView : MonoBehaviour, ICombatPresentationCommands
    {
        private readonly Dictionary<int, RectTransform> _enemyDamageAnchors = new();

        [SerializeField] private RectTransform _floatingTextRoot;
        [SerializeField] private FloatingDamageTextView _floatingDamageTextPrefab;
        [SerializeField] private RectTransform _playerDamageAnchor;
        [SerializeField] private RectTransform _fallbackMonsterDamageAnchor;
        [SerializeField] private Font _defaultFont;
        [SerializeField] private GameObject _linkTarget;

        public void Bind(
            Transform floatingTextRoot,
            FloatingDamageTextView floatingDamageTextPrefab,
            RectTransform playerDamageAnchor,
            RectTransform fallbackMonsterDamageAnchor,
            Font defaultFont,
            GameObject linkTarget)
        {
            _floatingTextRoot = floatingTextRoot as RectTransform;
            _floatingDamageTextPrefab = floatingDamageTextPrefab;
            _playerDamageAnchor = playerDamageAnchor;
            _fallbackMonsterDamageAnchor = fallbackMonsterDamageAnchor;
            _defaultFont = defaultFont;
            _linkTarget = linkTarget;
        }

        public void SetEnemyDamageAnchor(
            CombatParticipantId participantId,
            RectTransform anchor)
        {
            if (!participantId.IsValid || anchor == null)
            {
                return;
            }

            _enemyDamageAnchors[participantId.Value] = anchor;
        }

        public async UniTask ShowFloatingDamageAsync(
            FloatingDamageRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Amount <= 0)
            {
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

            RectTransform anchor = ResolveDamageAnchor(request.TargetParticipantId, request.IsPlayerTarget);
            if (anchor == null)
            {
                Debug.LogWarning($"[Presentation] Missing {(request.IsPlayerTarget ? "player" : "monster")} damage anchor.");
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
            await damageText.Play(request.Amount, request.IsCritical, anchorKind, cancellationToken);
        }

        public async UniTask ShowTurnBannerAsync(
            string message,
            float duration,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (_floatingTextRoot == null)
            {
                await CombatPresentationTweens.DelayAsync(duration, ResolveLinkTarget(), cancellationToken);
                return;
            }

            var bannerObject = new GameObject("Turn Banner", typeof(RectTransform));
            RectTransform rectTransform = bannerObject.GetComponent<RectTransform>();
            rectTransform.SetParent(_floatingTextRoot, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, 180f);
            rectTransform.sizeDelta = new Vector2(700f, 80f);

            var text = bannerObject.AddComponent<Text>();
            text.font = _defaultFont;
            text.fontSize = 40;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color32(255, 230, 140, 255);
            text.text = message;

            try
            {
                await CombatPresentationTweens.DelayAsync(duration, ResolveLinkTarget(), cancellationToken);
            }
            finally
            {
                if (bannerObject != null)
                {
                    Destroy(bannerObject);
                }
            }
        }

        private RectTransform ResolveDamageAnchor(
            CombatParticipantId participantId,
            bool isPlayerTarget)
        {
            if (isPlayerTarget)
            {
                return _playerDamageAnchor;
            }

            if (participantId.IsValid &&
                _enemyDamageAnchors.TryGetValue(participantId.Value, out RectTransform anchor))
            {
                return anchor;
            }

            return _fallbackMonsterDamageAnchor;
        }

        private GameObject ResolveLinkTarget()
        {
            return _linkTarget != null ? _linkTarget : gameObject;
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
