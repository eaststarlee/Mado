using UnityEngine;
using System.IO;

[System.Serializable]
public class SaveData
{
    // 나중에 마나, 체력, 능력치 등을 여기에 추가하시면 됩니다.
    public string lastSceneName = "S_FR_001_Start";
    public string lastSpawnId = "Default";
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    
    public SaveData CurrentData { get; private set; }

    private string saveFilePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 윈도우 기준: C:\Users\사용자명\AppData\LocalLow\회사명\게임명\mado_save.json
        saveFilePath = Path.Combine(Application.persistentDataPath, "mado_save.json");
    }

    /// <summary>
    /// 저장된 파일이 있는지 확인
    /// </summary>
    public bool HasSaveData()
    {
        return File.Exists(saveFilePath);
    }

    /// <summary>
    /// 게임 저장 (RoomTransition이나 SavePoint에서 호출)
    /// </summary>
    public void SaveGame(string sceneName, string spawnId)
    {
        if (CurrentData == null) CurrentData = new SaveData();

        CurrentData.lastSceneName = sceneName;
        CurrentData.lastSpawnId = spawnId;

        string json = JsonUtility.ToJson(CurrentData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"[SaveManager] 게임 저장 완료: 맵({sceneName}) / 스폰({spawnId})");
    }

    /// <summary>
    /// 게임 불러오기 (시작할 때 Bootstrapper가 호출)
    /// </summary>
    public SaveData LoadGame()
    {
        if (HasSaveData())
        {
            string json = File.ReadAllText(saveFilePath);
            CurrentData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[SaveManager] 게임 로드 완료: 맵({CurrentData.lastSceneName})");
        }
        else
        {
            // 데이터가 없으면 새 게임 데이터 리턴
            CurrentData = new SaveData();
            Debug.Log("[SaveManager] 세이브가 없습니다. 새 게임 데이터를 생성합니다.");
        }
        return CurrentData;
    }

    /// <summary>
    /// 데이터 삭제 (옵션)
    /// </summary>
    public void DeleteSaveData()
    {
        if (HasSaveData())
        {
            File.Delete(saveFilePath);
            CurrentData = new SaveData();
            Debug.Log("[SaveManager] 세이브 파일 삭제 완료");
        }
    }
}
