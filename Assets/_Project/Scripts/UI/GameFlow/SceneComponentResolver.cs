using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlotRogue.UI.GameFlow
{
    internal static class SceneComponentResolver
    {
        internal static T FindInSceneRoot<T>(Transform origin) where T : Component
        {
            Transform root = origin.root != null ? origin.root : origin;
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }

            Scene scene = origin.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                component = roots[index].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        internal static Transform FindDeepChild(Transform parent, string objectName)
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
    }
}
