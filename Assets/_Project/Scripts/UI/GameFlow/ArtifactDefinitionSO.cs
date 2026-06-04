using SlotRogue.Slot.Data;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    [CreateAssetMenu(menuName = "SlotRogue/Artifact/Artifact Definition", fileName = "NewArtifact")]
    public sealed class ArtifactDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _artifactId;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea(2, 4)] private string _description;
        [SerializeField] private ArtifactCategory _category;

        [Header("Trigger")]
        [SerializeField] private SlotSymbolType _targetSymbol;
        [SerializeField] private int _minimumMatchLength = 3;

        [Header("Effect")]
        [SerializeField] private ArtifactEffectKind _effectKind;
        [Tooltip("BonusDamage/Defense/Heal용 수치")]
        [SerializeField] private int _bonusAmount;
        [Tooltip("화염/빙결: 지속 턴 수")]
        [SerializeField] private int _statusDuration;
        [Tooltip("화염: 턴당 피해 / 독: 스택 수 (최대 5)")]
        [SerializeField] private int _statusMagnitude;
        [Tooltip("Refresh: 중첩 시 지속 시간 초기화 (화염·빙결) / Stack: 강도 누적 (독)")]
        [SerializeField] private StatusStackBehavior _statusStackBehavior;

        public string ArtifactId => _artifactId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public ArtifactCategory Category => _category;
        public SlotSymbolType TargetSymbol => _targetSymbol;
        public int MinimumMatchLength => _minimumMatchLength;
        public ArtifactEffectKind EffectKind => _effectKind;
        public int BonusAmount => _bonusAmount;
        public int StatusDuration => _statusDuration;
        public int StatusMagnitude => _statusMagnitude;
        public StatusStackBehavior StatusStackBehavior => _statusStackBehavior;

        internal static ArtifactDefinitionSO Create(
            string id,
            string displayName,
            string description,
            ArtifactCategory category,
            SlotSymbolType targetSymbol,
            int minimumMatchLength,
            ArtifactEffectKind effectKind,
            int bonusAmount = 0,
            int statusDuration = 0,
            int statusMagnitude = 0,
            StatusStackBehavior statusStackBehavior = StatusStackBehavior.Refresh)
        {
            var so = CreateInstance<ArtifactDefinitionSO>();
            so._artifactId = id;
            so._displayName = displayName;
            so._description = description;
            so._category = category;
            so._targetSymbol = targetSymbol;
            so._minimumMatchLength = minimumMatchLength;
            so._effectKind = effectKind;
            so._bonusAmount = bonusAmount;
            so._statusDuration = statusDuration;
            so._statusMagnitude = statusMagnitude;
            so._statusStackBehavior = statusStackBehavior;
            return so;
        }
    }
}
