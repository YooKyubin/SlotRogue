using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattlePresentationOverlayView : MonoBehaviour
    {
        [SerializeField] private RectTransform _floatingTextRoot;
        [SerializeField] private RectTransform _playerDamageAnchor;

        public Transform FloatingTextRoot => _floatingTextRoot;

        public RectTransform PlayerDamageAnchor => _playerDamageAnchor;

        public void Bind(RectTransform floatingTextRoot, RectTransform playerDamageAnchor)
        {
            _floatingTextRoot = floatingTextRoot;
            _playerDamageAnchor = playerDamageAnchor;
        }
    }
}
