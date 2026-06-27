using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;
using SlotRogue.Slot.ViewModels;
using SlotRogue.UI.SlotPresentation;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    internal sealed class SlotTurnController
    {
        private readonly SlotMachineModel _slotViewModel;
        private readonly RunBattleSpinSequence _spinSequence;
        private readonly SlotPresentationManager _presentationManager;
        private readonly Func<string, Sprite> _relicIconResolver;

        internal SlotTurnController(
            SlotMachineModel slotViewModel,
            RunBattleSpinSequence spinSequence,
            SlotPresentationManager presentationManager,
            Func<string, Sprite> relicIconResolver = null)
        {
            _slotViewModel = slotViewModel;
            _spinSequence = spinSequence;
            _presentationManager = presentationManager;
            _relicIconResolver = relicIconResolver;
        }

        internal void SetupImmediate()
        {
            _spinSequence.SetupImmediate();
            _presentationManager?.ShowImmediate(CreateInitialSlotDisplayResult());
        }

        internal async UniTask<SlotTurnResult> SpinAsync(SlotTurnRequest request)
        {
            _spinSequence.Reset();
            await _spinSequence.PlayDownAsync(request.CancellationToken);
            _spinSequence.StartSpin();
            _slotViewModel.Spin();

            return new SlotTurnResult(
                _slotViewModel.CurrentSpinResult,
                _slotViewModel.CurrentPatternMatches,
                _slotViewModel.CurrentPatternResult,
                _slotViewModel.CurrentCombatRequest);
        }

        internal async UniTask PlayPresentationAsync(
            SlotTurnResult slotTurnResult,
            RelicResolveResult relicResult,
            RunCombatRequestResult combatRequestResult,
            CancellationToken cancellationToken)
        {
            if (_presentationManager == null)
            {
                await _spinSequence.SettleIfNeededAsync(cancellationToken);
                return;
            }

            SlotPresentationResult presentationResult =
                BuildPresentationResult(slotTurnResult, relicResult, combatRequestResult);
            bool presentationDone = false;
            bool slotSpinDone = false;

            void HandleSlotReelStopped(int reelIndex)
            {
                _spinSequence.SetReelIdle(reelIndex);
            }

            void HandleSlotSpinCompleted()
            {
                slotSpinDone = true;
            }

            _presentationManager.SlotReelStopped += HandleSlotReelStopped;
            _presentationManager.SlotSpinCompleted += HandleSlotSpinCompleted;
            try
            {
                _presentationManager.Play(presentationResult, _ => presentationDone = true);

                await UniTask.WaitUntil(
                    () => slotSpinDone || presentationDone,
                    cancellationToken: cancellationToken);
                await _spinSequence.SettleIfNeededAsync(cancellationToken);
                await UniTask.WaitUntil(
                    () => presentationDone,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                _presentationManager.SlotReelStopped -= HandleSlotReelStopped;
                _presentationManager.SlotSpinCompleted -= HandleSlotSpinCompleted;
            }
        }

        internal async UniTask BeforeBattleEventPresentedAsync(
            CombatEvent combatEvent,
            int eventIndex,
            IReadOnlyList<CombatEvent> events)
        {
            if (ShouldRaiseLeverBeforeEvent(combatEvent))
            {
                await _spinSequence.RaiseLeverIfNeededAsync();
            }
        }

        internal async UniTask AfterBattleEventPresentedAsync(
            CombatEvent combatEvent,
            int eventIndex,
            IReadOnlyList<CombatEvent> events)
        {
            if (IsLastPlayerAttackPresentation(combatEvent, eventIndex, events))
            {
                await _spinSequence.RaiseLeverIfNeededAsync();
            }
        }

        internal void ResetImmediate()
        {
            _spinSequence.ResetImmediate();
        }

        private SlotPresentationResult BuildPresentationResult(
            SlotTurnResult slotTurnResult,
            RelicResolveResult relicResult,
            RunCombatRequestResult combatRequestResult)
        {
            IReadOnlyList<SlotPatternMatch> matches = slotTurnResult.PatternMatches;
            var patternPresentations = new SlotPatternPresentationResult[matches.Count];

            for (int index = 0; index < matches.Count; index++)
            {
                SlotPatternMatch match = matches[index];
                var cellIndices = new int[match.MatchedCells.Count];

                for (int cellIndex = 0; cellIndex < match.MatchedCells.Count; cellIndex++)
                {
                    SlotCell cell = match.MatchedCells[cellIndex];
                    cellIndices[cellIndex] = SlotSpinResult.ToIndex(cell.Col, cell.Row);
                }

                patternPresentations[index] = new SlotPatternPresentationResult(
                    match.PresentationTitle,
                    match.Symbol,
                    match.MatchedCells.Count > 0 ? match.MatchedCells[0].Row : -1,
                    match.MatchedCells.Count > 0 ? match.MatchedCells[0].Col : -1,
                    match.MatchedCells.Count,
                    cellIndices,
                    $"{match.Symbol} x{match.MatchedCells.Count} / x{match.Multiplier:0.0}",
                    $"+{match.CalculatedValue} pts",
                    match.Definition.IsJackpot,
                    index,
                    match.CalculatedValue);
            }

            SlotCombatRequest request =
                combatRequestResult?.FinalRequest ?? SlotCombatRequest.Empty;
            SlotRelicTriggerPresentationResult[] relicPresentations =
                BuildRelicPresentations(
                    relicResult,
                    combatRequestResult,
                    combatRequestResult?.BaseRequest);
            var finalResult = new SlotFinalPresentationResult(
                request.Damage,
                request.Defense,
                request.AttackCount,
                request.HealAmount,
                BuildFinalSummaryText(request));

            return new SlotPresentationResult(
                slotTurnResult.SpinResult,
                patternPresentations,
                relicPresentations,
                finalResult);
        }

        private SlotRelicTriggerPresentationResult[] BuildRelicPresentations(
            RelicResolveResult relicResult,
            RunCombatRequestResult combatRequestResult,
            SlotCombatRequest baseRequest)
        {
            IReadOnlyList<RelicContributionDelta> contributions =
                CombineRelicContributions(relicResult, combatRequestResult);
            if (contributions == null || contributions.Count == 0)
            {
                return Array.Empty<SlotRelicTriggerPresentationResult>();
            }

            int attackCount = Math.Max(1, baseRequest?.AttackCount ?? 1);
            int attackPower = Math.Max(0, baseRequest?.Damage ?? 0) * attackCount;
            var results = new SlotRelicTriggerPresentationResult[contributions.Count];

            for (int index = 0; index < contributions.Count; index++)
            {
                RelicContributionDelta contribution = contributions[index];
                RelicDefinition definition = RelicCatalog.GetById(contribution.RelicId);
                int addedAttackPower = contribution.DamagePerHit * attackCount;
                int previousAttackPower = attackPower;
                attackPower += addedAttackPower;

                results[index] = new SlotRelicTriggerPresentationResult(
                    contribution.RelicId,
                    contribution.RelicName,
                    _relicIconResolver?.Invoke(contribution.RelicId),
                    definition?.Description ?? ResolveRelicDescription(
                        contribution.RelicId,
                        contribution.RelicName),
                    BuildRelicValueText(
                        previousAttackPower,
                        attackPower,
                        addedAttackPower,
                        contribution.Block,
                        contribution.Heal),
                    contribution.TriggerPatternIndex,
                    contribution.DamagePerHit,
                    contribution.Block,
                    contribution.Heal);
            }

            return results;
        }

        private static IReadOnlyList<RelicContributionDelta> CombineRelicContributions(
            RelicResolveResult relicResult,
            RunCombatRequestResult combatRequestResult)
        {
            IReadOnlyList<RelicContributionDelta> direct = relicResult?.Contributions;
            IReadOnlyList<RelicContributionDelta> derived =
                combatRequestResult?.DerivedHealContributions;

            int directCount = direct?.Count ?? 0;
            int derivedCount = derived?.Count ?? 0;

            if (directCount == 0)
            {
                return derivedCount == 0 ? Array.Empty<RelicContributionDelta>() : derived;
            }

            if (derivedCount == 0)
            {
                return direct;
            }

            var combined = new RelicContributionDelta[directCount + derivedCount];
            for (int index = 0; index < directCount; index++)
            {
                combined[index] = direct[index];
            }

            for (int index = 0; index < derivedCount; index++)
            {
                combined[directCount + index] = derived[index];
            }

            return combined;
        }

        private static string ResolveRelicDescription(string relicId, string relicName)
        {
            return TutorialBattleDefinition.TryGetRelicDescription(relicId, out string description)
                ? description
                : $"{relicName} 발동";
        }

        private static string BuildRelicValueText(
            int previousAttackPower,
            int attackPower,
            int addedAttackPower,
            int block,
            int heal)
        {
            var values = new List<string>(3);

            if (addedAttackPower > 0)
            {
                values.Add($"공격력 {previousAttackPower} → {attackPower} (+{addedAttackPower})");
            }

            if (block > 0)
            {
                values.Add($"방어 +{block}");
            }

            if (heal > 0)
            {
                values.Add($"회복 +{heal}");
            }

            return values.Count > 0 ? string.Join(" / ", values) : "효과 발동";
        }

        private static string BuildFinalSummaryText(SlotCombatRequest request)
        {
            if (request == null)
            {
                return "ATK 0 / DEF 0 / HEAL 0";
            }

            string summary = $"ATK {request.Damage} / DEF {request.Defense} / HEAL {request.HealAmount}";

            if (request.AttackCount > 1)
            {
                summary += $" / HIT {request.AttackCount}";
            }

            return summary;
        }

        internal static SlotSpinResult CreateInitialSlotDisplayResult()
        {
            IReadOnlyList<SlotSymbolType> symbols = SlotSymbolPool.Symbols;
            var displaySymbols = new SlotSymbolType[SlotSpinResult.CellCount];

            for (int index = 0; index < displaySymbols.Length; index++)
            {
                displaySymbols[index] = symbols[index % symbols.Count];
            }

            return new SlotSpinResult(displaySymbols);
        }

        private static bool ShouldRaiseLeverBeforeEvent(CombatEvent combatEvent)
        {
            if (combatEvent.Kind == CombatEventKind.BattleEnded)
            {
                return true;
            }

            return combatEvent.Kind == CombatEventKind.PhaseChanged &&
                combatEvent.Phase != BattlePhase.Resolving;
        }

        private static bool IsLastPlayerAttackPresentation(
            CombatEvent combatEvent,
            int eventIndex,
            IReadOnlyList<CombatEvent> events)
        {
            if (!IsPlayerAttackPresentation(combatEvent))
            {
                return false;
            }

            for (int index = eventIndex + 1; index < events.Count; index++)
            {
                CombatEvent nextEvent = events[index];
                if (IsPlayerAttackPresentation(nextEvent))
                {
                    return false;
                }

                if (nextEvent.Kind == CombatEventKind.PhaseChanged &&
                    nextEvent.Phase != BattlePhase.Resolving)
                {
                    break;
                }
            }

            return true;
        }

        private static bool IsPlayerAttackPresentation(CombatEvent combatEvent)
        {
            return combatEvent.Kind == CombatEventKind.EffectApplied &&
                combatEvent.Phase == BattlePhase.Resolving &&
                !combatEvent.IsPlayerParticipant;
        }
    }

    internal readonly struct SlotTurnRequest
    {
        internal SlotTurnRequest(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }

        internal CancellationToken CancellationToken { get; }
    }

    internal sealed class SlotTurnResult
    {
        internal SlotTurnResult(
            SlotSpinResult spinResult,
            IReadOnlyList<SlotPatternMatch> patternMatches,
            SlotPatternResult patternResult,
            SlotCombatRequest baseCombatRequest)
        {
            SpinResult = spinResult;
            PatternMatches = patternMatches;
            PatternResult = patternResult;
            BaseCombatRequest = baseCombatRequest;
        }

        internal SlotSpinResult SpinResult { get; }

        internal IReadOnlyList<SlotPatternMatch> PatternMatches { get; }

        internal SlotPatternResult PatternResult { get; }

        internal SlotCombatRequest BaseCombatRequest { get; }
    }
}
