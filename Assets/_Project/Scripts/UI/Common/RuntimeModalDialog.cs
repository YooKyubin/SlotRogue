using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Common
{
    /// <summary>
    /// 코드로 즉석에서 띄우는 임시 모달 다이얼로그(제목 + 메시지 + 버튼 1~2개).
    /// 일시정지 등 아직 씬에 저작되지 않은 팝업을 임시로 제공합니다.
    /// </summary>
    public sealed class RuntimeModalDialog : MonoBehaviour
    {
        private Action _onPrimary;
        private Action _onSecondary;

        /// <summary>다이얼로그를 생성해 표시합니다. 버튼 클릭 시 자동으로 닫힙니다.</summary>
        public static RuntimeModalDialog Show(
            string title,
            string message,
            string primaryLabel,
            Action onPrimary,
            string secondaryLabel = null,
            Action onSecondary = null)
        {
            var host = new GameObject(nameof(RuntimeModalDialog));
            var dialog = host.AddComponent<RuntimeModalDialog>();
            dialog.Build(title, message, primaryLabel, onPrimary, secondaryLabel, onSecondary);
            return dialog;
        }

        public void Close()
        {
            if (this != null)
            {
                Destroy(gameObject);
            }
        }

        private void Build(
            string title,
            string message,
            string primaryLabel,
            Action onPrimary,
            string secondaryLabel,
            Action onSecondary)
        {
            _onPrimary = onPrimary;
            _onSecondary = onSecondary;

            Canvas canvas = RuntimeUiKit.CreateOverlayCanvas("RuntimeModalCanvas", 5000);
            canvas.transform.SetParent(transform, false);

            RuntimeUiKit.CreateBackdrop(canvas.transform, new Color(0f, 0f, 0f, 0.65f));

            RectTransform panel = RuntimeUiKit.CreatePanel(
                canvas.transform,
                new Color(0.12f, 0.13f, 0.18f, 0.98f),
                new Vector2(780f, 460f));

            Text title_t = RuntimeUiKit.CreateText(
                panel, title, 44, TextAnchor.MiddleCenter, new Color(0.96f, 0.86f, 0.5f, 1f));
            RuntimeUiKit.Place(title_t.rectTransform, new Vector2(0f, 150f), new Vector2(700f, 80f));

            Text message_t = RuntimeUiKit.CreateText(
                panel, message, 32, TextAnchor.MiddleCenter, new Color(0.9f, 0.92f, 0.96f, 1f));
            RuntimeUiKit.Place(message_t.rectTransform, new Vector2(0f, 30f), new Vector2(700f, 160f));

            bool hasSecondary = !string.IsNullOrEmpty(secondaryLabel);
            var buttonSize = new Vector2(300f, 96f);

            if (hasSecondary)
            {
                Button secondary = RuntimeUiKit.CreateButton(
                    panel, secondaryLabel, new Color(0.30f, 0.32f, 0.40f, 1f), HandleSecondary);
                RuntimeUiKit.Place(
                    secondary.GetComponent<RectTransform>(), new Vector2(-170f, -140f), buttonSize);

                Button primary = RuntimeUiKit.CreateButton(
                    panel, primaryLabel, new Color(0.86f, 0.58f, 0.18f, 1f), HandlePrimary);
                RuntimeUiKit.Place(
                    primary.GetComponent<RectTransform>(), new Vector2(170f, -140f), buttonSize);
            }
            else
            {
                Button primary = RuntimeUiKit.CreateButton(
                    panel, primaryLabel, new Color(0.86f, 0.58f, 0.18f, 1f), HandlePrimary);
                RuntimeUiKit.Place(
                    primary.GetComponent<RectTransform>(), new Vector2(0f, -140f), buttonSize);
            }
        }

        private void HandlePrimary()
        {
            Action action = _onPrimary;
            Close();
            action?.Invoke();
        }

        private void HandleSecondary()
        {
            Action action = _onSecondary;
            Close();
            action?.Invoke();
        }
    }
}
