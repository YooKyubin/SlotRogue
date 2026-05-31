using System;
using UnityEngine;

namespace SlotRogue.Data.Combat
{
    [CreateAssetMenu(menuName = "SlotRogue/Combat/Monster Turn Pattern")]
    public sealed class MonsterTurnPatternDefinition : ScriptableObject
    {
        public MonsterTurnStepDefinition[] turns = Array.Empty<MonsterTurnStepDefinition>();
    }
}
