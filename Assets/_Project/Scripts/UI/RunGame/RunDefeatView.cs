using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.RunGame.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// 패배 화면(부활 제안 / 결과) View입니다. TMP 전용.
    /// 상태 구독·입력 event 연결은 Bind가 소유합니다(ADR-0020).
    /// RANKING 버튼과 SCORE 텍스트는 선택 요소입니다(없어도 동작).
    /// </summary>
    public sealed partial class RunDefeatView : ViewComponentBase, IRunGameView, IDefeatPortraitView
    {
        private const int SymbolStatRowCount = 6;

        [Header("Revive Offer")]
        [SerializeField] private GameObject _reviveOfferRoot;
        [Tooltip("WAVE 숫자 텍스트 (코드가 채움). '돌파 실패' 문구는 프리팹 고정.")]
        [SerializeField] private TMP_Text _reviveWaveText;
        [SerializeField] private Image _monsterImage;
        [SerializeField] private TMP_Text _countdownText;
        [SerializeField] private Button _reviveButton;
        [SerializeField] private TMP_Text _reviveButtonText;

        [Header("Run Result")]
        [SerializeField] private GameObject _resultRoot;
        [Tooltip("WAVE 숫자 텍스트 (코드가 채움). '돌파 …' 문구는 프리팹 고정.")]
        [SerializeField] private TMP_Text _resultWaveText;
        [Tooltip("선택: 점수 시스템이 생기면 채운다(현재 placeholder).")]
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _summaryText;
        [SerializeField] private TMP_Text _contributionText;
        [SerializeField] private Button _attackTabButton;
        [SerializeField] private TMP_Text _attackTabText;
        [SerializeField] private Button _defenseTabButton;
        [SerializeField] private TMP_Text _defenseTabText;
        [Header("Tab Sprites (선택 상태 교체용)")]
        [SerializeField] private Sprite _attackTabActiveSprite;
        [SerializeField] private Sprite _attackTabInactiveSprite;
        [SerializeField] private Sprite _defenseTabActiveSprite;
        [SerializeField] private Sprite _defenseTabInactiveSprite;
        [Tooltip("심볼 아이콘 스프라이트(SlotSymbolPool.Symbols 순서: 체리·세븐·다이아·벨·클로버·레몬). 정렬 순서대로 코드가 행에 배치한다.")]
        [SerializeField] private Sprite[] _symbolIcons = Array.Empty<Sprite>();
        [SerializeField] private RectTransform _symbolStatsRoot;
        [SerializeField] private Button _newRunButton;
        [SerializeField] private TMP_Text _newRunButtonText;
        [Tooltip("선택: 결과 화면에 RANKING 진입 버튼이 있을 때만.")]
        [SerializeField] private Button _rankingButton;
        [SerializeField] private TMP_Text _rankingButtonText;
        [SerializeField] private Button _homeButton;
        [SerializeField] private TMP_Text _homeButtonText;

        private Sprite _monsterPortrait;
        private bool _subscribed;
        private const float BarFillDuration = 0.5f;
        private const float BarFillStagger = 0.06f;

        private RunDefeatViewState _lastState = RunDefeatViewState.Empty;
        private ResultStatTab _activeStatTab = ResultStatTab.Attack;
        private bool _resultVisible;
        private readonly SymbolStatRow[] _symbolStatRows =
            new SymbolStatRow[SymbolStatRowCount];
        private AddressableSpriteProvider _symbolIconProvider;
        private CancellationTokenSource _symbolIconCts;
        private int _symbolIconVersion;

        public event Action RestartRequested;

        public event Action RankingRequested;

        public event Action HomeRequested;

        public event Action ReviveRequested;

        private void OnDestroy()
        {
            UnsubscribeButtons();
            KillBarTweens();
            _symbolIconCts?.Cancel();
            _symbolIconCts?.Dispose();
            _symbolIconCts = null;
            _symbolIconProvider?.Dispose();
            _symbolIconProvider = null;
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
            KillBarTweens();
            _resultVisible = false;
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

            // WAVE 숫자만 코드가 채우고, '돌파 실패/성공' 문구는 프리팹 고정 텍스트를 유지한다.
            string waveLabel = $"WAVE {state.BattleNumber}";
            SetText(_reviveWaveText, waveLabel);
            SetText(_resultWaveText, waveLabel);
            SetText(_countdownText, state.CountdownLabel);
            SetText(_summaryText, state.Summary);
            SetText(_contributionText, state.ContributionSummary);
            SetText(_newRunButtonText, state.RestartLabel);
            SetText(_rankingButtonText, state.RankingLabel);
            SetText(_homeButtonText, state.HomeLabel);
            SetText(_reviveButtonText, state.ReviveLabel);

            // SCORE는 점수 시스템 생기기 전까지 placeholder(프리팹 작성값 유지).

            SetActive(_reviveOfferRoot, state.IsReviveOffer);
            SetActive(_resultRoot, state.IsResultVisible);
            _lastState = state;
            // 결과 화면이 막 나타날 때만 막대를 0→목표로 차오르게 한다(탭 전환은 즉시).
            bool enteringResult = state.IsResultVisible && !_resultVisible;
            _resultVisible = state.IsResultVisible;
            RenderSymbolStats(state, enteringResult);

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
            ResolveSymbolStatRows();
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

        // RANKING/SCORE/탭은 선택 요소이므로 필수에서 제외한다.
        private bool HasRequiredReferences()
        {
            return _reviveOfferRoot != null &&
                _countdownText != null &&
                _reviveButton != null &&
                _reviveButtonText != null &&
                _resultRoot != null &&
                (_summaryText != null || HasSymbolStatRows()) &&
                _newRunButton != null &&
                _newRunButtonText != null &&
                _homeButton != null &&
                _homeButtonText != null;
        }

        private string BuildMissingReferenceSummary()
        {
            var builder = new System.Text.StringBuilder();
            AppendMissing(builder, _reviveOfferRoot != null, "Revive Offer Root");
            AppendMissing(builder, _countdownText != null, "Revive Countdown");
            AppendMissing(builder, _reviveButton != null, "Revive Button");
            AppendMissing(builder, _reviveButtonText != null, "Revive Button Text");
            AppendMissing(builder, _resultRoot != null, "Run Result Root");
            AppendMissing(
                builder,
                _summaryText != null || HasSymbolStatRows(),
                "Defeat Summary or Result Symbol Stat Rows");
            AppendMissing(builder, _newRunButton != null, "New Run Button");
            AppendMissing(builder, _newRunButtonText != null, "New Run Button Text");
            AppendMissing(builder, _homeButton != null, "Home Button");
            AppendMissing(builder, _homeButtonText != null, "Home Button Text");
            return builder.Length > 0 ? builder.ToString() : "none";
        }

        private void SubscribeButtons()
        {
            if (_subscribed ||
                _newRunButton == null ||
                _homeButton == null ||
                _reviveButton == null)
            {
                return;
            }

            _newRunButton.onClick.AddListener(HandleRestartClicked);
            _homeButton.onClick.AddListener(HandleHomeClicked);
            _reviveButton.onClick.AddListener(HandleReviveClicked);
            _rankingButton?.onClick.AddListener(HandleRankingClicked);
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
            _homeButton?.onClick.RemoveListener(HandleHomeClicked);
            _reviveButton?.onClick.RemoveListener(HandleReviveClicked);
            _rankingButton?.onClick.RemoveListener(HandleRankingClicked);
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
            RenderSymbolStats(_lastState, animate: false);
        }

        private void HandleDefenseTabClicked()
        {
            _activeStatTab = ResultStatTab.Defense;
            RenderSymbolStats(_lastState, animate: false);
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

        private void RenderSymbolStats(RunDefeatViewState state, bool animate)
        {
            bool hasRows = HasSymbolStatRows();
            // 직렬화 필드는 미할당 시 C# null이 아니라 Unity "가짜 null"이라 `?.`이 통과해 버린다.
            // Component 오버로드(SetActive)가 Unity 오버로드 `!= null`로 안전하게 가른다(AGENTS §6).
            SetActive(_symbolStatsRoot, state.IsResultVisible && hasRows);
            SetActive(_contributionText, state.IsResultVisible && !hasRows);

            if (!hasRows)
            {
                return;
            }

            int iconVersion = ++_symbolIconVersion;
            bool attackActive = _activeStatTab == ResultStatTab.Attack;
            SetTabSprite(attackActive);
            SetTextColor(_attackTabText, Color.white);
            SetTextColor(_defenseTabText, Color.white);

            // 활성 탭(공격/방어) 수치 기준 내림차순으로 정렬해, 많이 기여한 심볼이 위로 온다.
            List<RunDefeatSymbolStatViewState> sorted = BuildSortedStats(state);
            int maxValue = CalculateMaxStatValue(state);
            for (int index = 0; index < _symbolStatRows.Length; index++)
            {
                SymbolStatRow row = _symbolStatRows[index];
                if (row?.IsValid != true)
                {
                    continue;
                }

                bool hasStat = index < sorted.Count;
                RunDefeatSymbolStatViewState stat = hasStat ? sorted[index] : default;
                int value = StatValue(stat);
                Sprite icon = hasStat ? ResolveSymbolIcon(stat.Symbol) : null;
                row.Render(
                    stat, value, maxValue, animate, index * BarFillStagger, BarFillDuration, icon);
                if (hasStat && icon == null)
                {
                    ApplyAddressableSymbolIcon(row, stat.Symbol, iconVersion);
                }
            }
        }

        private List<RunDefeatSymbolStatViewState> BuildSortedStats(RunDefeatViewState state)
        {
            var list = new List<RunDefeatSymbolStatViewState>();
            if (state.SymbolStats != null)
            {
                list.AddRange(state.SymbolStats);
            }

            list.Sort((left, right) => StatValue(right).CompareTo(StatValue(left)));
            return list;
        }

        private int StatValue(RunDefeatSymbolStatViewState stat)
        {
            return _activeStatTab == ResultStatTab.Attack
                ? stat.AttackPower
                : stat.DefensePower;
        }

        private Sprite ResolveSymbolIcon(SlotSymbolType symbol)
        {
            if (_symbolIcons == null)
            {
                return null;
            }

            IReadOnlyList<SlotSymbolType> order = SlotSymbolPool.Symbols;
            for (int index = 0; index < order.Count && index < _symbolIcons.Length; index++)
            {
                if (order[index] == symbol)
                {
                    return _symbolIcons[index];
                }
            }

            return null;
        }

        private void ApplyAddressableSymbolIcon(
            SymbolStatRow row,
            SlotSymbolType symbol,
            int version)
        {
            if (row == null)
            {
                return;
            }

            string key = SlotSymbolIconKeys.For(symbol);
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            _symbolIconCts ??= new CancellationTokenSource();
            LoadSymbolIconAsync(
                row,
                key,
                SymbolProvider(),
                version,
                _symbolIconCts.Token).Forget();
        }

        private AddressableSpriteProvider SymbolProvider()
        {
            return _symbolIconProvider ??= new AddressableSpriteProvider(string.Empty);
        }

        private async UniTaskVoid LoadSymbolIconAsync(
            SymbolStatRow row,
            string key,
            AddressableSpriteProvider provider,
            int version,
            CancellationToken cancellationToken)
        {
            Sprite sprite;
            try
            {
                sprite = await provider.LoadAsync(key, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (version == _symbolIconVersion && sprite != null)
            {
                row.SetIcon(sprite);
            }
        }

        private void KillBarTweens()
        {
            for (int index = 0; index < _symbolStatRows.Length; index++)
            {
                _symbolStatRows[index]?.KillFill();
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

        // 선택된 탭은 active 스프라이트, 나머지는 inactive 스프라이트로 교체한다(직렬화 참조 — ADR-0006).
        private void SetTabSprite(bool attackActive)
        {
            ApplyTabSprite(
                _attackTabButton,
                attackActive ? _attackTabActiveSprite : _attackTabInactiveSprite);
            ApplyTabSprite(
                _defenseTabButton,
                attackActive ? _defenseTabInactiveSprite : _defenseTabActiveSprite);
        }

        private static void ApplyTabSprite(Button tabButton, Sprite sprite)
        {
            if (tabButton == null ||
                sprite == null ||
                tabButton.targetGraphic is not Image image)
            {
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.enabled = true;
        }

        private enum ResultStatTab
        {
            Attack = 0,
            Defense = 1,
        }
    }
}
