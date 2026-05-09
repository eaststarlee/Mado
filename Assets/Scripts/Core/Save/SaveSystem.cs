using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Threading.Tasks;

// ── 슬롯 선택 UI용 메타 데이터 ────────────────────────
[Serializable]
public class SaveSlotMeta
{
    public int    slotIndex;
    public bool   isEmpty       = true;
    public string sceneName;
    public string spawnId;      
    public float  totalPlayTime;      
    public long   lastSavedAt;        
}

[Serializable]
public class SaveMetaFile
{
    public SaveSlotMeta[] slots = new SaveSlotMeta[3];
}


public static class SaveSystem
{
    private static string GetSlotPath(int slot) => Path.Combine(Application.persistentDataPath, $"slot_{slot}.json");
    private static string GetMetaPath() => Path.Combine(Application.persistentDataPath, "slot_list.json");

    // ── 슬롯 메타데이터 관리 ──────────────────────────────────────────
    public static SaveSlotMeta[] GetAllSlotMetas()
    {
        string metaPath = GetMetaPath();
        SaveMetaFile metaFile = null;
        
        if (File.Exists(metaPath))
        {
            try
            {
                string json = File.ReadAllText(metaPath);
                metaFile = JsonConvert.DeserializeObject<SaveMetaFile>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Meta Load Failed: {e.Message}");
            }
        }
        
        if (metaFile == null || metaFile.slots == null)
        {
            metaFile = new SaveMetaFile();
            for (int i = 0; i < 3; i++) metaFile.slots[i] = new SaveSlotMeta { slotIndex = i };
        }
        return metaFile.slots;
    }

    private static void UpdateSlotMeta(GameData data, int slot)
    {
        SaveSlotMeta[] metas = GetAllSlotMetas();
        if (data == null)
        {
            metas[slot] = new SaveSlotMeta { slotIndex = slot, isEmpty = true };
        }
        else
        {
            metas[slot] = new SaveSlotMeta
            {
                slotIndex = slot,
                isEmpty = false,
                sceneName = data.lastSceneName,
                spawnId = data.lastSpawnId,
                totalPlayTime = data.meta.totalPlayTime,
                lastSavedAt = data.meta.lastSavedAt
            };
        }
        
        SaveMetaFile metaFile = new SaveMetaFile { slots = metas };
        File.WriteAllText(GetMetaPath(), JsonConvert.SerializeObject(metaFile, Formatting.Indented));
    }

    public static void DeleteSlot(int slot)
    {
        string path = GetSlotPath(slot);
        if (File.Exists(path)) File.Delete(path);
        UpdateSlotMeta(null, slot);
    }
    
    // ── 본 데이터 관리 ──────────────────────────────────────────────

    // 게임 로드
    public static GameData Load(int slot)
    {
        string path = GetSlotPath(slot);
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<GameData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Load Failed: {e.Message}");
            return null;
        }
    }

    // 비동기 안전 저장 (원자적 쓰기)
    public static async Task<bool> SaveAsync(GameData data, int slot)
    {
        string path = GetSlotPath(slot);
        string tmpPath = path + ".tmp";
        string bakPath = path + ".bak";

        try
        {
            // 1. JSON 변환
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            // 2. 임시 파일에 쓰기
            await File.WriteAllTextAsync(tmpPath, json);

            // 3. 기존 파일이 있다면 백업으로 덮어쓰면서 원본 교체
            if (File.Exists(path))
            {
                File.Replace(tmpPath, path, bakPath);
            }
            else
            {
                File.Move(tmpPath, path);
            }

            // 4. 슬롯 메타 갱신
            UpdateSlotMeta(data, slot);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Save Failed: {e.Message}");
            if (File.Exists(tmpPath))
            {
                File.Delete(tmpPath);
            }
            return false;
        }
    }
}
