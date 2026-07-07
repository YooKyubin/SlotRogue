using SlotRogue.UI.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.App
{
    /// <summary>
    /// 진행 중인 런을 백그라운드 전환/종료 시점에 저장하고, 런 시작/종료 시 저장본을 무효화합니다.
    /// 모바일 OS는 광고 시청·백그라운드 중 앱을 회수할 수 있으므로 OnApplicationPause(true)에서
    /// 저장해야 런 진행이 보존됩니다. 씬 배선 없이 런타임에 스스로 부트스트랩됩니다.
    /// </summary>
    public sealed class RunPersistenceService : MonoBehaviour
    {
        private static RunPersistenceService _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null)
            {
                return;
            }

            var host = new GameObject(nameof(RunPersistenceService));
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<RunPersistenceService>();
        }

        /// <summary>
        /// 저장된 런이 있으면 GameFlowSession을 복원합니다. 성공 시 true.
        /// 로비 Play 흐름이 새 런 대신 이어하기를 결정할 때 사용합니다.
        /// </summary>
        public static bool TryResume()
        {
            if (!RunPersistenceStore.TryLoad(out RunSaveData data))
            {
                return false;
            }

            return GameFlowSession.RestoreFromSave(data);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            // 런 시작/종료 시 저장본 무효화: 끝난 런이 되살아나거나 새 런이 옛 저장본을 덮지 않게 한다.
            GameFlowSession.RunStarted += HandleRunBoundary;
            GameFlowSession.RunEnded += HandleRunBoundary;
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            GameFlowSession.RunStarted -= HandleRunBoundary;
            GameFlowSession.RunEnded -= HandleRunBoundary;
            _instance = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveIfResumable();
            }
        }

        private void OnApplicationQuit()
        {
            SaveIfResumable();
        }

        private static void HandleRunBoundary()
        {
            RunPersistenceStore.Clear();
        }

        private static void SaveIfResumable()
        {
            if (GameFlowSession.IsResumable)
            {
                RunPersistenceStore.Save(GameFlowSession.CaptureSave());
                return;
            }

            // 사망(부활 대기) 상태로 백그라운드에 들어가면, 이전 전투 경계에서 남아 있던 저장본을
            // 무효화한다. 그러지 않으면 부활 대기 중 강제 종료 후 이어하기로 죽은 웨이브를 처음부터
            // 다시 시작할 수 있다(익스플로잇). 타이틀/로비의 이어하기 대기 저장본(HasRun=false)은
            // 건드리지 않도록, 진행 중인 런이 사망한 경우에만 지운다.
            if (GameFlowSession.HasRun && GameFlowSession.IsDefeatPending)
            {
                RunPersistenceStore.Clear();
            }
        }
    }
}
