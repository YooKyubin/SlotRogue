using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SlotRogue.UI.RunGame
{
    // RunGameFlowController가 의존하는 MonoBehaviour 협력자들의 추상입니다(ADR-0020).
    // 흐름 컨트롤러를 순수 C#으로 테스트할 수 있도록, 컨트롤러가 실제로 호출하는 멤버만 노출합니다.
    // 구체 구현(RunGameNavigator/BattleSceneCompositionRoot/RunTutorialOverlayView/RunDefeatView)은
    // 그대로 두고 인터페이스만 덧붙이며, SceneRoot가 구체 인스턴스를 주입합니다.

    /// <summary>화면 전환 협력자. <see cref="RunGameNavigator"/>가 구현합니다.</summary>
    public interface IRunGameNavigator
    {
        RunGameState CurrentState { get; }

        void GoTo(RunGameState nextState);
    }

    /// <summary>전투 씬 조립/제어 협력자. <see cref="GameFlow.BattleSceneCompositionRoot"/>가 구현합니다.</summary>
    public interface IBattleSceneController
    {
        UniTask PrepareBattleEntryAsync(CancellationToken cancellationToken);

        void BeginBattle();

        void SetTutorialSpinBlocked(bool blocked);

        void SetTutorialTargetSelectionBlocked(bool blocked);

        Sprite GetDefeatingMonsterPortrait();

        void FinalizePendingDefeat();

        bool TryRevive();
    }

    /// <summary>튜토리얼 안내 오버레이. <see cref="RunTutorialOverlayView"/>가 구현합니다.</summary>
    public interface ITutorialOverlay
    {
        void Hide();

        void ShowMessage(string message);
    }

    /// <summary>패배 화면의 몬스터 초상화 표시. <see cref="RunDefeatView"/>가 구현합니다.</summary>
    public interface IDefeatPortraitView
    {
        void SetMonsterPortrait(Sprite portrait);
    }
}
