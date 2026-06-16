using System;
using System.Collections.Generic;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyActionPresentationMap
    {
        private readonly Dictionary<int, EnemyActionPresentation> _presentationsByKey = new();

        public EnemyActionPresentationMap(IReadOnlyList<EnemyActionPresentation> presentations)
        {
            if (presentations == null)
            {
                return;
            }

            for (int index = 0; index < presentations.Count; index++)
            {
                EnemyActionPresentation presentation = presentations[index];
                if (presentation.ActionKey.IsValid)
                {
                    _presentationsByKey[presentation.ActionKey.Value] = presentation;
                }
            }
        }

        public static EnemyActionPresentationMap Empty { get; } =
            new(Array.Empty<EnemyActionPresentation>());

        public bool TryGet(EnemyActionKey actionKey, out EnemyActionPresentation presentation)
        {
            if (!actionKey.IsValid)
            {
                presentation = default;
                return false;
            }

            return _presentationsByKey.TryGetValue(actionKey.Value, out presentation);
        }
    }
}
