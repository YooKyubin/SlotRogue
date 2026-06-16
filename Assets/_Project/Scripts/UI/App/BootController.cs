using Cysharp.Threading.Tasks;
using SlotRogue.UI.Iap;
using SlotRogue.UI.Leaderboard;
using UnityEngine;

namespace SlotRogue.UI.App
{
    /// <summary>
    /// BootScene에 붙이는 앱 진입점입니다.
    /// 앱 초기화(현재는 없음)를 마친 뒤 GameStart 씬으로 이동합니다.
    ///
    /// 나중에 SDK / Firebase / 광고 / 저장 데이터 초기화를 추가할 때는
    /// InitializeAsync 안에 await 단계를 덧붙이면 됩니다. 초기화 흐름을
    /// 한 곳에 모아 두기 위해 async 구조를 미리 잡아 둡니다.
    /// </summary>
    public sealed class BootController : MonoBehaviour
    {
        private void Awake()
        {
            IapStoreConnectionCallbacks.Register();
        }

        private void Start()
        {
            // 이벤트 진입점에서의 fire-and-forget은 async void 대신 UniTaskVoid를 사용합니다.
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            AdsRemoveState.Initialize();

            // 온라인 기능은 본편 진입을 막지 않도록 백그라운드에서 준비합니다.
            SlotRogueLeaderboardService.InitializeAsync().Forget();
            await UniTask.CompletedTask;

            GameSceneLoader.LoadGameStart();
        }
    }
}
