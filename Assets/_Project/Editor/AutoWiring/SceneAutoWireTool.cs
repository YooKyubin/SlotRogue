using System;
using System.Collections.Generic;
using System.Reflection;
using SlotRogue.Core.Tooling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlotRogue.Editor.AutoWiring
{
    [InitializeOnLoad]
    public static class SceneAutoWireTool
    {
        private const string MenuRoot = "SlotRogue/Tools/Auto Wire/";
        private const string AutoRunMenu = MenuRoot + "Auto Wire On Hierarchy Change";
        private const string AutoRunPreferenceKey = "SlotRogue.AutoWire.AutoRunOnHierarchyChange";
        private const string ProjectNamespace = "SlotRogue";
        private const int MaxDetailLines = 80;

        private static bool _autoRunQueued;

        static SceneAutoWireTool()
        {
            EditorApplication.hierarchyChanged += QueueAutoRun;
        }

        [MenuItem(MenuRoot + "Wire Selected GameObjects")]
        public static void WireSelectedGameObjects()
        {
            GameObject[] gameObjects = Selection.gameObjects;
            var report = WireGameObjects(
                gameObjects,
                AutoWireOptions.Execute("Selected GameObjects", useHeuristics: true));
            report.Log();
        }

        [MenuItem(MenuRoot + "Wire Selected GameObjects", true)]
        private static bool CanWireSelectedGameObjects()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        [MenuItem(MenuRoot + "Wire Active Scene")]
        public static void WireActiveScene()
        {
            var report = WireScene(
                SceneManager.GetActiveScene(),
                AutoWireOptions.Execute("Active Scene", useHeuristics: true));
            report.Log();
        }

        [MenuItem(MenuRoot + "Wire All Open Scenes")]
        public static void WireAllOpenScenes()
        {
            var report = new AutoWireReport("All Open Scenes");
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                report.Merge(WireScene(
                    SceneManager.GetSceneAt(index),
                    AutoWireOptions.Execute("All Open Scenes", useHeuristics: true, quiet: true)));
            }

            report.Log();
        }

        [MenuItem(MenuRoot + "Report Active Scene")]
        public static void ReportActiveScene()
        {
            var report = WireScene(
                SceneManager.GetActiveScene(),
                AutoWireOptions.CreateDryRun("Active Scene Report", useHeuristics: true));
            report.Log();
        }

        [MenuItem(AutoRunMenu)]
        public static void ToggleAutoRun()
        {
            bool enabled = !EditorPrefs.GetBool(AutoRunPreferenceKey, false);
            EditorPrefs.SetBool(AutoRunPreferenceKey, enabled);
            Menu.SetChecked(AutoRunMenu, enabled);
            Debug.Log($"[AutoWire] Auto wire on hierarchy change {(enabled ? "enabled" : "disabled")}.");
        }

        [MenuItem(AutoRunMenu, true)]
        private static bool ValidateToggleAutoRun()
        {
            Menu.SetChecked(AutoRunMenu, EditorPrefs.GetBool(AutoRunPreferenceKey, false));
            return true;
        }

        private static AutoWireReport WireScene(Scene scene, AutoWireOptions options)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                var invalidReport = new AutoWireReport(options.Context);
                invalidReport.AddDetail("Scene is not valid or not loaded.");
                return invalidReport;
            }

            return WireGameObjects(scene.GetRootGameObjects(), options);
        }

        private static AutoWireReport WireGameObjects(
            IReadOnlyList<GameObject> roots,
            AutoWireOptions options)
        {
            var report = new AutoWireReport(options.Context);
            if (roots == null || roots.Count == 0)
            {
                report.AddDetail("No root GameObjects to scan.");
                return report;
            }

            var components = new HashSet<MonoBehaviour>();
            for (int index = 0; index < roots.Count; index++)
            {
                if (roots[index] == null)
                {
                    continue;
                }

                MonoBehaviour[] found = roots[index].GetComponentsInChildren<MonoBehaviour>(true);
                for (int componentIndex = 0; componentIndex < found.Length; componentIndex++)
                {
                    if (found[componentIndex] != null)
                    {
                        components.Add(found[componentIndex]);
                    }
                }
            }

            foreach (MonoBehaviour component in components)
            {
                WireComponent(component, options, report);
            }

            return report;
        }

        private static void WireComponent(
            MonoBehaviour component,
            AutoWireOptions options,
            AutoWireReport report)
        {
            Type componentType = component.GetType();
            if (!IsProjectType(componentType))
            {
                return;
            }

            FieldInfo[] fields = GetSerializableFields(componentType);
            if (fields.Length == 0)
            {
                return;
            }

            SerializedObject serializedObject = null;
            bool recordedUndo = false;
            bool changed = false;

            for (int index = 0; index < fields.Length; index++)
            {
                FieldInfo field = fields[index];
                AutoWireAttribute attribute = field.GetCustomAttribute<AutoWireAttribute>();
                if (attribute == null && !options.UseHeuristics)
                {
                    continue;
                }

                if (!TryGetObjectFieldKind(field, out Type elementType, out bool isArray))
                {
                    continue;
                }

                serializedObject ??= new SerializedObject(component);
                SerializedProperty property = serializedObject.FindProperty(field.Name);
                if (property == null)
                {
                    report.Unsupported++;
                    report.AddDetail($"{Describe(component, field)} is not serialized.");
                    continue;
                }

                report.Scanned++;
                bool allowOverwrite = options.AllowOverwrite || (attribute != null && attribute.AllowOverwrite);
                AutoWireSearchScope scope = attribute != null ? attribute.Scope : AutoWireSearchScope.Scene;
                bool includeInactive = attribute == null || attribute.IncludeInactive;
                string targetName = ResolveTargetName(field, attribute);
                List<UnityEngine.Object> candidates = FindCandidates(component, elementType, scope, includeInactive);
                if (attribute == null)
                {
                    RemoveOwnerCandidate(component, candidates);
                }

                bool fieldChanged = isArray
                    ? WireArrayField(
                        component,
                        field,
                        property,
                        elementType,
                        targetName,
                        attribute != null,
                        candidates,
                        allowOverwrite,
                        options,
                        report)
                    : WireObjectField(
                        component,
                        field,
                        property,
                        elementType,
                        targetName,
                        candidates,
                        allowOverwrite,
                        options,
                        report);

                if (!fieldChanged)
                {
                    continue;
                }

                if (!options.DryRun && !recordedUndo)
                {
                    Undo.RecordObject(component, "Auto Wire Scene References");
                    recordedUndo = true;
                }

                changed = true;
            }

            if (!changed || options.DryRun || serializedObject == null)
            {
                return;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
            if (component.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            }
        }

        private static bool WireObjectField(
            MonoBehaviour component,
            FieldInfo field,
            SerializedProperty property,
            Type elementType,
            string targetName,
            List<UnityEngine.Object> candidates,
            bool allowOverwrite,
            AutoWireOptions options,
            AutoWireReport report)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                report.Unsupported++;
                report.AddDetail($"{Describe(component, field)} is not an object reference property.");
                return false;
            }

            if (!allowOverwrite && property.objectReferenceValue != null)
            {
                report.AlreadySet++;
                return false;
            }

            AutoWireMatch match = ResolveSingleCandidate(candidates, targetName, elementType);
            if (match.Status == AutoWireMatchStatus.None)
            {
                report.Missing++;
                report.AddDetail($"{Describe(component, field)} missing target '{targetName}'.");
                return false;
            }

            if (match.Status == AutoWireMatchStatus.Ambiguous)
            {
                report.Ambiguous++;
                report.AddDetail($"{Describe(component, field)} has ambiguous target '{targetName}'.");
                return false;
            }

            report.Wired++;
            report.AddDetail($"{Describe(component, field)} -> {DescribeObject(match.Value)}");
            if (!options.DryRun)
            {
                property.objectReferenceValue = match.Value;
            }

            return true;
        }

        private static bool WireArrayField(
            MonoBehaviour component,
            FieldInfo field,
            SerializedProperty property,
            Type elementType,
            string targetName,
            bool hasAttribute,
            List<UnityEngine.Object> candidates,
            bool allowOverwrite,
            AutoWireOptions options,
            AutoWireReport report)
        {
            if (!property.isArray)
            {
                report.Unsupported++;
                report.AddDetail($"{Describe(component, field)} is not an array/list property.");
                return false;
            }

            if (!allowOverwrite && IsArrayFullyAssigned(property))
            {
                report.AlreadySet++;
                return false;
            }

            if (!hasAttribute)
            {
                report.Unsupported++;
                report.AddDetail($"{Describe(component, field)} is an array and needs [AutoWire(\"Name\")].");
                return false;
            }

            List<ScoredCandidate> matches = FindScoredCandidates(candidates, targetName, elementType);
            if (matches.Count == 0)
            {
                report.Missing++;
                report.AddDetail($"{Describe(component, field)} found no array targets named '{targetName}'.");
                return false;
            }

            matches.Sort((left, right) => CompareArrayCandidates(left.Value, right.Value, targetName));
            report.Wired++;
            report.AddDetail($"{Describe(component, field)} -> {matches.Count} targets");
            if (!options.DryRun)
            {
                property.arraySize = matches.Count;
                for (int index = 0; index < matches.Count; index++)
                {
                    property.GetArrayElementAtIndex(index).objectReferenceValue = matches[index].Value;
                }
            }

            return true;
        }

        private static AutoWireMatch ResolveSingleCandidate(
            List<UnityEngine.Object> candidates,
            string targetName,
            Type elementType)
        {
            List<ScoredCandidate> scored = FindScoredCandidates(candidates, targetName, elementType);
            if (scored.Count == 0)
            {
                if (candidates.Count == 1)
                {
                    return AutoWireMatch.Found(candidates[0]);
                }

                return AutoWireMatch.None();
            }

            scored.Sort((left, right) => right.Score.CompareTo(left.Score));
            if (scored.Count > 1 && scored[0].Score == scored[1].Score)
            {
                return AutoWireMatch.Ambiguous();
            }

            return AutoWireMatch.Found(scored[0].Value);
        }

        private static List<ScoredCandidate> FindScoredCandidates(
            List<UnityEngine.Object> candidates,
            string targetName,
            Type elementType)
        {
            var scored = new List<ScoredCandidate>();
            for (int index = 0; index < candidates.Count; index++)
            {
                UnityEngine.Object candidate = candidates[index];
                int score = ScoreCandidate(candidate, targetName, elementType);
                if (score > 0)
                {
                    scored.Add(new ScoredCandidate(candidate, score));
                }
            }

            return scored;
        }

        private static int ScoreCandidate(
            UnityEngine.Object candidate,
            string targetName,
            Type elementType)
        {
            string candidateName = GetCandidateName(candidate);
            string candidateNormalized = Normalize(candidateName);
            string targetNormalized = Normalize(targetName);
            if (candidateNormalized.Length == 0 || targetNormalized.Length == 0)
            {
                return 0;
            }

            if (candidateNormalized == targetNormalized)
            {
                return 1000;
            }

            if (candidateNormalized.Contains(targetNormalized))
            {
                return 850;
            }

            string[] targetTokens = ToTokens(targetName);
            string[] candidateTokens = ToTokens(candidateName);
            if (ContainsTokensInOrder(candidateTokens, targetTokens))
            {
                return 760 + targetTokens.Length;
            }

            if (ContainsAllTokens(candidateTokens, targetTokens))
            {
                return 650 + targetTokens.Length;
            }

            string typeName = elementType != null ? ObjectNames.NicifyVariableName(elementType.Name) : string.Empty;
            if (!string.IsNullOrEmpty(typeName) &&
                ContainsTokensInOrder(candidateTokens, ToTokens(typeName)) &&
                ContainsAnyToken(candidateTokens, targetTokens))
            {
                return 520;
            }

            return 0;
        }

        private static List<UnityEngine.Object> FindCandidates(
            MonoBehaviour owner,
            Type elementType,
            AutoWireSearchScope scope,
            bool includeInactive)
        {
            var candidates = new List<UnityEngine.Object>();

            switch (scope)
            {
                case AutoWireSearchScope.Children:
                    CollectFromTransform(owner.transform, elementType, includeInactive, candidates);
                    break;
                case AutoWireSearchScope.Parents:
                    CollectFromParents(owner.transform, elementType, includeInactive, candidates);
                    break;
                case AutoWireSearchScope.OpenScenes:
                    for (int index = 0; index < SceneManager.sceneCount; index++)
                    {
                        CollectFromScene(SceneManager.GetSceneAt(index), elementType, includeInactive, candidates);
                    }
                    break;
                default:
                    CollectFromScene(owner.gameObject.scene, elementType, includeInactive, candidates);
                    break;
            }

            RemoveDuplicates(candidates);
            return candidates;
        }

        private static void CollectFromScene(
            Scene scene,
            Type elementType,
            bool includeInactive,
            List<UnityEngine.Object> candidates)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                CollectFromTransform(roots[index].transform, elementType, includeInactive, candidates);
            }
        }

        private static void CollectFromTransform(
            Transform root,
            Type elementType,
            bool includeInactive,
            List<UnityEngine.Object> candidates)
        {
            if (root == null)
            {
                return;
            }

            if (elementType == typeof(GameObject))
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive);
                for (int index = 0; index < transforms.Length; index++)
                {
                    candidates.Add(transforms[index].gameObject);
                }

                return;
            }

            if (!typeof(Component).IsAssignableFrom(elementType))
            {
                return;
            }

            Component[] components = root.GetComponentsInChildren(elementType, includeInactive);
            for (int index = 0; index < components.Length; index++)
            {
                candidates.Add(components[index]);
            }
        }

        private static void CollectFromParents(
            Transform origin,
            Type elementType,
            bool includeInactive,
            List<UnityEngine.Object> candidates)
        {
            if (origin == null)
            {
                return;
            }

            Transform current = origin;
            while (current != null)
            {
                if (includeInactive || current.gameObject.activeInHierarchy)
                {
                    if (elementType == typeof(GameObject))
                    {
                        candidates.Add(current.gameObject);
                    }
                    else if (typeof(Component).IsAssignableFrom(elementType))
                    {
                        Component component = current.GetComponent(elementType);
                        if (component != null)
                        {
                            candidates.Add(component);
                        }
                    }
                }

                current = current.parent;
            }
        }

        private static FieldInfo[] GetSerializableFields(Type type)
        {
            var fields = new List<FieldInfo>();
            while (type != null &&
                type != typeof(MonoBehaviour) &&
                IsProjectType(type))
            {
                FieldInfo[] declaredFields = type.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                for (int index = 0; index < declaredFields.Length; index++)
                {
                    FieldInfo field = declaredFields[index];
                    if (field.IsStatic ||
                        field.IsInitOnly ||
                        field.GetCustomAttribute<NonSerializedAttribute>() != null)
                    {
                        continue;
                    }

                    bool serialized = field.IsPublic ||
                        field.GetCustomAttribute<SerializeField>() != null ||
                        field.GetCustomAttribute<AutoWireAttribute>() != null;
                    if (serialized)
                    {
                        fields.Add(field);
                    }
                }

                type = type.BaseType;
            }

            return fields.ToArray();
        }

        private static bool IsProjectType(Type type)
        {
            string typeNamespace = type?.Namespace;
            return string.Equals(typeNamespace, ProjectNamespace, StringComparison.Ordinal) ||
                (typeNamespace != null &&
                    typeNamespace.StartsWith(ProjectNamespace + ".", StringComparison.Ordinal));
        }

        private static void RemoveOwnerCandidate(
            MonoBehaviour owner,
            List<UnityEngine.Object> candidates)
        {
            for (int index = candidates.Count - 1; index >= 0; index--)
            {
                if (ReferenceEquals(candidates[index], owner))
                {
                    candidates.RemoveAt(index);
                }
            }
        }

        private static bool TryGetObjectFieldKind(
            FieldInfo field,
            out Type elementType,
            out bool isArray)
        {
            Type fieldType = field.FieldType;
            elementType = fieldType;
            isArray = false;

            if (fieldType.IsArray)
            {
                elementType = fieldType.GetElementType();
                isArray = true;
            }
            else if (fieldType.IsGenericType &&
                fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                elementType = fieldType.GetGenericArguments()[0];
                isArray = true;
            }

            return elementType != null && typeof(UnityEngine.Object).IsAssignableFrom(elementType);
        }

        private static string ResolveTargetName(FieldInfo field, AutoWireAttribute attribute)
        {
            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.ObjectName))
            {
                return attribute.ObjectName;
            }

            return ObjectNames.NicifyVariableName(field.Name.TrimStart('_'));
        }

        private static string GetCandidateName(UnityEngine.Object candidate)
        {
            if (candidate is Component component)
            {
                return component.gameObject.name;
            }

            return candidate != null ? candidate.name : string.Empty;
        }

        private static string Describe(MonoBehaviour component, FieldInfo field)
        {
            return $"{component.GetType().Name}.{field.Name}";
        }

        private static string DescribeObject(UnityEngine.Object value)
        {
            if (value is Component component)
            {
                return $"{component.gameObject.name}/{component.GetType().Name}";
            }

            return value != null ? value.name : "null";
        }

        private static bool IsArrayFullyAssigned(SerializedProperty property)
        {
            if (property.arraySize == 0)
            {
                return false;
            }

            for (int index = 0; index < property.arraySize; index++)
            {
                if (property.GetArrayElementAtIndex(index).objectReferenceValue == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CompareArrayCandidates(
            UnityEngine.Object left,
            UnityEngine.Object right,
            string targetName)
        {
            int leftIndex = ExtractIndex(GetCandidateName(left), targetName);
            int rightIndex = ExtractIndex(GetCandidateName(right), targetName);
            if (leftIndex != rightIndex)
            {
                return leftIndex.CompareTo(rightIndex);
            }

            int siblingCompare = GetSiblingPath(left).CompareTo(GetSiblingPath(right));
            if (siblingCompare != 0)
            {
                return siblingCompare;
            }

            return string.Compare(GetCandidateName(left), GetCandidateName(right), StringComparison.Ordinal);
        }

        private static int ExtractIndex(string candidateName, string targetName)
        {
            if (string.Equals(candidateName, targetName, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            int parenStart = candidateName.LastIndexOf('(');
            int parenEnd = candidateName.LastIndexOf(')');
            if (parenStart >= 0 &&
                parenEnd > parenStart &&
                int.TryParse(candidateName.Substring(parenStart + 1, parenEnd - parenStart - 1), out int parenIndex))
            {
                return parenIndex;
            }

            int lastDigit = candidateName.Length - 1;
            while (lastDigit >= 0 && !char.IsDigit(candidateName[lastDigit]))
            {
                lastDigit--;
            }

            if (lastDigit < 0)
            {
                return int.MaxValue;
            }

            int firstDigit = lastDigit;
            while (firstDigit > 0 && char.IsDigit(candidateName[firstDigit - 1]))
            {
                firstDigit--;
            }

            return int.TryParse(candidateName.Substring(firstDigit, lastDigit - firstDigit + 1), out int trailingIndex)
                ? trailingIndex
                : int.MaxValue;
        }

        private static string GetSiblingPath(UnityEngine.Object candidate)
        {
            Transform transform = candidate is Component component
                ? component.transform
                : candidate is GameObject gameObject
                    ? gameObject.transform
                    : null;

            if (transform == null)
            {
                return string.Empty;
            }

            var indices = new Stack<int>();
            while (transform != null)
            {
                indices.Push(transform.GetSiblingIndex());
                transform = transform.parent;
            }

            return string.Join("/", indices);
        }

        private static void RemoveDuplicates(List<UnityEngine.Object> candidates)
        {
            var seen = new HashSet<int>();
            for (int index = candidates.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object candidate = candidates[index];
                if (candidate == null || !seen.Add(candidate.GetInstanceID()))
                {
                    candidates.RemoveAt(index);
                }
            }
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var chars = new List<char>(value.Length);
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (char.IsLetterOrDigit(character))
                {
                    chars.Add(char.ToLowerInvariant(character));
                }
            }

            return new string(chars.ToArray());
        }

        private static string[] ToTokens(string value)
        {
            string nicified = ObjectNames.NicifyVariableName(value.TrimStart('_'));
            string[] rawTokens = nicified.Split(
                new[] { ' ', '_', '-', '/', '.', '(', ')' },
                StringSplitOptions.RemoveEmptyEntries);
            var tokens = new List<string>(rawTokens.Length);
            for (int index = 0; index < rawTokens.Length; index++)
            {
                string token = Normalize(rawTokens[index]);
                if (!string.IsNullOrEmpty(token))
                {
                    tokens.Add(token);
                }
            }

            return tokens.ToArray();
        }

        private static bool ContainsTokensInOrder(string[] candidateTokens, string[] targetTokens)
        {
            if (targetTokens.Length == 0)
            {
                return false;
            }

            int targetIndex = 0;
            for (int index = 0; index < candidateTokens.Length && targetIndex < targetTokens.Length; index++)
            {
                if (candidateTokens[index].Contains(targetTokens[targetIndex]) ||
                    targetTokens[targetIndex].Contains(candidateTokens[index]))
                {
                    targetIndex++;
                }
            }

            return targetIndex == targetTokens.Length;
        }

        private static bool ContainsAllTokens(string[] candidateTokens, string[] targetTokens)
        {
            if (targetTokens.Length == 0)
            {
                return false;
            }

            for (int targetIndex = 0; targetIndex < targetTokens.Length; targetIndex++)
            {
                bool found = false;
                for (int candidateIndex = 0; candidateIndex < candidateTokens.Length; candidateIndex++)
                {
                    if (candidateTokens[candidateIndex].Contains(targetTokens[targetIndex]) ||
                        targetTokens[targetIndex].Contains(candidateTokens[candidateIndex]))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsAnyToken(string[] candidateTokens, string[] targetTokens)
        {
            for (int index = 0; index < targetTokens.Length; index++)
            {
                for (int candidateIndex = 0; candidateIndex < candidateTokens.Length; candidateIndex++)
                {
                    if (candidateTokens[candidateIndex].Contains(targetTokens[index]) ||
                        targetTokens[index].Contains(candidateTokens[candidateIndex]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void QueueAutoRun()
        {
            if (!EditorPrefs.GetBool(AutoRunPreferenceKey, false) ||
                _autoRunQueued ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling)
            {
                return;
            }

            _autoRunQueued = true;
            EditorApplication.delayCall += RunQueuedAutoWire;
        }

        private static void RunQueuedAutoWire()
        {
            _autoRunQueued = false;
            if (!EditorPrefs.GetBool(AutoRunPreferenceKey, false) ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling)
            {
                return;
            }

            var report = WireScene(
                SceneManager.GetActiveScene(),
                AutoWireOptions.Execute("Hierarchy Change", useHeuristics: false, quiet: true));
            if (report.Wired > 0 || report.Ambiguous > 0)
            {
                report.Log();
            }
        }

        private readonly struct AutoWireOptions
        {
            private AutoWireOptions(
                string context,
                bool dryRun,
                bool useHeuristics,
                bool allowOverwrite,
                bool quiet)
            {
                Context = context;
                DryRun = dryRun;
                UseHeuristics = useHeuristics;
                AllowOverwrite = allowOverwrite;
                Quiet = quiet;
            }

            public string Context { get; }
            public bool DryRun { get; }
            public bool UseHeuristics { get; }
            public bool AllowOverwrite { get; }
            public bool Quiet { get; }

            public static AutoWireOptions Execute(
                string context,
                bool useHeuristics,
                bool quiet = false)
            {
                return new AutoWireOptions(context, false, useHeuristics, false, quiet);
            }

            public static AutoWireOptions CreateDryRun(string context, bool useHeuristics)
            {
                return new AutoWireOptions(context, true, useHeuristics, false, false);
            }
        }

        private sealed class AutoWireReport
        {
            private readonly List<string> _details = new();

            public AutoWireReport(string context)
            {
                Context = context;
            }

            public string Context { get; }
            public int Scanned { get; set; }
            public int Wired { get; set; }
            public int AlreadySet { get; set; }
            public int Missing { get; set; }
            public int Ambiguous { get; set; }
            public int Unsupported { get; set; }

            public void Merge(AutoWireReport other)
            {
                if (other == null)
                {
                    return;
                }

                Scanned += other.Scanned;
                Wired += other.Wired;
                AlreadySet += other.AlreadySet;
                Missing += other.Missing;
                Ambiguous += other.Ambiguous;
                Unsupported += other.Unsupported;
                for (int index = 0; index < other._details.Count; index++)
                {
                    AddDetail(other._details[index]);
                }
            }

            public void AddDetail(string detail)
            {
                if (_details.Count < MaxDetailLines)
                {
                    _details.Add(detail);
                }
            }

            public void Log()
            {
                string summary =
                    $"[AutoWire] {Context}: scanned {Scanned}, wired {Wired}, already set {AlreadySet}, " +
                    $"missing {Missing}, ambiguous {Ambiguous}, unsupported {Unsupported}.";
                if (_details.Count == 0)
                {
                    Debug.Log(summary);
                    return;
                }

                Debug.Log(summary + "\n" + string.Join("\n", _details));
            }
        }

        private readonly struct ScoredCandidate
        {
            public ScoredCandidate(UnityEngine.Object value, int score)
            {
                Value = value;
                Score = score;
            }

            public UnityEngine.Object Value { get; }
            public int Score { get; }
        }

        private readonly struct AutoWireMatch
        {
            private AutoWireMatch(AutoWireMatchStatus status, UnityEngine.Object value)
            {
                Status = status;
                Value = value;
            }

            public AutoWireMatchStatus Status { get; }
            public UnityEngine.Object Value { get; }

            public static AutoWireMatch Found(UnityEngine.Object value)
            {
                return new AutoWireMatch(AutoWireMatchStatus.Found, value);
            }

            public static AutoWireMatch None()
            {
                return new AutoWireMatch(AutoWireMatchStatus.None, null);
            }

            public static AutoWireMatch Ambiguous()
            {
                return new AutoWireMatch(AutoWireMatchStatus.Ambiguous, null);
            }
        }

        private enum AutoWireMatchStatus
        {
            None,
            Found,
            Ambiguous
        }
    }
}
