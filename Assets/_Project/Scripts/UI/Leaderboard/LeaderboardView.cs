using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using R3;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SlotRogue.UI.Leaderboard
{
    public sealed class LeaderboardView : MonoBehaviour
    {
        [SerializeField] private Button _openButton;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private GameObject _entriesViewport;
        [SerializeField] private Transform _entriesContainer;
        [SerializeField] private Text _entriesText;
        [SerializeField] private TMP_Text _entriesTmpText;
        [SerializeField] private Text _statusText;
        [SerializeField] private TMP_Text _statusTmpText;
        [SerializeField] private InputField _nameInput;
        [SerializeField] private Button _saveProfileButton;
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private Text _detailTitleText;
        [SerializeField] private TMP_Text _detailTitleTmpText;
        [SerializeField] private Text _detailText;
        [SerializeField] private TMP_Text _detailTmpText;
        [SerializeField] private Button _detailCloseButton;
        [SerializeField] private LeaderboardEntryRowBinding[] _entryRows =
            Array.Empty<LeaderboardEntryRowBinding>();

        private readonly List<RowClickBinding> _rowClickBindings = new();
        private UnityAction _detailCloseAction;
        private bool _detailCloseSubscribed;
        private bool _subscribed;
        private bool _showLauncher = true;

        public event Action OpenRequested;

        public event Action CloseRequested;

        public event Action RefreshRequested;

        /// <summary>
        /// 자기 ViewModel을 구독(상태→Render)하고 close/refresh 입력을 ViewModel command로 연결한다(ADR-0020).
        /// launcher의 OpenRequested는 씬마다 진입 경로가 달라 소유자(SceneRoot)가 연결한다.
        /// </summary>
        public void Bind(LeaderboardViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            CloseRequested += viewModel.Close;
            RefreshRequested += () => viewModel.RefreshAsync().Forget();
            viewModel.State.Subscribe(Render).AddTo(this);
        }

        public bool EnsureRuntimeLayout()
        {
            ResolvePlacedReferences();
            HideProfileEditControls();

            if (!HasRequiredReferences())
            {
                Debug.LogError(
                    "[LeaderboardView] Leaderboard Open Button, Leaderboard Panel, and Close Button must be placed in the hierarchy.");
                return false;
            }

            SubscribeButtons();
            return true;
        }

        public void SetLauncherVisible(bool isVisible)
        {
            _showLauncher = isVisible;
            if (_openButton != null && (_panel == null || !_panel.activeSelf))
            {
                _openButton.gameObject.SetActive(_showLauncher);
            }
        }

        public void Render(LeaderboardViewState state)
        {
            if (!EnsureRuntimeLayout())
            {
                return;
            }

            bool isProfileRequired = state?.IsProfileRequired == true;
            bool isVisible = state?.IsVisible == true && !isProfileRequired;

            _panel.SetActive(isVisible);
            _openButton.gameObject.SetActive(
                !isVisible &&
                !isProfileRequired &&
                _showLauncher);

            if (!isVisible || state == null)
            {
                HideDetailPanel();
                return;
            }

            _panel.transform.SetAsLastSibling();
            RenderEntries(state.Entries);
            SetText(_statusText, _statusTmpText, state.StatusMessage);
            SetInteractable(_refreshButton, !state.IsLoading);
        }

        private void OnDestroy()
        {
            UnsubscribeButtons();
            UnsubscribeRowButtons();
            UnsubscribeDetailCloseButton();
        }

        private void ResolvePlacedReferences()
        {
            _openButton ??= FindButton("Leaderboard Open Button", "Ranking Button");
            _panel ??= FindDescendant("Leaderboard Panel")?.gameObject ??
                FindDescendant("Ranking Panel")?.gameObject;
            _closeButton ??= FindButton("Close Button", "Leaderboard Close Button");
            _refreshButton ??= FindButton("Refresh Button", "Leaderboard Refresh Button");
            _entriesViewport ??= FindDescendant("Leaderboard Entries Viewport")?.gameObject ??
                FindDescendant("Leaderboard List Viewport")?.gameObject;
            _entriesContainer ??= FindDescendant("Leaderboard Entries") ??
                FindDescendant("Leaderboard Entry List") ??
                FindDescendant("Ranking Entry List") ??
                _entriesViewport?.transform;
            _entriesText ??= FindDescendant("Leaderboard Entries")?.GetComponent<Text>();
            _entriesTmpText ??= FindDescendant("Leaderboard Entries")?.GetComponent<TMP_Text>();
            _statusText ??= FindDescendant("Leaderboard Status")?.GetComponent<Text>();
            _statusTmpText ??= FindDescendant("Leaderboard Status")?.GetComponent<TMP_Text>();
            _nameInput ??= FindDescendant("Player Name Input")?.GetComponent<InputField>() ??
                FindDescendant("Nickname Input")?.GetComponent<InputField>();
            _saveProfileButton ??= FindButton("Save Name Button", "Save Profile Button");
            _detailPanel ??= FindDescendant("Leaderboard Detail Panel")?.gameObject ??
                FindDescendant("Entry Detail Panel")?.gameObject ??
                FindDescendant("Detail Panel")?.gameObject;
            _detailTitleText ??= FindText("Leaderboard Detail Title", "Detail Title");
            _detailTitleTmpText ??= FindTmpText("Leaderboard Detail Title", "Detail Title");
            _detailText ??= FindText("Leaderboard Detail Text", "Detail Text");
            _detailTmpText ??= FindTmpText("Leaderboard Detail Text", "Detail Text");
            _detailCloseButton ??= FindButton("Detail Close Button", "Close Detail Button");

            ResolveEntryRows();

            if (_detailPanel != null)
            {
                _detailPanel.SetActive(false);
            }
        }

        private bool HasRequiredReferences()
        {
            return _openButton != null &&
                _panel != null &&
                _closeButton != null;
        }

        private void HideProfileEditControls()
        {
            if (_nameInput != null)
            {
                _nameInput.gameObject.SetActive(false);
            }

            if (_saveProfileButton != null)
            {
                _saveProfileButton.gameObject.SetActive(false);
            }

            if (_refreshButton != null)
            {
                _refreshButton.gameObject.SetActive(false);
            }
        }

        private void ResolveEntryRows()
        {
            if (HasSerializedRows())
            {
                for (int index = 0; index < _entryRows.Length; index++)
                {
                    _entryRows[index]?.Resolve();
                }

                return;
            }

            var rowRoots = new List<Transform>();
            CollectEntryRowsFromContainer(rowRoots);
            if (rowRoots.Count == 0)
            {
                CollectEntryRowsByName(rowRoots);
            }

            _entryRows = new LeaderboardEntryRowBinding[rowRoots.Count];
            for (int index = 0; index < rowRoots.Count; index++)
            {
                _entryRows[index] = new LeaderboardEntryRowBinding(rowRoots[index]);
                _entryRows[index].Resolve();
            }
        }

        private bool HasSerializedRows()
        {
            if (_entryRows == null || _entryRows.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < _entryRows.Length; index++)
            {
                if (_entryRows[index]?.Root != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void CollectEntryRowsFromContainer(List<Transform> rows)
        {
            if (_entriesContainer == null)
            {
                return;
            }

            for (int index = 0; index < _entriesContainer.childCount; index++)
            {
                Transform child = _entriesContainer.GetChild(index);
                if (IsLegacyEntriesText(child))
                {
                    continue;
                }

                if (LooksLikeEntryRow(child))
                {
                    rows.Add(child);
                }
            }
        }

        private void CollectEntryRowsByName(List<Transform> rows)
        {
            Transform searchRoot = _panel != null ? _panel.transform : transform;
            Transform[] descendants =
                searchRoot.GetComponentsInChildren<Transform>(includeInactive: true);
            for (int index = 0; index < descendants.Length; index++)
            {
                Transform candidate = descendants[index];
                if (candidate == searchRoot ||
                    rows.Contains(candidate) ||
                    IsLegacyEntriesText(candidate) ||
                    !LooksLikeEntryRow(candidate))
                {
                    continue;
                }

                rows.Add(candidate);
            }
        }

        private bool LooksLikeEntryRow(Transform candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            string name = candidate.name;
            bool rowName =
                ContainsOrdinalIgnoreCase(name, "row") ||
                ContainsOrdinalIgnoreCase(name, "entry") ||
                ContainsOrdinalIgnoreCase(name, "ranker") ||
                ContainsOrdinalIgnoreCase(name, "ranking item");
            if (!rowName)
            {
                return false;
            }

            if (ContainsOrdinalIgnoreCase(name, "viewport") ||
                ContainsOrdinalIgnoreCase(name, "text") ||
                ContainsOrdinalIgnoreCase(name, "title") ||
                ContainsOrdinalIgnoreCase(name, "status"))
            {
                return false;
            }

            return candidate.GetComponentInChildren<Button>(includeInactive: true) != null ||
                candidate.GetComponentsInChildren<Text>(includeInactive: true).Length > 0 ||
                candidate.GetComponentsInChildren<TMP_Text>(includeInactive: true).Length > 0;
        }

        private bool IsLegacyEntriesText(Transform candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            return (_entriesText != null && candidate == _entriesText.transform) ||
                (_entriesTmpText != null && candidate == _entriesTmpText.transform);
        }

        private void RenderEntries(IReadOnlyList<LeaderboardEntryData> entries)
        {
            IReadOnlyList<LeaderboardEntryData> safeEntries =
                entries ?? Array.Empty<LeaderboardEntryData>();
            UnsubscribeRowButtons();

            if (_entryRows != null && _entryRows.Length > 0)
            {
                SetText(_entriesText, _entriesTmpText, string.Empty);
                for (int index = 0; index < _entryRows.Length; index++)
                {
                    LeaderboardEntryRowBinding row = _entryRows[index];
                    if (row == null || row.Root == null)
                    {
                        continue;
                    }

                    if (index >= safeEntries.Count)
                    {
                        row.Root.SetActive(false);
                        continue;
                    }

                    LeaderboardEntryData entry = safeEntries[index];
                    row.Render(entry);
                    SubscribeRowButton(row, entry);
                }

                return;
            }

            SetText(_entriesText, _entriesTmpText, BuildEntriesText(safeEntries));
        }

        private void SubscribeButtons()
        {
            if (_subscribed)
            {
                return;
            }

            _openButton.onClick.AddListener(HandleOpenClicked);
            _closeButton.onClick.AddListener(HandleCloseClicked);
            if (_refreshButton != null)
            {
                _refreshButton.onClick.AddListener(HandleRefreshClicked);
            }

            SubscribeDetailCloseButton();
            _subscribed = true;
        }

        private void UnsubscribeButtons()
        {
            if (!_subscribed)
            {
                return;
            }

            if (_openButton != null)
            {
                _openButton.onClick.RemoveListener(HandleOpenClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            if (_refreshButton != null)
            {
                _refreshButton.onClick.RemoveListener(HandleRefreshClicked);
            }

            _subscribed = false;
        }

        private void SubscribeRowButton(
            LeaderboardEntryRowBinding row,
            LeaderboardEntryData entry)
        {
            if (row.DetailButton == null)
            {
                return;
            }

            UnityAction action = () => ShowEntryDetails(entry);
            row.DetailButton.onClick.AddListener(action);
            _rowClickBindings.Add(new RowClickBinding(row.DetailButton, action));
        }

        private void UnsubscribeRowButtons()
        {
            for (int index = 0; index < _rowClickBindings.Count; index++)
            {
                RowClickBinding binding = _rowClickBindings[index];
                if (binding.Button != null)
                {
                    binding.Button.onClick.RemoveListener(binding.Action);
                }
            }

            _rowClickBindings.Clear();
        }

        private void SubscribeDetailCloseButton()
        {
            if (_detailCloseSubscribed || _detailCloseButton == null)
            {
                return;
            }

            _detailCloseAction = HideDetailPanel;
            _detailCloseButton.onClick.AddListener(_detailCloseAction);
            _detailCloseSubscribed = true;
        }

        private void UnsubscribeDetailCloseButton()
        {
            if (!_detailCloseSubscribed || _detailCloseButton == null)
            {
                return;
            }

            _detailCloseButton.onClick.RemoveListener(_detailCloseAction);
            _detailCloseSubscribed = false;
        }

        private void HandleOpenClicked()
        {
            OpenRequested?.Invoke();
        }

        private void HandleRefreshClicked()
        {
            RefreshRequested?.Invoke();
        }

        private void HandleCloseClicked()
        {
            HideDetailPanel();
            CloseRequested?.Invoke();
        }

        private void ShowEntryDetails(LeaderboardEntryData entry)
        {
            string detail = BuildEntryDetails(entry);
            if (_detailPanel != null)
            {
                _detailPanel.SetActive(true);
                _detailPanel.transform.SetAsLastSibling();
                SetText(_detailTitleText, _detailTitleTmpText, entry.PlayerName);
                SetText(_detailText, _detailTmpText, detail);
                return;
            }

            SetText(_statusText, _statusTmpText, detail);
        }

        private void HideDetailPanel()
        {
            if (_detailPanel != null)
            {
                _detailPanel.SetActive(false);
            }
        }

        private static string BuildEntriesText(IReadOnlyList<LeaderboardEntryData> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (int index = 0; index < entries.Count; index++)
            {
                LeaderboardEntryData entry = entries[index];
                if (index > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine();
                }

                builder.Append(entry.IsCurrentPlayer ? "* " : string.Empty);
                builder.Append('#');
                builder.Append(entry.Rank);
                builder.Append(' ');
                builder.Append(entry.PlayerName);
                builder.AppendLine();
                builder.Append("Wave : ");
                builder.Append(entry.Wave);
                if (!string.IsNullOrWhiteSpace(entry.Message))
                {
                    builder.Append("  ");
                    builder.Append(entry.Message);
                }
            }

            return builder.ToString();
        }

        private static string BuildEntryDetails(LeaderboardEntryData entry)
        {
            var builder = new StringBuilder();
            builder.Append(entry.PlayerName);
            builder.Append(" / Wave : ");
            builder.Append(entry.Wave);
            builder.AppendLine();
            builder.AppendLine();

            builder.AppendLine("유물");
            if (entry.RelicIds == null || entry.RelicIds.Count == 0)
            {
                builder.AppendLine("- 없음");
            }
            else
            {
                for (int index = 0; index < entry.RelicIds.Count; index++)
                {
                    string relicId = entry.RelicIds[index];
                    RelicDefinition relic = RelicCatalog.GetById(relicId);
                    builder.Append("- ");
                    builder.Append(string.IsNullOrWhiteSpace(relic?.Name)
                        ? relicId
                        : relic.Name);
                    builder.AppendLine();
                }
            }

            builder.AppendLine();
            builder.AppendLine("심볼");
            if (entry.SymbolCounts == null || entry.SymbolCounts.Count == 0)
            {
                builder.AppendLine("- 기록 없음");
            }
            else
            {
                for (int index = 0; index < entry.SymbolCounts.Count; index++)
                {
                    LeaderboardSymbolCount count = entry.SymbolCounts[index];
                    if (count == null || count.Count <= 0)
                    {
                        continue;
                    }

                    builder.Append("- ");
                    builder.Append(FormatSymbolName(count));
                    builder.Append(" x");
                    builder.Append(count.Count);
                    builder.AppendLine();
                }
            }

            return builder.ToString().TrimEnd();
        }

        private static string FormatSymbolName(LeaderboardSymbolCount count)
        {
            return count != null && count.TryGetSymbol(out SlotSymbolType symbol)
                ? RelicDisplay.SymbolKorean(symbol)
                : count?.Symbol ?? string.Empty;
        }

        private Button FindButton(params string[] names)
        {
            Transform found = FindDescendant(names);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private Text FindText(params string[] names)
        {
            Transform found = FindDescendant(names);
            return found != null ? found.GetComponent<Text>() : null;
        }

        private TMP_Text FindTmpText(params string[] names)
        {
            Transform found = FindDescendant(names);
            return found != null ? found.GetComponent<TMP_Text>() : null;
        }

        private Transform FindDescendant(params string[] names)
        {
            Transform[] descendants =
                GetComponentsInChildren<Transform>(includeInactive: true);
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                string expected = names[nameIndex];
                for (int index = 0; index < descendants.Length; index++)
                {
                    Transform descendant = descendants[index];
                    if (string.Equals(
                            descendant.name,
                            expected,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return descendant;
                    }
                }
            }

            return null;
        }

        private static Text FindTextInChildren(Transform root, params string[] names)
        {
            Transform found = FindNamedChild(root, names);
            return found != null
                ? found.GetComponent<Text>()
                : FindComponentByName<Text>(root, names);
        }

        private static TMP_Text FindTmpTextInChildren(Transform root, params string[] names)
        {
            Transform found = FindNamedChild(root, names);
            return found != null
                ? found.GetComponent<TMP_Text>()
                : FindComponentByName<TMP_Text>(root, names);
        }

        private static Button FindButtonInChildren(Transform root, params string[] names)
        {
            Transform found = FindNamedChild(root, names);
            return found != null
                ? found.GetComponent<Button>()
                : FindComponentByName<Button>(root, names);
        }

        private static Image FindImageInChildren(Transform root, params string[] names)
        {
            Transform found = FindNamedChild(root, names);
            return found != null
                ? found.GetComponent<Image>()
                : FindComponentByName<Image>(root, names);
        }

        private static Transform FindNamedChild(Transform root, params string[] names)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] descendants =
                root.GetComponentsInChildren<Transform>(includeInactive: true);
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                string expected = names[nameIndex];
                for (int index = 0; index < descendants.Length; index++)
                {
                    if (string.Equals(
                            descendants[index].name,
                            expected,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return descendants[index];
                    }
                }
            }

            return null;
        }

        private static T FindComponentByName<T>(Transform root, params string[] names)
            where T : Component
        {
            if (root == null)
            {
                return null;
            }

            T[] components = root.GetComponentsInChildren<T>(includeInactive: true);
            for (int index = 0; index < components.Length; index++)
            {
                string objectName = components[index].gameObject.name;
                for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    if (ContainsOrdinalIgnoreCase(objectName, names[nameIndex]))
                    {
                        return components[index];
                    }
                }
            }

            return null;
        }

        private static void SetText(Text text, TMP_Text tmpText, string value)
        {
            string safeValue = value ?? string.Empty;
            if (text != null)
            {
                text.text = safeValue;
            }

            if (tmpText != null)
            {
                tmpText.text = safeValue;
            }
        }

        private static void SetInteractable(Selectable selectable, bool interactable)
        {
            if (selectable != null)
            {
                selectable.interactable = interactable;
            }
        }

        private static bool ContainsOrdinalIgnoreCase(string value, string part)
        {
            return !string.IsNullOrEmpty(value) &&
                !string.IsNullOrEmpty(part) &&
                value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        [Serializable]
        private sealed class LeaderboardEntryRowBinding
        {
            [SerializeField] internal GameObject Root;
            [SerializeField] internal Text PlayerNameText;
            [SerializeField] internal TMP_Text PlayerNameTmpText;
            [SerializeField] internal Text WaveText;
            [SerializeField] internal TMP_Text WaveTmpText;
            [SerializeField] internal Text MessageText;
            [SerializeField] internal TMP_Text MessageTmpText;
            [SerializeField] internal Button DetailButton;
            [SerializeField] internal Image ProfileImage;

            internal LeaderboardEntryRowBinding()
            {
            }

            internal LeaderboardEntryRowBinding(Transform root)
            {
                Root = root != null ? root.gameObject : null;
            }

            internal void Resolve()
            {
                if (Root == null)
                {
                    return;
                }

                Transform root = Root.transform;
                PlayerNameText ??= FindTextInChildren(
                    root,
                    "Player Name",
                    "Nickname",
                    "Profile Name",
                    "Name Text");
                PlayerNameTmpText ??= FindTmpTextInChildren(
                    root,
                    "Player Name",
                    "Nickname",
                    "Profile Name",
                    "Name Text");
                WaveText ??= FindTextInChildren(
                    root,
                    "Wave",
                    "Max Wave",
                    "Wave Text",
                    "Best Wave");
                WaveTmpText ??= FindTmpTextInChildren(
                    root,
                    "Wave",
                    "Max Wave",
                    "Wave Text",
                    "Best Wave");
                MessageText ??= FindTextInChildren(
                    root,
                    "Message",
                    "Comment",
                    "Speech",
                    "Bubble",
                    "Phrase",
                    "Ment");
                MessageTmpText ??= FindTmpTextInChildren(
                    root,
                    "Message",
                    "Comment",
                    "Speech",
                    "Bubble",
                    "Phrase",
                    "Ment");
                DetailButton ??= FindButtonInChildren(
                    root,
                    "Detail Button",
                    "Details Button",
                    "Info Button",
                    "Inspect Button",
                    "Left Button");
                ProfileImage ??= FindImageInChildren(
                    root,
                    "Profile Image",
                    "Profile Icon",
                    "Portrait",
                    "Avatar");
            }

            internal void Render(LeaderboardEntryData entry)
            {
                Root.SetActive(true);
                SetText(PlayerNameText, PlayerNameTmpText, entry.PlayerName);
                SetText(WaveText, WaveTmpText, $"Wave : {entry.Wave}");
                SetText(MessageText, MessageTmpText, entry.Message);

                if (ProfileImage != null)
                {
                    ProfileImage.enabled = true;
                }
            }
        }

        private readonly struct RowClickBinding
        {
            internal RowClickBinding(Button button, UnityAction action)
            {
                Button = button;
                Action = action;
            }

            internal Button Button { get; }

            internal UnityAction Action { get; }
        }
    }
}
