using System.Collections.Generic;

namespace SlotRogue.Relics.Pool
{
    /// <summary>
    /// 런타임 유물 카탈로그. v29 기획표의 단일 출처인 <see cref="RelicSpecCatalog"/>(41종)를
    /// 기존 소비처(상점·전투·세이브·부트)가 쓰는 <see cref="RelicDefinition"/> 형태로 변환해 제공한다.
    /// 구 v23 80종 하드코딩 데이터는 v29 전면 교체로 삭제됨. v29엔 시작(Starter) 등급이 없어
    /// <see cref="Starters"/>는 항상 빈 목록이다(스타터 선택 단계는 이미 비활성).
    /// 효과 실행 엔진(RelicSpec 러너)은 P1에서 붙는다 — 그전까지 EffectType은 Special로 두어
    /// 기존 <c>RelicEffectRunner</c>가 아무 효과도 내지 않게 한다(껍데기 상태).
    /// </summary>
    public static class RelicCatalog
    {
        private static readonly RelicDefinition[] AllRelics = BuildAll();
        private static readonly Dictionary<string, RelicDefinition> ById = BuildIndex(AllRelics);

        public static IReadOnlyList<RelicDefinition> All => AllRelics;

        /// <summary>v29엔 시작 유물이 없다. 항상 빈 목록.</summary>
        public static IReadOnlyList<RelicDefinition> Starters { get; } =
            System.Array.Empty<RelicDefinition>();

        /// <summary>상점/보상 추첨 풀 — v29 전 유물(시작 등급 없음).</summary>
        public static IReadOnlyList<RelicDefinition> RewardPool => AllRelics;

        public static RelicDefinition GetById(string id) =>
            !string.IsNullOrEmpty(id) && ById.TryGetValue(id, out RelicDefinition relic) ? relic : null;

        public static IReadOnlyList<RelicDefinition> RewardByGrade(RelicGrade grade) =>
            Filter(AllRelics, r => r.Grade == grade);

        private static RelicDefinition[] BuildAll()
        {
            IReadOnlyList<RelicSpec> specs = RelicSpecCatalog.All;
            var relics = new RelicDefinition[specs.Count];
            for (int index = 0; index < specs.Count; index++)
            {
                relics[index] = FromSpec(specs[index]);
            }

            return relics;
        }

        /// <summary>v29 <see cref="RelicSpec"/> 한 개를 런타임 <see cref="RelicDefinition"/>으로 변환한다.</summary>
        private static RelicDefinition FromSpec(RelicSpec spec)
        {
            RelicRole role = RoleFor(spec.Category);
            return new RelicDefinition(
                spec.Id,
                spec.Grade,
                spec.DisplayName,
                RelicIconKeys.DefaultFor(role),
                role,
                RelicTriggerType.Passive,
                RelicEffectType.Special,
                null,
                null,
                0,
                0,
                0,
                0,
                0,
                EnemyStatusRequirement.None,
                isStarter: false,
                phase1: true,
                description: spec.Description,
                intent: spec.DevNote,
                qaRisk: string.Empty,
                category: spec.Category,
                life: LifeString(spec.Lifetime.Kind),
                shopSlot: "slot",
                price: spec.Price,
                maxCopies: spec.MaxCopies);
        }

        private static RelicRole RoleFor(string category)
        {
            return category switch
            {
                "combat" => RelicRole.Damage,
                _ => RelicRole.Utility,
            };
        }

        private static string LifeString(RelicLifetimeKind kind)
        {
            return kind switch
            {
                RelicLifetimeKind.ConsumableUses => "euse",
                RelicLifetimeKind.ConsumableWaves => "ewave",
                _ => "perm",
            };
        }

        private static Dictionary<string, RelicDefinition> BuildIndex(RelicDefinition[] relics)
        {
            var map = new Dictionary<string, RelicDefinition>(relics.Length);
            for (int index = 0; index < relics.Length; index++)
            {
                map[relics[index].Id] = relics[index];
            }

            return map;
        }

        private static RelicDefinition[] Filter(
            RelicDefinition[] source,
            System.Predicate<RelicDefinition> predicate)
        {
            var list = new List<RelicDefinition>();
            for (int index = 0; index < source.Length; index++)
            {
                if (predicate(source[index]))
                {
                    list.Add(source[index]);
                }
            }

            return list.ToArray();
        }
    }
}
