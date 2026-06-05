using System.Collections.Generic;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class SlotPresentationDemoController : MonoBehaviour
    {
        [SerializeField] private SlotPresentationManager _presentationManager;
        [SerializeField] private Sprite _demoRelicIcon;
        [SerializeField] private Text _statusText;
        [SerializeField] private Button _fullDemoButton;
        [SerializeField] private Button _multiDemoButton;
        [SerializeField] private Button _replayBestButton;
        [SerializeField] private Button _skipButton;
        [SerializeField] private bool _playOnStart = true;

        public void Bind(
            SlotPresentationManager presentationManager,
            Text statusText,
            Button fullDemoButton,
            Button multiDemoButton,
            Button replayBestButton,
            Button skipButton,
            Sprite demoRelicIcon = null)
        {
            _presentationManager = presentationManager;
            _demoRelicIcon = demoRelicIcon;
            _statusText = statusText;
            _fullDemoButton = fullDemoButton;
            _multiDemoButton = multiDemoButton;
            _replayBestButton = replayBestButton;
            _skipButton = skipButton;
        }

        private void OnEnable()
        {
            AddListener(_fullDemoButton, PlayFullDemo);
            AddListener(_multiDemoButton, PlayMultiDemo);
            AddListener(_replayBestButton, ReplayPerfectBoardDemo);
            AddListener(_skipButton, SkipAll);
        }

        private void Start()
        {
            if (_playOnStart)
            {
                PlayFullDemo();
            }
            else
            {
                SetStatus("데모를 선택하세요.");
            }
        }

        private void OnDisable()
        {
            RemoveListener(_fullDemoButton, PlayFullDemo);
            RemoveListener(_multiDemoButton, PlayMultiDemo);
            RemoveListener(_replayBestButton, ReplayPerfectBoardDemo);
            RemoveListener(_skipButton, SkipAll);
        }

        [ContextMenu("Play Perfect Board Demo")]
        public void PlayFullDemo()
        {
            SlotSymbolType[] symbols = CreateAllSameSymbols(SlotSymbolType.Cherry);

            PlayDemo(
                symbols,
                new[]
                {
                    new SlotRelicTriggerPresentationResult(
                        "cherry",
                        "체리",
                        _demoRelicIcon,
                        "완전 체리 보드에서 체리 아이콘 피해 보너스가 발동됩니다.",
                        "+15 피해")
                },
                "잭팟 보드: 모든 패턴 순서대로 발동 → 유물 → 최종 결과.");
        }

        [ContextMenu("Play Mixed Board Demo")]
        public void PlayMultiDemo()
        {
            SlotSymbolType[] symbols =
            {
                SlotSymbolType.Bell, SlotSymbolType.Clover, SlotSymbolType.Grape, SlotSymbolType.Clover, SlotSymbolType.Bell,
                SlotSymbolType.Cherry, SlotSymbolType.Cherry, SlotSymbolType.Cherry, SlotSymbolType.Cherry, SlotSymbolType.Cherry,
                SlotSymbolType.Bell, SlotSymbolType.Clover, SlotSymbolType.Grape, SlotSymbolType.Clover, SlotSymbolType.Bell
            };

            PlayDemo(
                symbols,
                new[]
                {
                    new SlotRelicTriggerPresentationResult(
                        "RunBonus",
                        "숫돌",
                        _demoRelicIcon,
                        "패턴 이후 런 피해 보너스가 추가됩니다.",
                        "+4 피해")
                },
                "혼합 보드: 가로 5칸 2열 → 유물 → 최종 결과.");
        }

        [ContextMenu("Replay Perfect Board Demo")]
        public void ReplayPerfectBoardDemo()
        {
            PlayFullDemo();
        }

        public void SkipAll()
        {
            if (_presentationManager != null)
            {
                _presentationManager.SkipAll();
            }
        }

        private void PlayDemo(
            SlotSymbolType[] symbols,
            SlotRelicTriggerPresentationResult[] relics,
            string status)
        {
            SetStatus(status);

            if (_presentationManager == null)
            {
                SetStatus($"{status}\nPresentationManager missing.");
                return;
            }

            var spinResult = new SlotSpinResult(symbols);
            var resolver = new SlotPatternResolver();
            IReadOnlyList<SlotPatternMatch> matches = resolver.ResolveAll(spinResult);

            SlotPatternPresentationResult[] patternResults = ConvertMatchesToPresentation(matches);
            SlotFinalPresentationResult finalResult = BuildFinalResult(matches);

            var presentationResult = new SlotPresentationResult(
                spinResult,
                patternResults,
                relics,
                finalResult);

            _presentationManager.Play(
                presentationResult,
                _ => SetStatus($"{status}\n완료."));
        }

        private static SlotPatternPresentationResult[] ConvertMatchesToPresentation(
            IReadOnlyList<SlotPatternMatch> matches)
        {
            var results = new SlotPatternPresentationResult[matches.Count];

            for (int index = 0; index < matches.Count; index++)
            {
                SlotPatternMatch match = matches[index];
                int[] highlightedIndices = new int[match.MatchedCells.Count];

                for (int cellIndex = 0; cellIndex < match.MatchedCells.Count; cellIndex++)
                {
                    SlotCell cell = match.MatchedCells[cellIndex];
                    highlightedIndices[cellIndex] = SlotSpinResult.ToIndex(cell.Col, cell.Row);
                }

                int sfxLevel = Mathf.Clamp(match.Definition.OrderIndex, 0, 7);
                bool isFinale = index == matches.Count - 1;
                string description = $"아이콘: {match.Symbol} / 배율 x{match.Definition.Multiplier:0.0}";
                string bonusText = $"+{match.CalculatedValue} 점";

                results[index] = new SlotPatternPresentationResult(
                    patternName: match.PresentationTitle,
                    symbol: match.Symbol,
                    row: match.MatchedCells.Count > 0 ? match.MatchedCells[0].Row : -1,
                    startColumn: match.MatchedCells.Count > 0 ? match.MatchedCells[0].Col : -1,
                    matchLength: match.MatchedCells.Count,
                    highlightedCellIndices: highlightedIndices,
                    description: description,
                    bonusText: bonusText,
                    isFinale: isFinale,
                    sfxLevel: sfxLevel);
            }

            return results;
        }

        private static SlotFinalPresentationResult BuildFinalResult(
            IReadOnlyList<SlotPatternMatch> matches)
        {
            int totalScore = 0;

            foreach (SlotPatternMatch match in matches)
            {
                totalScore += match.CalculatedValue;
            }

            string summary = totalScore > 0
                ? $"총 점수: {totalScore} 점"
                : "매치된 패턴 없음.";

            return new SlotFinalPresentationResult(
                damage: totalScore,
                defense: 0,
                attackCount: Mathf.Max(1, matches.Count),
                healAmount: 0,
                summaryText: summary);
        }

        private static SlotSymbolType[] CreateAllSameSymbols(SlotSymbolType symbol)
        {
            var symbols = new SlotSymbolType[SlotSpinResult.CellCount];

            for (int index = 0; index < symbols.Length; index++)
            {
                symbols[index] = symbol;
            }

            return symbols;
        }

        private void SetStatus(string value)
        {
            if (_statusText != null)
            {
                _statusText.text = value;
            }
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }
    }
}
