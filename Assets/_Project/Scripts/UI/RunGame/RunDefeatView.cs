using System;
using R3;
using SlotRogue.UI.RunGame.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    public sealed class RunDefeatView : MonoBehaviour, IRunGameView
    {
        private const int SymbolStatRowCount = 6;
        private const string AttackTabSpritePath = "Textures/UI/bt_rankingdata1";
        private const string DefenseTabSpritePath = "Textures/UI/bt_rankingdata2";

        [SerializeField] private RectTransform _layoutRoot;
        [SerializeField] private Text _titleText;
        [SerializeField] private TMP_Text _titleTmpText;

        [Header("Revive Offer")]
        [SerializeField] private GameObject _reviveOfferRoot;
        [SerializeField] private Image _monsterImage;
        [SerializeField] private Text _countdownText;
        [SerializeField] private TMP_Text _countdownTmpText;
        [SerializeField] private Button _reviveButton;
        [SerializeField] private Text _reviveButtonText;
        [SerializeField] private TMP_Text _reviveButtonTmpText;

        [Header("Run Result")]
        [SerializeField] private GameObject _resultRoot;
        [SerializeField] private Text _summaryText;
        [SerializeField] private TMP_Text _summaryTmpText;
        [SerializeField] private Text _contributionText;
        [SerializeField] private TMP_Text _contributionTmpText;
        [SerializeField] private Button _attackTabButton;
        [SerializeField] private Text _attackTabText;
        [SerializeField] private TMP_Text _attackTabTmpText;
        [SerializeField] private Button _defenseTabButton;
        [SerializeField] private Text _defenseTabText;
        [SerializeField] private TMP_Text _defenseTabTmpText;
        [SerializeField] private RectTransform _symbolStatsRoot;
        [SerializeField] private Button _newRunButton;
        [SerializeField] private Text _newRunButtonText;
        [SerializeField] private TMP_Text _newRunButtonTmpText;
        [SerializeField] private Button _rankingButton;
        [SerializeField] private Text _rankingButtonText;
        [SerializeField] private TMP_Text _rankingButtonTmpText;
        [SerializeField] private Button _homeButton;
        [SerializeField] private Text _homeButtonText;
        [SerializeField] private TMP_Text _homeButtonTmpText;

        private Sprite _monsterPortrait;
        private bool _subscribed;
        private RunDefeatViewState _lastState = RunDefeatViewState.Empty;
        private ResultStatTab _activeStatTab = ResultStatTab.Attack;
        private Sprite _attackTabSprite;
        private Sprite _defenseTabSprite;
        private readonly SymbolStatRow[] _symbolStatRows =
            new SymbolStatRow[SymbolStatRowCount];

        public event Action RestartRequested;

        public event Action RankingRequested;

        public event Action HomeRequested;

        public event Action ReviveRequested;

        private void OnDestroy()
        {
            UnsubscribeButtons();
        }

        /// <summary>
        /// 자기 ViewModel을 구독(상태→Render)하고 입력 event를 ViewModel command로 연결한다.
        /// ViewModel이 phase를 게이트한 뒤 intent event를 발사하면 presenter가 흐름을 처리한다(ADR-0020).
        /// </summary>
        public void Bind(RunDefeatViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            RestartRequested += viewModel.RequestRestart;
            RankingRequested += viewModel.RequestRanking;
            HomeRequested += viewModel.RequestHome;
            ReviveRequested += viewModel.RequestRevive;

            viewModel.State.Subscribe(Render).AddTo(this);
        }

        public void OnEnter()
        {
            if (!EnsureRuntimeLayout())
            {
                return;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void OnExit()
        {
            gameObject.SetActive(false);
        }

        public void SetMonsterPortrait(Sprite portrait)
        {
            _monsterPortrait = portrait;
            if (_monsterImage != null)
            {
                _monsterImage.sprite = portrait;
                _monsterImage.gameObject.SetActive(portrait != null);
            }
        }

        public void Render(RunDefeatViewState state)
        {
            if (!EnsureRuntimeLayout())
            {
                return;
            }

            SetText(_titleText, state.Title);
            SetText(_titleTmpText, state.Title);
            SetText(_countdownText, state.CountdownLabel);
            SetText(_countdownTmpText, state.CountdownLabel);
            SetText(_summaryText, state.Summary);
            SetText(_summaryTmpText, state.Summary);
            SetText(_contributionText, state.ContributionSummary);
            SetText(_contributionTmpText, state.ContributionSummary);
            SetText(_newRunButtonText, state.RestartLabel);
            SetText(_newRunButtonTmpText, state.RestartLabel);
            SetText(_rankingButtonText, state.RankingLabel);
            SetText(_rankingButtonTmpText, state.RankingLabel);
            SetText(_homeButtonText, state.HomeLabel);
            SetText(_homeButtonTmpText, state.HomeLabel);
            SetText(_reviveButtonText, state.ReviveLabel);
            SetText(_reviveButtonTmpText, state.ReviveLabel);

            SetActive(_reviveOfferRoot, state.IsReviveOffer);
            SetActive(_resultRoot, state.IsResultVisible);
            _lastState = state;
            RenderSymbolStats(state);

            if (_monsterImage != null)
            {
                _monsterImage.sprite = _monsterPortrait;
                _monsterImage.gameObject.SetActive(
                    state.IsReviveOffer && _monsterPortrait != null);
            }

            if (_reviveButton != null)
            {
                _reviveButton.gameObject.SetActive(state.IsReviveVisible);
                _reviveButton.interactable = state.CanRevive;
            }
        }

        public bool EnsureRuntimeLayout()
        {
            ResolveSceneReferences();
            if (!HasRequiredReferences())
            {
                Debug.LogError(
                    "[RunDefeatView] Required defeat result UI objects must be placed in the hierarchy. " +
                    $"Missing: {BuildMissingReferenceSummary()}");
                return false;
            }

            SubscribeButtons();
            return true;
        }

        private void ResolveSceneReferences()
        {
            _layoutRoot ??= FindDeepChild(transform, "Defeat Layout") as RectTransform;
            _titleText ??= FindChildComponent<Text>("Defeat Title");
            _titleTmpText ??= FindChildComponent<TMP_Text>("Defeat Title Text") ??
                FindChildComponent<TMP_Text>("Defeat Title");
            _reviveOfferRoot ??= FindDeepChild(transform, "Revive Offer Root")?.gameObject;
            _monsterImage ??= FindChildComponent<Image>("Defeating Monster Image");
            _countdownText ??= FindChildComponent<Text>("Revive Countdown");
            _countdownTmpText ??= FindChildComponent<TMP_Text>("Revive Countdown");
            _reviveButton ??= FindChildComponent<Button>("Revive Button");
            _reviveButtonText ??= FindChildComponent<Text>("Revive Button Text");
            _reviveButtonTmpText ??= FindChildComponent<TMP_Text>("Revive Button Text");
            _resultRoot ??= FindDeepChild(transform, "Run Result Root")?.gameObject;
            _summaryText ??= FindChildComponent<Text>("Defeat Summary");
            _summaryTmpText ??= FindChildComponent<TMP_Text>("Defeat Summary");
            _contributionText ??= FindChildComponent<Text>("Symbol Contribution Text") ??
                FindChildComponent<Text>("Relic Contribution Text");
            _contributionTmpText ??= FindChildComponent<TMP_Text>("Symbol Contribution Text") ??
                FindChildComponent<TMP_Text>("Relic Contribution Text");
            _attackTabButton ??= FindChildComponent<Button>("Attack Tab Button");
            _attackTabText ??= FindChildComponent<Text>("Attack Tab Text");
            _attackTabTmpText ??= FindChildComponent<TMP_Text>("Attack Tab Text");
            _defenseTabButton ??= FindChildComponent<Button>("Defense Tab Button");
            _defenseTabText ??= FindChildComponent<Text>("Defense Tab Text");
            _defenseTabTmpText ??= FindChildComponent<TMP_Text>("Defense Tab Text");
            _symbolStatsRoot ??= FindDeepChild(transform, "Result Symbol Stats Root") as RectTransform;
            _newRunButton ??= FindChildComponent<Button>("New Run Button");
            _newRunButtonText ??= FindChildComponent<Text>("New Run Button Text");
            _newRunButtonTmpText ??= FindChildComponent<TMP_Text>("New Run Button Text");
            _rankingButton ??= FindChildComponent<Button>("Ranking Button");
            _rankingButtonText ??= FindChildComponent<Text>("Ranking Button Text");
            _rankingButtonTmpText ??= FindChildComponent<TMP_Text>("Ranking Button Text");
            _homeButton ??= FindChildComponent<Button>("Home Button");
            _homeButtonText ??= FindChildComponent<Text>("Home Button Text");
            _homeButtonTmpText ??= FindChildComponent<TMP_Text>("Home Button Text");
            ResolveSymbolStatRows();
        }

        private bool HasRequiredReferences()
        {
            return _layoutRoot != null &&
                (_titleText != null || _titleTmpText != null) &&
                _reviveOfferRoot != null &&
                HasText(_countdownText, _countdownTmpText) &&
                _reviveButton != null &&
                HasText(_reviveButtonText, _reviveButtonTmpText) &&
                _resultRoot != null &&
                HasText(_summaryText, _summaryTmpText) &&
                (HasText(_contributionText, _contributionTmpText) || HasSymbolStatRows()) &&
                _newRunButton != null &&
                HasText(_newRunButtonText, _newRunButtonTmpText) &&
                _rankingButton != null &&
                HasText(_rankingButtonText, _rankingButtonTmpText) &&
                _homeButton != null &&
                HasText(_homeButtonText, _homeButtonTmpText);
        }

        private string BuildMissingReferenceSummary()
        {
            var builder = new System.Text.StringBuilder();
            AppendMissing(builder, _layoutRoot != null, "Defeat Layout");
            AppendMissing(builder, _titleText != null || _titleTmpText != null, "Defeat Title/Text");
            AppendMissing(builder, _reviveOfferRoot != null, "Revive Offer Root");
            AppendMissing(builder, HasText(_countdownText, _countdownTmpText), "Revive Countdown");
            AppendMissing(builder, _reviveButton != null, "Revive Button");
            AppendMissing(builder, HasText(_reviveButtonText, _reviveButtonTmpText), "Revive Button Text");
            AppendMissing(builder, _resultRoot != null, "Run Result Root");
            AppendMissing(builder, HasText(_summaryText, _summaryTmpText), "Defeat Summary");
            AppendMissing(
                builder,
                HasText(_contributionText, _contributionTmpText) || HasSymbolStatRows(),
                "Symbol Contribution Text or Result Symbol Stat Rows");
            AppendMissing(builder, _newRunButton != null, "New Run Button");
            AppendMissing(builder, HasText(_newRunButtonText, _newRunButtonTmpText), "New Run Button Text");
            AppendMissing(builder, _rankingButton != null, "Ranking Button");
            AppendMissing(builder, HasText(_rankingButtonText, _rankingButtonTmpText), "Ranking Button Text");
            AppendMissing(builder, _homeButton != null, "Home Button");
            AppendMissing(builder, HasText(_homeButtonText, _homeButtonTmpText), "Home Button Text");
            return builder.Length > 0 ? builder.ToString() : "none";
        }

        private static void AppendMissing(
            System.Text.StringBuilder builder,
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

        private void SubscribeButtons()
        {
            if (_subscribed ||
                _newRunButton == null ||
                _rankingButton == null ||
                _homeButton == null ||
                _reviveButton == null)
            {
                return;
            }

            _newRunButton.onClick.AddListener(HandleRestartClicked);
            _rankingButton.onClick.AddListener(HandleRankingClicked);
            _homeButton.onClick.AddListener(HandleHomeClicked);
            _reviveButton.onClick.AddListener(HandleReviveClicked);
            _attackTabButton?.onClick.AddListener(HandleAttackTabClicked);
            _defenseTabButton?.onClick.AddListener(HandleDefenseTabClicked);
            _subscribed = true;
        }

        private void UnsubscribeButtons()
        {
            if (!_subscribed)
            {
                return;
            }

            _newRunButton?.onClick.RemoveListener(HandleRestartClicked);
            _rankingButton?.onClick.RemoveListener(HandleRankingClicked);
            _homeButton?.onClick.RemoveListener(HandleHomeClicked);
            _reviveButton?.onClick.RemoveListener(HandleReviveClicked);
            _attackTabButton?.onClick.RemoveListener(HandleAttackTabClicked);
            _defenseTabButton?.onClick.RemoveListener(HandleDefenseTabClicked);
            _subscribed = false;
        }

        private void HandleRestartClicked()
        {
            RestartRequested?.Invoke();
        }

        private void HandleRankingClicked()
        {
            RankingRequested?.Invoke();
        }

        private void HandleHomeClicked()
        {
            HomeRequested?.Invoke();
        }

        private void HandleReviveClicked()
        {
            ReviveRequested?.Invoke();
        }

        private void HandleAttackTabClicked()
        {
            _activeStatTab = ResultStatTab.Attack;
            RenderSymbolStats(_lastState);
        }

        private void HandleDefenseTabClicked()
        {
            _activeStatTab = ResultStatTab.Defense;
            RenderSymbolStats(_lastState);
        }

        private void ResolveSymbolStatRows()
        {
            for (int index = 0; index < SymbolStatRowCount; index++)
            {
                if (_symbolStatRows[index]?.IsValid == true)
                {
                    continue;
                }

                Transform row = FindDeepChild(transform, $"Result Symbol Stat Row {index}");
                _symbolStatRows[index] = SymbolStatRow.Resolve(row);
            }
        }

        private bool HasSymbolStatRows()
        {
            for (int index = 0; index < _symbolStatRows.Length; index++)
            {
                if (_symbolStatRows[index]?.IsValid == true)
                {
                    return true;
                }
            }

            return false;
        }

        private void RenderSymbolStats(RunDefeatViewState state)
        {
            bool hasRows = HasSymbolStatRows();
            // 직렬화 필드는 미할당 시 C# null이 아니라 Unity "가짜 null"이라 `?.`이 통과해 버린다.
            // Component 오버로드(SetActive)가 Unity 오버로드 `!= null`로 안전하게 가른다(AGENTS §6).
            SetActive(_symbolStatsRoot, state.IsResultVisible && hasRows);
            SetActive(_contributionText, state.IsResultVisible && !hasRows);
            SetActive(_contributionTmpText, state.IsResultVisible && !hasRows);

            if (!hasRows)
            {
                return;
            }

            bool attackActive = _activeStatTab == ResultStatTab.Attack;
            SetTabSprite(attackActive);
            SetTextColor(_attackTabText, Color.white);
            SetTextColor(_attackTabTmpText, Color.white);
            SetTextColor(_defenseTabText, Color.white);
            SetTextColor(_defenseTabTmpText, Color.white);

            int maxValue = CalculateMaxStatValue(state);
            for (int index = 0; index < _symbolStatRows.Length; index++)
            {
                SymbolStatRow row = _symbolStatRows[index];
                if (row?.IsValid != true)
                {
                    continue;
                }

                bool hasStat = state.SymbolStats != null && index < state.SymbolStats.Count;
                RunDefeatSymbolStatViewState stat = hasStat
                    ? state.SymbolStats[index]
                    : default;
                int value = _activeStatTab == ResultStatTab.Attack
                    ? stat.AttackPower
                    : stat.DefensePower;
                row.Render(stat, value, maxValue);
            }
        }

        private int CalculateMaxStatValue(RunDefeatViewState state)
        {
            int max = 1;
            if (state.SymbolStats == null)
            {
                return max;
            }

            for (int index = 0; index < state.SymbolStats.Count; index++)
            {
                RunDefeatSymbolStatViewState stat = state.SymbolStats[index];
                int value = _activeStatTab == ResultStatTab.Attack
                    ? stat.AttackPower
                    : stat.DefensePower;
                max = Mathf.Max(max, value);
            }

            return max;
        }

        private T FindChildComponent<T>(string objectName) where T : Component
        {
            Transform child = FindDeepChild(transform, objectName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static Transform FindDeepChild(Transform parent, string objectName)
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

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static bool HasText(Text text, TMP_Text tmpText)
        {
            return text != null || tmpText != null;
        }

        private static void SetTextColor(Text text, Color color)
        {
            if (text != null)
            {
                text.color = color;
            }
        }

        private static void SetTextColor(TMP_Text text, Color color)
        {
            if (text != null)
            {
                text.color = color;
            }
        }

        private void SetTabSprite(bool attackActive)
        {
            if (_attackTabButton?.targetGraphic is Image tabImage)
            {
                tabImage.sprite = attackActive
                    ? LoadTabSprite(ref _attackTabSprite, AttackTabSpritePath)
                    : LoadTabSprite(ref _defenseTabSprite, DefenseTabSpritePath);
                tabImage.color = Color.white;
                tabImage.preserveAspect = true;
            }

            if (_defenseTabButton?.targetGraphic != null)
            {
                _defenseTabButton.targetGraphic.color = Color.clear;
            }
        }

        private static Sprite LoadTabSprite(ref Sprite cachedSprite, string resourcePath)
        {
            if (cachedSprite != null)
            {
                return cachedSprite;
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            cachedSprite = sprites != null && sprites.Length > 0 ? sprites[0] : null;
            return cachedSprite;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        // Component 오버로드: 미할당/파괴된 직렬화 참조를 Unity 오버로드 `!= null`로 안전하게 거른다.
        // (`component?.gameObject`는 Unity "가짜 null"에서 UnassignedReferenceException을 던진다.)
        private static void SetActive(Component target, bool active)
        {
            if (target != null)
            {
                target.gameObject.SetActive(active);
            }
        }

        private enum ResultStatTab
        {
            Attack = 0,
            Defense = 1,
        }

        private sealed class SymbolStatRow
        {
            private readonly GameObject _root;
            private readonly Text _rowText;
            private readonly TMP_Text _rowTmpText;
            private readonly Image _iconFrame;
            private readonly Text _nameText;
            private readonly TMP_Text _nameTmpText;
            private readonly Text _valueText;
            private readonly TMP_Text _valueTmpText;
            private readonly Image _fillImage;

            private SymbolStatRow(
                GameObject root,
                Text rowText,
                TMP_Text rowTmpText,
                Image iconFrame,
                Text nameText,
                TMP_Text nameTmpText,
                Text valueText,
                TMP_Text valueTmpText,
                Image fillImage)
            {
                _root = root;
                _rowText = rowText;
                _rowTmpText = rowTmpText;
                _iconFrame = iconFrame;
                _nameText = nameText;
                _nameTmpText = nameTmpText;
                _valueText = valueText;
                _valueTmpText = valueTmpText;
                _fillImage = fillImage;
            }

            internal bool IsValid =>
                _root != null &&
                (_rowText != null ||
                    _rowTmpText != null ||
                    _nameText != null ||
                    _nameTmpText != null ||
                    _valueText != null ||
                    _valueTmpText != null);

            internal static SymbolStatRow Resolve(Transform row)
            {
                if (row == null)
                {
                    return null;
                }

                Text rowText = row.GetComponent<Text>();
                TMP_Text rowTmpText = row.GetComponent<TMP_Text>();
                Image iconFrame = FindNestedComponent<Image>(row, "Symbol Icon Frame");
                Text nameText = FindNestedComponent<Text>(row, "Symbol Name");
                TMP_Text nameTmpText = FindNestedComponent<TMP_Text>(row, "Symbol Name");
                Text valueText = FindNestedComponent<Text>(row, "Symbol Value Text");
                TMP_Text valueTmpText = FindNestedComponent<TMP_Text>(row, "Symbol Value Text");
                Image fillImage = FindNestedComponent<Image>(row, "Symbol Value Bar Fill");
                return new SymbolStatRow(
                    row.gameObject,
                    rowText,
                    rowTmpText,
                    iconFrame,
                    nameText,
                    nameTmpText,
                    valueText,
                    valueTmpText,
                    fillImage);
            }

            internal void Render(
                RunDefeatSymbolStatViewState stat,
                int value,
                int maxValue)
            {
                _root.SetActive(true);
                string displayName = string.IsNullOrWhiteSpace(stat.DisplayName)
                    ? "-"
                    : stat.DisplayName;
                bool hasAuthoredRow =
                    _nameText != null ||
                    _nameTmpText != null ||
                    _valueText != null ||
                    _valueTmpText != null ||
                    _fillImage != null;

                if (_rowText != null)
                {
                    _rowText.enabled = !hasAuthoredRow;
                    if (!hasAuthoredRow)
                    {
                        string prefix = _iconFrame != null ? "      " : string.Empty;
                        _rowText.text =
                            $"{prefix}{displayName,-8}  x{stat.PatternCount}  {BuildBar(value, maxValue)}  {value}";
                    }
                }

                if (_rowTmpText != null)
                {
                    _rowTmpText.enabled = !hasAuthoredRow;
                    if (!hasAuthoredRow)
                    {
                        string prefix = _iconFrame != null ? "      " : string.Empty;
                        _rowTmpText.text =
                            $"{prefix}{displayName,-8}  x{stat.PatternCount}  {BuildBar(value, maxValue)}  {value}";
                    }
                }

                if (_nameText != null)
                {
                    _nameText.text = $"{displayName}  x{stat.PatternCount}";
                }

                if (_nameTmpText != null)
                {
                    _nameTmpText.text = $"{displayName}  x{stat.PatternCount}";
                }

                if (_valueText != null)
                {
                    _valueText.text = value.ToString();
                }

                if (_valueTmpText != null)
                {
                    _valueTmpText.text = value.ToString();
                }

                if (_fillImage != null)
                {
                    _fillImage.type = Image.Type.Filled;
                    _fillImage.fillMethod = Image.FillMethod.Horizontal;
                    _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                    _fillImage.fillAmount = maxValue <= 0 ? 0f : Mathf.Clamp01((float)value / maxValue);
                }
            }

            private static string BuildBar(int value, int maxValue)
            {
                const int Width = 10;
                int filled = maxValue <= 0
                    ? 0
                    : Mathf.RoundToInt(Mathf.Clamp01((float)value / maxValue) * Width);
                return new string('|', filled).PadRight(Width, '.');
            }

            private static T FindNestedComponent<T>(Transform root, string objectName)
                where T : Component
            {
                Transform found = FindDeepChild(root, objectName);
                return found != null ? found.GetComponent<T>() : null;
            }
        }
    }
}
