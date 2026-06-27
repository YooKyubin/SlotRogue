using System;
using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Leaderboard
{
    /// <summary>
    /// 웨이브 랭킹 화면 View입니다. TMP 전용이며, 하이어라키에 배치된
    /// Top-3 포디움(Gold/Silver/Bronze) + 리스트 행(RankingPanel) + 내 기록(MyRankPanel)을 바인딩합니다.
    /// 상태 구독·close 입력은 Bind가 소유합니다(ADR-0020).
    /// </summary>
    public sealed class LeaderboardView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;
        [Tooltip("Scroll View > Viewport > Content")]
        [SerializeField] private Transform _rowsContainer;
        [Tooltip("4위부터 채울 행(RankingPanel) 프리팹. 비우면 Content의 첫 행을 템플릿으로 사용.")]
        [SerializeField] private GameObject _rowPrefab;
        [SerializeField] private RankRowBinding _goldRow = new();
        [SerializeField] private RankRowBinding _silverRow = new();
        [SerializeField] private RankRowBinding _bronzeRow = new();
        [SerializeField] private RankRowBinding _myRankRow = new();

        // 리스트 행은 프리팹을 Content에 인스턴스화해 재사용(풀링)한다. 100위까지 동적 생성.
        private readonly List<RankRowBinding> _rowPool = new();
        private GameObject _rowTemplate;
        private bool _poolInitialized;
        private bool _subscribed;
        private bool _showLauncher = true;

        public event Action OpenRequested;

        public event Action CloseRequested;

        /// <summary>
        /// 자기 ViewModel을 구독(상태→Render)하고 close 입력을 ViewModel command로 연결한다(ADR-0020).
        /// launcher의 OpenRequested는 씬마다 진입 경로가 달라 소유자(SceneRoot)가 연결한다.
        /// </summary>
        public void Bind(LeaderboardViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            CloseRequested += viewModel.Close;
            viewModel.State.Subscribe(Render).AddTo(this);
        }

        public bool EnsureRuntimeLayout()
        {
            ResolvePlacedReferences();

            if (_panel == null || _closeButton == null)
            {
                Debug.LogError(
                    "[LeaderboardView] Leaderboard Content panel and Close Button must be placed in the hierarchy.");
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
            if (_openButton != null)
            {
                _openButton.gameObject.SetActive(
                    !isVisible && !isProfileRequired && _showLauncher);
            }

            if (!isVisible || state == null)
            {
                return;
            }

            _panel.transform.SetAsLastSibling();
            RenderEntries(state.Entries);
        }

        private void RenderEntries(IReadOnlyList<LeaderboardEntryData> entries)
        {
            IReadOnlyList<LeaderboardEntryData> safe =
                entries ?? Array.Empty<LeaderboardEntryData>();

            // 포디움: 1/2/3위.
            RenderPodium(_goldRow, safe, 0);
            RenderPodium(_silverRow, safe, 1);
            RenderPodium(_bronzeRow, safe, 2);

            // 리스트: 4위부터 끝까지(최대 100위) 프리팹 인스턴스에 채운다.
            int listCount = Mathf.Max(0, safe.Count - 3);
            EnsureRowPool(listCount);
            for (int poolIndex = 0; poolIndex < _rowPool.Count; poolIndex++)
            {
                if (poolIndex < listCount)
                {
                    _rowPool[poolIndex].Render(safe[poolIndex + 3]);
                }
                else
                {
                    _rowPool[poolIndex].Hide();
                }
            }

            // 내 기록.
            RenderMyRank(safe);
        }

        private static void RenderPodium(
            RankRowBinding row,
            IReadOnlyList<LeaderboardEntryData> entries,
            int index)
        {
            if (index < entries.Count)
            {
                row.Render(entries[index]);
            }
            else
            {
                row.Hide();
            }
        }

        private void RenderMyRank(IReadOnlyList<LeaderboardEntryData> entries)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].IsCurrentPlayer)
                {
                    _myRankRow.Render(entries[index]);
                    return;
                }
            }

            _myRankRow.Hide();
        }

        private void OnDestroy()
        {
            UnsubscribeButtons();
        }

        private void ResolvePlacedReferences()
        {
            _panel ??= FindDescendant("Leaderboard Content", "Leaderboard Panel")?.gameObject;
            _openButton ??= FindButton("Leaderboard Open Button", "Ranking Button");
            _closeButton ??= FindButton("Close Button", "Leaderboard Close Button");
            _rowsContainer ??= FindDescendant("Content");

            ResolvePodium(_goldRow, "GoldRankPanel");
            ResolvePodium(_silverRow, "SilverRankPanel");
            ResolvePodium(_bronzeRow, "BronzeRankPanel");
            ResolvePodium(_myRankRow, "MyRankPanel");
            InitRowPool();
        }

        private void ResolvePodium(RankRowBinding row, string panelName)
        {
            if (row.Root == null)
            {
                Transform panel = FindDescendant(panelName);
                if (panel != null)
                {
                    row.SetRoot(panel.gameObject);
                }
            }

            row.Resolve();
        }

        // Content의 첫 행을 템플릿으로 잡고(또는 _rowPrefab), 기존 자식은 모두 비활성화한다.
        private void InitRowPool()
        {
            if (_poolInitialized)
            {
                return;
            }

            _rowTemplate = ResolveRowTemplate();

            if (_rowsContainer != null)
            {
                for (int index = 0; index < _rowsContainer.childCount; index++)
                {
                    _rowsContainer.GetChild(index).gameObject.SetActive(false);
                }
            }

            _poolInitialized = true;
        }

        private GameObject ResolveRowTemplate()
        {
            if (_rowPrefab != null)
            {
                return _rowPrefab;
            }

            if (_rowsContainer == null)
            {
                return null;
            }

            for (int index = 0; index < _rowsContainer.childCount; index++)
            {
                Transform child = _rowsContainer.GetChild(index);
                if (ContainsOrdinalIgnoreCase(child.name, "RankingPanel") ||
                    ContainsOrdinalIgnoreCase(child.name, "Row") ||
                    ContainsOrdinalIgnoreCase(child.name, "Entry"))
                {
                    return child.gameObject;
                }
            }

            return _rowsContainer.childCount > 0
                ? _rowsContainer.GetChild(0).gameObject
                : null;
        }

        // 필요한 개수만큼 행 인스턴스를 만들어 풀에 채운다(재사용 — refresh마다 파괴/재생성 안 함).
        private void EnsureRowPool(int count)
        {
            if (count <= _rowPool.Count)
            {
                return;
            }

            if (_rowTemplate == null || _rowsContainer == null)
            {
                Debug.LogError(
                    "[LeaderboardView] Ranking row prefab/template was not found; cannot build the list.");
                return;
            }

            while (_rowPool.Count < count)
            {
                GameObject clone = Instantiate(_rowTemplate, _rowsContainer);
                clone.name = $"RankingPanel ({_rowPool.Count})";
                var binding = new RankRowBinding(clone.transform);
                binding.Resolve();
                _rowPool.Add(binding);
            }
        }

        private void SubscribeButtons()
        {
            if (_subscribed)
            {
                return;
            }

            if (_openButton != null)
            {
                _openButton.onClick.AddListener(HandleOpenClicked);
            }

            _closeButton.onClick.AddListener(HandleCloseClicked);
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

            _subscribed = false;
        }

        private void HandleOpenClicked()
        {
            OpenRequested?.Invoke();
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        private Button FindButton(params string[] names)
        {
            Transform found = FindDescendant(names);
            return found != null ? found.GetComponent<Button>() : null;
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

        private static bool ContainsOrdinalIgnoreCase(string value, string part)
        {
            return !string.IsNullOrEmpty(value) &&
                !string.IsNullOrEmpty(part) &&
                value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 포디움/리스트/내기록 공용 행 바인딩(TMP 전용).
        /// DesignationText([칭호])는 데이터가 없어 건드리지 않는다(프리팹 placeholder 유지).
        /// </summary>
        [Serializable]
        private sealed class RankRowBinding
        {
            [SerializeField] internal GameObject Root;
            [SerializeField] internal TMP_Text RankText;
            [SerializeField] internal TMP_Text DesignationText;
            [SerializeField] internal TMP_Text NameText;
            [SerializeField] internal TMP_Text WaveText;
            [SerializeField] internal Image ProfileImage;

            internal RankRowBinding()
            {
            }

            internal RankRowBinding(Transform root)
            {
                Root = root != null ? root.gameObject : null;
            }

            internal void SetRoot(GameObject root)
            {
                Root = root;
            }

            internal void Resolve()
            {
                if (Root == null)
                {
                    return;
                }

                Transform root = Root.transform;
                RankText ??= FindTmp(root, "RankText", "Rank Text", "Text (TMP)");
                DesignationText ??= FindTmp(root, "DesignationText", "Designation Text", "Title");
                NameText ??= FindTmp(root, "NameText", "Name Text", "Player Name", "Nickname");
                WaveText ??= FindTmp(root, "WaveText", "Wave Text", "Wave");
                ProfileImage ??= FindImage(root, "ProfileImage", "Profile Image", "Profile Icon");
            }

            internal void Render(LeaderboardEntryData entry)
            {
                if (Root == null)
                {
                    return;
                }

                Root.SetActive(true);
                SetTmp(RankText, entry.Rank.ToString());
                SetTmp(NameText, entry.PlayerName);
                SetTmp(WaveText, $"WAVE {entry.Wave}");

                // [칭호]는 LeaderboardEntryData에 데이터가 없어 아직 채우지 않는다(프리팹 placeholder 유지).
                // 칭호 시스템이 생기면 여기서 SetTmp(DesignationText, entry.Title) 한 줄이면 된다.

                if (ProfileImage != null)
                {
                    ProfileImage.enabled = true;
                }
            }

            internal void Hide()
            {
                if (Root != null)
                {
                    Root.SetActive(false);
                }
            }

            private static void SetTmp(TMP_Text text, string value)
            {
                if (text != null)
                {
                    text.text = value ?? string.Empty;
                }
            }

            private static TMP_Text FindTmp(Transform root, params string[] names)
            {
                Transform found = FindNamedChild(root, names);
                return found != null ? found.GetComponent<TMP_Text>() : null;
            }

            private static Image FindImage(Transform root, params string[] names)
            {
                Transform found = FindNamedChild(root, names);
                return found != null ? found.GetComponent<Image>() : null;
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
        }
    }
}
