using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyFormationPositioner : MonoBehaviour
    {
        [SerializeField] private Transform[] _oneEnemyAnchors = Array.Empty<Transform>();
        [SerializeField] private Transform[] _twoEnemyAnchors = Array.Empty<Transform>();
        [SerializeField] private Transform[] _threeEnemyAnchors = Array.Empty<Transform>();

        private readonly HashSet<string> _loggedConfigurationErrors = new();

        public void ApplyLayout(IReadOnlyList<EnemyFormationSlotView> slotViews, IReadOnlyList<int> occupiedSlotIndices)
        {
            if (!TryBuildLayout(slotViews, occupiedSlotIndices, out EnemyFormationSlotView[] orderedSlotViews, out Transform[] anchors))
            {
                return;
            }

            for (int index = 0; index < orderedSlotViews.Length; index++)
            {
                orderedSlotViews[index].Root.position = anchors[index].position;
            }
        }

        private bool TryBuildLayout(
            IReadOnlyList<EnemyFormationSlotView> slotViews,
            IReadOnlyList<int> occupiedSlotIndices,
            out EnemyFormationSlotView[] orderedSlotViews,
            out Transform[] anchors)
        {
            orderedSlotViews = null;
            anchors = null;

            if (slotViews == null || occupiedSlotIndices == null)
            {
                LogConfigurationError("Slot views and occupied slot indices must be provided.");
                return false;
            }

            int enemyCount = occupiedSlotIndices.Count;
            if (enemyCount < 1 || enemyCount > 3)
            {
                LogConfigurationError($"Enemy count must be between 1 and 3, but was {enemyCount}.");
                return false;
            }

            Transform[] selectedAnchors = ResolveAnchors(enemyCount);
            if (selectedAnchors == null || selectedAnchors.Length != enemyCount)
            {
                LogConfigurationError($"Anchor count for {enemyCount} enemies must be {enemyCount}.");
                return false;
            }

            for (int index = 0; index < selectedAnchors.Length; index++)
            {
                if (selectedAnchors[index] == null)
                {
                    LogConfigurationError($"Anchor {index} for {enemyCount} enemies is missing.");
                    return false;
                }
            }

            var sortedSlotIndices = new int[enemyCount];
            var usedSlotIndices = new HashSet<int>();
            for (int index = 0; index < enemyCount; index++)
            {
                int slotIndex = occupiedSlotIndices[index];
                if (slotIndex < 0 || slotIndex >= slotViews.Count)
                {
                    LogConfigurationError($"Formation slot {slotIndex} is outside the available slot range.");
                    return false;
                }

                if (!usedSlotIndices.Add(slotIndex))
                {
                    LogConfigurationError($"Formation slot {slotIndex} is duplicated.");
                    return false;
                }

                if (slotViews[slotIndex] == null)
                {
                    LogConfigurationError($"Formation slot view {slotIndex} is missing.");
                    return false;
                }

                sortedSlotIndices[index] = slotIndex;
            }

            Array.Sort(sortedSlotIndices);
            orderedSlotViews = new EnemyFormationSlotView[enemyCount];
            for (int index = 0; index < sortedSlotIndices.Length; index++)
            {
                orderedSlotViews[index] = slotViews[sortedSlotIndices[index]];
            }

            anchors = selectedAnchors;
            return true;
        }

        private Transform[] ResolveAnchors(int enemyCount)
        {
            return enemyCount switch
            {
                1 => _oneEnemyAnchors,
                2 => _twoEnemyAnchors,
                3 => _threeEnemyAnchors,
                _ => null,
            };
        }

        private void LogConfigurationError(string message)
        {
            if (!_loggedConfigurationErrors.Add(message))
            {
                return;
            }

            Debug.LogError($"[EnemyFormationPositioner] {message}");
        }
    }
}
