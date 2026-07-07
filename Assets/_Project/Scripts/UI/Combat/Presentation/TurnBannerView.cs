using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class TurnBannerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _bannerTmpText;
        [SerializeField] private Text _bannerText;
        [SerializeField] private GameObject _linkTarget;

        public async UniTask ShowTurnBannerAsync(
            string message,
            float duration,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (_bannerTmpText == null && _bannerText == null)
            {
                await CombatPresentationTweens.DelayAsync(duration, ResolveLinkTarget(), cancellationToken);
                return;
            }

            SetBannerText(message);
            SetBannerVisible(true);

            try
            {
                await CombatPresentationTweens.DelayAsync(duration, ResolveLinkTarget(), cancellationToken);
            }
            finally
            {
                SetBannerVisible(false);
                SetBannerText(string.Empty);
            }
        }

        private void SetBannerText(string message)
        {
            if (_bannerTmpText != null)
            {
                _bannerTmpText.text = message;
            }

            if (_bannerText != null)
            {
                _bannerText.text = message;
            }
        }

        private void SetBannerVisible(bool visible)
        {
            if (_bannerTmpText != null)
            {
                _bannerTmpText.gameObject.SetActive(visible);
            }

            if (_bannerText != null)
            {
                _bannerText.gameObject.SetActive(visible);
            }
        }

        private GameObject ResolveLinkTarget()
        {
            return _linkTarget != null ? _linkTarget : gameObject;
        }
    }
}
