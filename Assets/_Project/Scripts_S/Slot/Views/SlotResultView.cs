using System.Text;

using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.Slot.Views
{
    public sealed class SlotResultView : MonoBehaviour
    {
        private static readonly Color PatternHitColor = new Color(1f, 0.82f, 0.23f, 1f);
        private static readonly Color BaseAttackColor = new Color(0.66f, 0.82f, 1f, 1f);

        public void Bind(
            Text symbolsText,
            Text patternText,
            Text damageText,
            Text attackCountText,
            Text healText,
            Text criticalText,
            Text combatRequestText)
        {
            _symbolsText = symbolsText;
            _patternText = patternText;
            _damageText = damageText;
            _attackCountText = attackCountText;
            _healText = healText;
            _criticalText = criticalText;
            _combatRequestText = combatRequestText;
        }

        public void Display(
            SlotSpinResult spinResult,
            SlotPatternResult patternResult,
            SlotCalculationResult calculationResult,
            SlotCombatRequest combatRequest)
        {
            if (spinResult == null || patternResult == null || calculationResult == null || combatRequest == null)
            {
                return;
            }

            SetText(_symbolsText, spinResult.ToBoardString());
            SetText(_patternText, FormatPatternText(patternResult, combatRequest));
            SetTextColor(_patternText, patternResult.HasMatch ? PatternHitColor : BaseAttackColor);
            SetText(_damageText, $"Damage: {calculationResult.Damage}");
            SetText(_attackCountText, $"Attack Count: {calculationResult.AttackCount}");
            SetText(_healText, $"Heal: {calculationResult.HealAmount}");
            SetText(_criticalText, $"Critical: {calculationResult.IsCritical}");
            SetText(_combatRequestText, BuildCombatRequestText(combatRequest));

            SlotDebugLog.Info(BuildConsoleText(spinResult, patternResult, calculationResult));
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetTextColor(Text text, Color color)
        {
            if (text != null)
            {
                text.color = color;
            }
        }

        private static string FormatPatternText(SlotPatternResult patternResult, SlotCombatRequest combatRequest)
        {
            if (patternResult.HasMatch)
            {
                return $"PATTERN HIT: {patternResult.PatternName}";
            }

            return $"NO PATTERN - {combatRequest.PatternName}";
        }

        private static string BuildCombatRequestText(SlotCombatRequest combatRequest)
        {
            return $"Combat Request: attack={combatRequest.Damage}, defense={combatRequest.Defense}";
        }

        private static string BuildConsoleText(
            SlotSpinResult spinResult,
            SlotPatternResult patternResult,
            SlotCalculationResult calculationResult)
        {
            var builder = new StringBuilder();
            builder.Append("Symbols=[");
            builder.Append(spinResult.ToFlatString());
            builder.Append("], Pattern=");
            builder.Append(patternResult.PatternName);
            builder.Append(", PatternHit=");
            builder.Append(patternResult.HasMatch);
            builder.Append(", Damage=");
            builder.Append(calculationResult.Damage);
            builder.Append(", AttackCount=");
            builder.Append(calculationResult.AttackCount);
            builder.Append(", Heal=");
            builder.Append(calculationResult.HealAmount);
            builder.Append(", Critical=");
            builder.Append(calculationResult.IsCritical);

            return builder.ToString();
        }

        [SerializeField] private Text _symbolsText;
        [SerializeField] private Text _patternText;
        [SerializeField] private Text _damageText;
        [SerializeField] private Text _attackCountText;
        [SerializeField] private Text _healText;
        [SerializeField] private Text _criticalText;
        [SerializeField] private Text _combatRequestText;
    }
}
