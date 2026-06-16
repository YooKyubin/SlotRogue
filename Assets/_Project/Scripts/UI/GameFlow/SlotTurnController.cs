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
        private readonly SlotMachineViewModel _slotViewModel;
        private readonly RunBattleSpinSequence _spinSequence;
        private readonly SlotPresentationManager _presentationManager;
        private readonly Func<string, Sprite> _relicIconResolver;

        internal SlotTurnController(
            SlotMachineViewModel slotViewModel,
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
                BuildRelicPresentations(relicResult, combatRequestResult?.BaseRequest);
            var finalResult = new SlotFinalPresentationResult(
                request.Damage,
                request.Defense,
                request.AttackCount,
                request.HealAmount,
                $"DMG {request.Damage} / DEF {request.Defense}");

            return new SlotPresentationResult(
                slotTurnResult.SpinResult,
                patternPresentations,
                relicPresentations,
                finalResult);
        }

        private SlotRelicTriggerPresentationResult[] BuildRelicPresentations(
            RelicResolveResult relicResult,
            SlotCombatRequest baseRequest)
        {
            IReadOnlyList<RelicContributionDelta> contributions = relicResult?.Contributions;
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
                    definition?.Description ?? $"{contribution.RelicName} 발동",
                    BuildRelicValueText(
                        previousAttackPower,
                        attackPower,
                        addedAttackPower,
                        contribution.Block,
                        contribution.Heal),
                    contribution.TriggerPatternIndex);
            }

            return results;
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
