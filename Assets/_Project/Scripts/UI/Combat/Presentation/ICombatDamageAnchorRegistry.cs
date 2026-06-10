using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    public interface ICombatDamageAnchorRegistry
    {
        void SetEnemyDamageAnchor(CombatParticipantId participantId, RectTransform anchor);

        RectTransform ResolveDamageAnchor(CombatParticipantId participantId, bool isPlayerTarget);
    }
}
