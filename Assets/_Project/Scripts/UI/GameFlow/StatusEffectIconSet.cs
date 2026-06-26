using System;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    [CreateAssetMenu(
        fileName = "StatusEffectIconSet",
        menuName = "SlotRogue/UI/Status Effect Icon Set")]
    public sealed class StatusEffectIconSet : ScriptableObject
    {
        [SerializeField] private Sprite _burn;
        [SerializeField] private Sprite _infection;
        [SerializeField] private Sprite _vulnerable;
        [SerializeField] private Sprite _weaken;
        [SerializeField] private Sprite _lifesteal;
        [SerializeField] private Sprite _thorns;
        [SerializeField] private Sprite _freeze;

        public Sprite GetIcon(StatusEffectKind kind)
        {
            Sprite icon;
            switch (kind)
            {
                case StatusEffectKind.Burn:
                    icon = _burn;
                    break;
                case StatusEffectKind.Infection:
                    icon = _infection;
                    break;
                case StatusEffectKind.Vulnerable:
                    icon = _vulnerable;
                    break;
                case StatusEffectKind.Weaken:
                    icon = _weaken;
                    break;
                case StatusEffectKind.Lifesteal:
                    icon = _lifesteal;
                    break;
                case StatusEffectKind.Thorns:
                    icon = _thorns;
                    break;
                case StatusEffectKind.Freeze:
                    icon = _freeze;
                    break;
                case StatusEffectKind.None:
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        "Status effect kind does not have an icon mapping.");
            }

            if (icon == null)
            {
                throw new InvalidOperationException(
                    $"Status effect icon is not assigned for {kind}.");
            }

            return icon;
        }
    }
}
