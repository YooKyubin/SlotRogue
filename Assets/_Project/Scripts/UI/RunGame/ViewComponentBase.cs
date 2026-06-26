using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// 하이어라키에 배치된 자식을 이름으로 해석하고 텍스트/표시 상태를 안전하게 갱신하는
    /// View 공용 헬퍼입니다. 각 View가 복붙하던 FindDeepChild/SetText/HasText 등을 한곳에 모읍니다.
    /// (직렬화 미할당/파괴된 참조는 Unity 오버로드 != null로 거릅니다 — AGENTS §6.)
    /// </summary>
    public abstract class ViewComponentBase : MonoBehaviour
    {
        protected T FindChildComponent<T>(string objectName) where T : Component
        {
            Transform child = FindDeepChild(transform, objectName);
            return child != null ? child.GetComponent<T>() : null;
        }

        protected static Transform FindDeepChild(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == objectName)
            {
                return parent;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform found = FindDeepChild(parent.GetChild(index), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        protected static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        protected static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        protected static bool HasText(Text text, TMP_Text tmpText)
        {
            return text != null || tmpText != null;
        }

        protected static void SetTextColor(Text text, Color color)
        {
            if (text != null)
            {
                text.color = color;
            }
        }

        protected static void SetTextColor(TMP_Text text, Color color)
        {
            if (text != null)
            {
                text.color = color;
            }
        }

        protected static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        // Component 오버로드: 미할당/파괴된 직렬화 참조를 Unity 오버로드 != null로 안전하게 거른다.
        // (component?.gameObject는 Unity "가짜 null"에서 UnassignedReferenceException을 던진다.)
        protected static void SetActive(Component target, bool active)
        {
            if (target != null)
            {
                target.gameObject.SetActive(active);
            }
        }

        protected static void AppendMissing(
            StringBuilder builder,
            bool hasReference,
            string label)
        {
            if (hasReference)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(label);
        }
    }
}
