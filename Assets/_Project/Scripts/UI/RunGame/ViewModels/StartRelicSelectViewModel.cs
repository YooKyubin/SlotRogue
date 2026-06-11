using System;
using System.Collections.Generic;
using SlotRogue.Relics.Pool;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.RunGame.ViewModels
{
    /// <summary>
    /// 시작 유물 선택 화면의 상태와 커맨드.
    /// v20.3: grade=Starter 유물(6종)만 제시하고, 선택 시 보유 유물에 추가한다.
    /// MonoBehaviour가 아닌 순수 C# 클래스.
    /// </summary>
    public sealed class StartRelicSelectViewModel
    {
        // ── 상태 ────────────────────────────────────────────────────────

        public IReadOnlyList<RelicDefinition> Relics { get; }
        public string Summary { get; private set; }

        // ── 이벤트 ──────────────────────────────────────────────────────

        /// <summary>유물이 선택되었을 때 발행합니다. (relicId)</summary>
        public event Action<string> RelicSelected;

        // ── 생성 ────────────────────────────────────────────────────────

        public StartRelicSelectViewModel()
        {
            Relics = RelicCatalog.Starters;
            Refresh();
        }

        // ── 커맨드 ──────────────────────────────────────────────────────

        /// <summary>View가 유물 버튼 클릭 시 호출합니다.</summary>
        public void SelectRelic(string relicId)
        {
            RelicDefinition relic = RelicCatalog.GetById(relicId);
            if (relic != null)
            {
                GameFlowSession.AddRelic(relic);
            }

            RelicSelected?.Invoke(relicId);
        }

        /// <summary>화면 진입 시 View가 최신 데이터로 갱신하도록 호출합니다.</summary>
        public void Refresh()
        {
            Summary = GameFlowSession.BuildSummary();
        }
    }
}
