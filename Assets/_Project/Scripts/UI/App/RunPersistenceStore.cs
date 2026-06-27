using SlotRogue.UI.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.App
{
    /// <summary>
    /// 진행 중인 런 스냅샷을 PlayerPrefs(JSON)로 저장/복원합니다.
    /// 단일 슬롯이며, 런 시작/종료 시 무효화됩니다.
    /// </summary>
    public static class RunPersistenceStore
    {
        private const string SaveKey = "SlotRogue.Run.Save.v1";

        public static bool HasSaved => PlayerPrefs.HasKey(SaveKey);

        public static void Save(RunSaveData data)
        {
            if (data == null)
            {
                return;
            }

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static bool TryLoad(out RunSaveData data)
        {
            data = null;
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return false;
            }

            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<RunSaveData>(json);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[RunPersistence] Failed to parse save, discarding: {exception.Message}");
                Clear();
                return false;
            }

            return data != null;
        }

        public static void Clear()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return;
            }

            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }
    }
}
