using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class FloatingCombatTextLayerView : MonoBehaviour
    {
        [SerializeField] private RectTransform _floatingTextRoot;
        [SerializeField] private FloatingDamageTextView _floatingDamageTextPrefab;
        [SerializeField] private MonoBehaviour _damageAnchorRegistrySource;

        private ICombatDamageAnchorRegistry _damageAnchorRegistry;

        private void Awake()
        {
            ResolveDamageAnchorRegistry();
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
            await damageText.Play(request.Amount, request.IsCritical, anchorKind, cancellationToken);
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
