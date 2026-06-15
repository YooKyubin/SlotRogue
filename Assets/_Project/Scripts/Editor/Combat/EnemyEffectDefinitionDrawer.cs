using System;
using System.Linq;
using SlotRogue.Data.Combat;
using UnityEditor;
using UnityEngine;

namespace SlotRogue.Editor.Combat
{
    [CustomPropertyDrawer(typeof(EnemyEffectDefinition), true)]
    public sealed class EnemyEffectDefinitionDrawer : PropertyDrawer
    {
        private const float MenuButtonWidth = 160f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect headerRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect labelRect = new(headerRect.x, headerRect.y, headerRect.width - MenuButtonWidth, headerRect.height);
            Rect buttonRect = new(labelRect.xMax, headerRect.y, MenuButtonWidth, headerRect.height);

            EditorGUI.LabelField(labelRect, label);

            string typeName = GetDisplayName(property.managedReferenceValue?.GetType());
            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(typeName), FocusType.Keyboard))
            {
                ShowTypeMenu(property);
            }

            if (property.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                DrawChildProperties(position, property);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference ||
                property.managedReferenceValue == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight;
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) &&
                   !SerializedProperty.EqualContents(iterator, end))
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUI.GetPropertyHeight(iterator, true);
                enterChildren = false;
            }

            return height;
        }

        private static void DrawChildProperties(Rect position, SerializedProperty property)
        {
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();
            bool enterChildren = true;
            float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            while (iterator.NextVisible(enterChildren) &&
                   !SerializedProperty.EqualContents(iterator, end))
            {
                float height = EditorGUI.GetPropertyHeight(iterator, true);
                Rect fieldRect = new(position.x, y, position.width, height);
                EditorGUI.PropertyField(fieldRect, iterator, true);
                y += height + EditorGUIUtility.standardVerticalSpacing;
                enterChildren = false;
            }
        }

        private static void ShowTypeMenu(SerializedProperty property)
        {
            GenericMenu menu = new();
            UnityEngine.Object[] targetObjects = property.serializedObject.targetObjects;
            string propertyPath = property.propertyPath;
            bool hasValue = property.managedReferenceValue != null;

            foreach (Type type in TypeCache.GetTypesDerivedFrom<EnemyEffectDefinition>()
                         .Where(effectType => !effectType.IsAbstract)
                         .OrderBy(effectType => effectType.Name))
            {
                Type capturedType = type;
                menu.AddItem(new GUIContent(GetDisplayName(capturedType)), false, () =>
                {
                    SetManagedReferenceValue(
                        targetObjects,
                        propertyPath,
                        Activator.CreateInstance(capturedType),
                        $"Set {nameof(EnemyEffectDefinition)} Type");
                });
            }

            if (hasValue)
            {
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Clear"), false, () =>
                {
                    SetManagedReferenceValue(
                        targetObjects,
                        propertyPath,
                        null,
                        $"Clear {nameof(EnemyEffectDefinition)} Type");
                });
            }

            menu.ShowAsContext();
        }

        private static void SetManagedReferenceValue(
            UnityEngine.Object[] targetObjects,
            string propertyPath,
            object value,
            string undoName)
        {
            var serializedObject = new SerializedObject(targetObjects);
            SerializedProperty targetProperty = serializedObject.FindProperty(propertyPath);
            if (targetProperty == null)
            {
                return;
            }

            serializedObject.Update();
            Undo.RecordObjects(targetObjects, undoName);
            targetProperty.managedReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private static string GetDisplayName(Type type)
        {
            if (type == null)
            {
                return "Select Effect Type";
            }

            return type.Name.Replace("EffectDefinition", string.Empty);
        }
    }
}
