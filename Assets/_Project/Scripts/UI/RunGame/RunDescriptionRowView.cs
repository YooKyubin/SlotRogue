using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    public sealed class RunDescriptionRowView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _attackPowerText;
        [SerializeField] private TMP_Text _probabilityText;
        [SerializeField] private TMP_Text _multiplierText;

        private bool _missingReferenceErrorLogged;

        internal Image Icon => _icon;

        internal TMP_Text TitleText => _titleText;

        internal TMP_Text AttackPowerText => _attackPowerText;

        internal TMP_Text ProbabilityText => _probabilityText;

        internal TMP_Text MultiplierText => _multiplierText;

        internal void ValidateRequiredReferences()
        {
            // 심볼 row는 Icon/Title/AttackPower/Probability를, 패턴 row는 Title/Multiplier만 쓴다.
            // 타입마다 필요한 필드가 다르므로 특정 필드를 일괄 강제하지 않고,
            // 모든 참조가 하나도 배선되지 않은 '완전 미배선' row만 오류로 잡는다(setter는 모두 null-safe).
            bool hasAnyReference =
                _icon != null ||
                _titleText != null ||
                _attackPowerText != null ||
                _probabilityText != null ||
                _multiplierText != null;

            if (_missingReferenceErrorLogged || hasAnyReference)
            {
                return;
            }

            _missingReferenceErrorLogged = true;
            Debug.LogError(
                "[RunDescriptionRowView] Description row has no references wired. " +
                "Wire the fields this row actually uses (symbol: Icon/Title/Attack/Probability, pattern: Title/Multiplier).",
                this);
        }
    }
}
