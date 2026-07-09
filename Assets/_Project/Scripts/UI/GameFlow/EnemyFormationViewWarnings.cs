using System.Collections.Generic;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    internal sealed class EnemyFormationViewWarnings
    {
        private readonly HashSet<int> _missingFormationSlotsLogged = new();
        private readonly HashSet<int> _missingDamageAnchorsLogged = new();
        private readonly HashSet<int> _missingSlotDamageAnchorsLogged = new();
        private readonly HashSet<int> _missingCombatVisualSlotsLogged = new();
        private readonly HashSet<int> _missingDamageVFXSlotsLogged = new();

        public void MissingFormationSlot(int slotIndex)
        {
            if (!_missingFormationSlotsLogged.Add(slotIndex))
            {
                return;
            }

            Debug.LogError(
                $"[EnemyFormationView] Formation slot {slotIndex} is missing. " +
                "Assign an EnemyFormationSlotView for every configured formation slot.");
        }

        public void MissingDamageAnchor(CombatParticipantId participantId)
        {
            int participantKey = ParticipantKey(participantId);
            if (!_missingDamageAnchorsLogged.Add(participantKey))
            {
                return;
            }

            Debug.LogError(
                $"[EnemyFormationView] Damage anchor mapping is missing for enemy participant {ParticipantLabel(participantId)}. " +
                "Render the enemy slot state before playing floating damage, and ensure the slot has a DamageAnchor.");
        }

        public void MissingSlotDamageAnchor(int slotIndex, CombatParticipantId participantId)
        {
            if (!_missingSlotDamageAnchorsLogged.Add(slotIndex))
            {
                return;
            }

            Debug.LogError(
                $"[EnemyFormationView] DamageAnchor is missing on formation slot {slotIndex} " +
                $"for enemy participant {ParticipantLabel(participantId)}.");
        }

        public void MissingCombatVisualSlot(CombatParticipantId participantId)
        {
            int participantKey = ParticipantKey(participantId);
            if (!_missingCombatVisualSlotsLogged.Add(participantKey))
            {
                return;
            }

            Debug.LogError(
                $"[EnemyFormationView] Combat visual slot mapping is missing for enemy participant {ParticipantLabel(participantId)}. " +
                "Render the enemy slot state before requesting combat visual animation.");
        }

        public void MissingDamageVFXSlot(CombatParticipantId participantId)
        {
            int participantKey = ParticipantKey(participantId);
            if (!_missingDamageVFXSlotsLogged.Add(participantKey))
            {
                return;
            }

            Debug.LogError(
                $"[EnemyFormationView] Damage VFX slot mapping is missing for enemy participant {ParticipantLabel(participantId)}. " +
                "Render the enemy slot state before requesting damage VFX.");
        }

        private static int ParticipantKey(CombatParticipantId participantId)
        {
            return participantId.IsValid ? participantId.Value : 0;
        }

        private static string ParticipantLabel(CombatParticipantId participantId)
        {
            return participantId.IsValid ? participantId.Value.ToString() : "invalid";
        }
    }
}
