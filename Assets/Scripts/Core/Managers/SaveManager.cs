using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

// ── 슬롯 선택 UI에 표시할 메타 데이터 ────────────────────────
[System.Serializable]
public class SaveSlotMeta
{
    public int    slotIndex;
    public bool   isEmpty       = true;
    public string sceneName;
    public float  totalPlayTime;      // 초 단위
    public long   lastSavedAt;        // Unix 타임스탬프
}

[System.Serializable]
public class SaveMetaFile
{
    public SaveSlotMeta[] slots = new SaveSlotMeta[3];
}

// ─────────────────────────────────────────────────────────────

/// <summary>
/// 상용급 슬롯 기반 세이브 시스템 싱글톤.
///
/// ■ 파일 구조:
///   persistentDataPath/
///   ├── mado_slot0.json  ← 실제 세이브 데이터
///   ├── mado_slot0.bak   ← 직전 저장 백업 (손상 시 자동 복구)
///   ├── mado_slot1.json / .bak
///   ├── mado_slot2.json / .bak
///   └── mado_meta.json   ← 슬롯 선택 UI용 메타 (손상 시 슬롯 파일에서 재구성)
///
/// ■ 비동기 저장: Task.Run으로 디스크 I/O를 백그라운드 처리 (메인 스레드 블로킹 없음)
/// ■ ISaveable: Awake 등록 → BroadcastSave/Load로 일괄 처리
/// ■ 마이그레이션: 로드 직후 SaveMigrator.Migrate() 자동 실행
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // ── 상태 ─────────────────────────────────────────────
    public SaveData CurrentData   { get; private set; }
    public int ActiveSlotIndex    { get; private set; } = -1;
    public bool IsSaving          { get; private set; }

    // ── ISaveable 등록부 ─────────────────────────────────
    private readonly List<ISaveable> _saveables = new();

    // ── 런타임 HashSet 캐시 (O(1) 플래그 조회) ───────────
    private HashSet<string> _defeatedBossCache;
    private HashSet<string> _unlockedAbilityCache;
    private HashSet<string> _collectedItemCache;
    private HashSet<string> _openedDoorCache;

    // ── 파일 경로 헬퍼 ───────────────────────────────────
    private string SlotPath(int idx)    => Path.Combine(Application.persistentDataPath, $"mado_slot{idx}.json");
    private string SlotBakPath(int idx) => Path.Combine(Application.persistentDataPath, $"mado_slot{idx}.bak");
    private string MetaPath             => Path.Combine(Application.persistentDataPath, "mado_meta.json");

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────────────────
    // ISaveable 등록 / 해제
    // ─────────────────────────────────────────────────────

    /// <summary>컴포넌트 Awake()에서 호출하여 세이브 브로드캐스트에 참여합니다.</summary>
    public void Register(ISaveable obj)
    {
        if (!_saveables.Contains(obj)) _saveables.Add(obj);
    }

    /// <summary>컴포넌트 OnDestroy()에서 호출하여 등록을 해제합니다.</summary>
    public void Unregister(ISaveable obj) => _saveables.Remove(obj);

    // ─────────────────────────────────────────────────────
    // 슬롯 관리
    // ─────────────────────────────────────────────────────

    /// <summary>슬롯 선택 UI에 표시할 메타 데이터 3개를 반환합니다.</summary>
    public SaveSlotMeta[] GetAllSlotMetas()
    {
        SaveMetaFile meta = LoadMeta();
        return meta.slots;
    }

    /// <summary>활성 슬롯을 지정하고 데이터를 로드합니다. 성공 여부를 반환합니다.</summary>
    public bool LoadSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 2)
        {
            Debug.LogError($"[SaveManager] 유효하지 않은 슬롯 인덱스: {slotIndex}");
            return false;
        }

        string path    = SlotPath(slotIndex);
        string bakPath = SlotBakPath(slotIndex);

        string json = TryReadFile(path) ?? TryReadFile(bakPath);

        if (json == null)
        {
            Debug.Log($"[SaveManager] 슬롯 {slotIndex}: 저장 파일 없음. 새 게임 데이터 생성.");
            CurrentData = CreateNewSaveData(slotIndex);
        }
        else
        {
            try
            {
                CurrentData = JsonUtility.FromJson<SaveData>(json);
                CurrentData = SaveMigrator.Migrate(CurrentData);       // 버전 마이그레이션
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 슬롯 {slotIndex} 파싱 실패: {e.Message} — 새 게임으로 초기화.");
                CurrentData = CreateNewSaveData(slotIndex);
            }
        }

        ActiveSlotIndex = slotIndex;
        BuildRuntimeCaches(CurrentData);
        return true;
    }

    /// <summary>새 게임 데이터로 슬롯을 초기화합니다.</summary>
    public void NewGame(int slotIndex)
    {
        CurrentData = CreateNewSaveData(slotIndex);
        ActiveSlotIndex = slotIndex;
        BuildRuntimeCaches(CurrentData);
        Debug.Log($"[SaveManager] 슬롯 {slotIndex} 새 게임 초기화 완료.");
    }

    /// <summary>슬롯 파일과 백업 파일을 삭제합니다. 확인 팝업은 UI(SlotSelectScreen) 책임.</summary>
    public void DeleteSlot(int slotIndex)
    {
        TryDeleteFile(SlotPath(slotIndex));
        TryDeleteFile(SlotBakPath(slotIndex));
        UpdateMeta(slotIndex, null);
        Debug.Log($"[SaveManager] 슬롯 {slotIndex} 삭제 완료.");
    }

    // ─────────────────────────────────────────────────────
    // 저장 / 불러오기
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// 현재 슬롯에 비동기 저장.
    /// 1) ISaveable 브로드캐스트로 데이터 수집
    /// 2) Task.Run으로 디스크 I/O를 백그라운드 처리 (메인 스레드 블로킹 없음)
    /// 3) 씬 전환 중이면 완료까지 대기
    /// </summary>
    public async Task SaveAsync()
    {
        if (ActiveSlotIndex < 0)
        {
            Debug.LogWarning("[SaveManager] 활성 슬롯이 없습니다. 저장을 건너뜁니다.");
            return;
        }

        // 씬 전환 중이면 완료까지 대기
        if (SceneLoader.Instance != null && SceneLoader.Instance.IsTransitioning)
        {
            Debug.Log("[SaveManager] 씬 전환 중 — 완료 후 저장합니다.");
            while (SceneLoader.Instance.IsTransitioning)
                await Task.Delay(100);
        }

        IsSaving = true;

        // ISaveable 브로드캐스트 (메인 스레드에서 수집)
        BroadcastSave();

        // 플레이 시간 갱신
        if (PlaytimeTracker.Instance != null)
            CurrentData.totalPlayTime = PlaytimeTracker.Instance.ElapsedSeconds;

        CurrentData.lastSavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 직렬화 (메인 스레드에서 수행 — JsonUtility 제약)
        string json    = JsonUtility.ToJson(CurrentData, prettyPrint: true);
        string path    = SlotPath(ActiveSlotIndex);
        string bakPath = SlotBakPath(ActiveSlotIndex);
        int    slot    = ActiveSlotIndex;

        // 디스크 I/O를 백그라운드 스레드에서 처리
        await Task.Run(() =>
        {
            try
            {
                // 기존 파일 → .bak 백업 (원자적 교체)
                if (File.Exists(path)) File.Copy(path, bakPath, overwrite: true);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 저장 실패 (슬롯 {slot}): {e.Message}");
            }
        });

        // 메타 업데이트
        UpdateMeta(slot, CurrentData);

        IsSaving = false;
        Debug.Log($"[SaveManager] 슬롯 {slot} 저장 완료.");
    }

    /// <summary>사망 처리: SavePoint 저장 데이터의 Checkpoint 부분만 복원합니다.</summary>
    public void RollbackToCheckpoint()
    {
        // 최신 저장된 슬롯에서 HP/SP/위치만 재로드
        string json = TryReadFile(SlotPath(ActiveSlotIndex)) ?? TryReadFile(SlotBakPath(ActiveSlotIndex));
        if (json == null) return;

        try
        {
            var saved = JsonUtility.FromJson<SaveData>(json);
            CurrentData.currentHP    = saved.currentHP;
            CurrentData.currentSP    = saved.currentSP;
            CurrentData.lastSceneName = saved.lastSceneName;
            CurrentData.lastSpawnId   = saved.lastSpawnId;
            BroadcastLoad();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 체크포인트 롤백 실패: {e.Message}");
        }
    }

    // ─────────────────────────────────────────────────────
    // ISaveable 브로드캐스트
    // ─────────────────────────────────────────────────────

    /// <summary>등록된 모든 ISaveable에 OnSave를 호출하여 CurrentData에 상태를 기록합니다.</summary>
    public void BroadcastSave()
    {
        foreach (var s in _saveables) s.OnSave(CurrentData);
    }

    /// <summary>등록된 모든 ISaveable에 OnLoad를 호출하여 CurrentData의 상태를 복원합니다.</summary>
    public void BroadcastLoad()
    {
        foreach (var s in _saveables) s.OnLoad(CurrentData);
    }

    // ─────────────────────────────────────────────────────
    // 플래그 조회 (O(1), HashSet 캐시 사용)
    // ─────────────────────────────────────────────────────

    public bool IsBossDefeated(string bossId)    => _defeatedBossCache?.Contains(bossId) ?? false;
    public bool HasAbility(string abilityId)     => _unlockedAbilityCache?.Contains(abilityId) ?? false;
    public bool IsItemCollected(string itemId)   => _collectedItemCache?.Contains(itemId) ?? false;
    public bool IsDoorOpened(string doorId)      => _openedDoorCache?.Contains(doorId) ?? false;

    // ─────────────────────────────────────────────────────
    // 플래그 설정 (List + HashSet 동시 갱신, 중복 자동 방지)
    // ─────────────────────────────────────────────────────

    public void SetBossDefeated(string bossId)
    {
        if (_defeatedBossCache.Add(bossId)) CurrentData.defeatedBosses.Add(bossId);
    }

    public void UnlockAbility(string abilityId)
    {
        if (_unlockedAbilityCache.Add(abilityId)) CurrentData.unlockedAbilities.Add(abilityId);
    }

    public void CollectItem(string itemId)
    {
        if (_collectedItemCache.Add(itemId)) CurrentData.collectedItems.Add(itemId);
    }

    public void OpenDoor(string doorId)
    {
        if (_openedDoorCache.Add(doorId)) CurrentData.openedDoors.Add(doorId);
    }

    // ─────────────────────────────────────────────────────
    // 내부 유틸리티
    // ─────────────────────────────────────────────────────

    /// <summary>로드 직후 HashSet 런타임 캐시를 빌드합니다.</summary>
    private void BuildRuntimeCaches(SaveData data)
    {
        _defeatedBossCache    = new HashSet<string>(data.defeatedBosses);
        _unlockedAbilityCache = new HashSet<string>(data.unlockedAbilities);
        _collectedItemCache   = new HashSet<string>(data.collectedItems);
        _openedDoorCache      = new HashSet<string>(data.openedDoors);
    }

    private SaveData CreateNewSaveData(int slotIndex) => new SaveData { slotIndex = slotIndex };

    // ─────────────────────────────────────────────────────
    // 메타 파일 관리
    // ─────────────────────────────────────────────────────

    private SaveMetaFile LoadMeta()
    {
        string json = TryReadFile(MetaPath);
        if (json != null)
        {
            try { return JsonUtility.FromJson<SaveMetaFile>(json); }
            catch { /* 손상 시 슬롯 파일에서 재구성 */ }
        }
        return RebuildMetaFromSlots();
    }

    /// <summary>mado_meta.json 손상 시, 각 슬롯 파일에서 메타를 재구성합니다.</summary>
    private SaveMetaFile RebuildMetaFromSlots()
    {
        var meta = new SaveMetaFile();
        meta.slots = new SaveSlotMeta[3];

        for (int i = 0; i < 3; i++)
        {
            meta.slots[i] = new SaveSlotMeta { slotIndex = i };
            string json = TryReadFile(SlotPath(i)) ?? TryReadFile(SlotBakPath(i));
            if (json == null) continue;

            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                meta.slots[i].isEmpty       = false;
                meta.slots[i].sceneName     = data.lastSceneName;
                meta.slots[i].totalPlayTime = data.totalPlayTime;
                meta.slots[i].lastSavedAt   = data.lastSavedAt;
            }
            catch { /* 슬롯 파일도 손상 — 빈 슬롯으로 처리 */ }
        }

        return meta;
    }

    private void UpdateMeta(int slotIndex, SaveData data)
    {
        SaveMetaFile meta = LoadMeta();
        if (meta.slots == null) meta.slots = new SaveSlotMeta[3];
        if (meta.slots[slotIndex] == null) meta.slots[slotIndex] = new SaveSlotMeta { slotIndex = slotIndex };

        if (data == null)
        {
            meta.slots[slotIndex] = new SaveSlotMeta { slotIndex = slotIndex, isEmpty = true };
        }
        else
        {
            meta.slots[slotIndex].isEmpty       = false;
            meta.slots[slotIndex].sceneName     = data.lastSceneName;
            meta.slots[slotIndex].totalPlayTime = data.totalPlayTime;
            meta.slots[slotIndex].lastSavedAt   = data.lastSavedAt;
        }

        try { File.WriteAllText(MetaPath, JsonUtility.ToJson(meta, prettyPrint: true)); }
        catch (Exception e) { Debug.LogWarning($"[SaveManager] 메타 파일 저장 실패: {e.Message}"); }
    }

    // ─────────────────────────────────────────────────────
    // 파일 I/O 헬퍼
    // ─────────────────────────────────────────────────────

    private string TryReadFile(string path)
    {
        if (!File.Exists(path)) return null;
        try { return File.ReadAllText(path); }
        catch (Exception e) { Debug.LogWarning($"[SaveManager] 파일 읽기 실패 ({path}): {e.Message}"); return null; }
    }

    private void TryDeleteFile(string path)
    {
        if (!File.Exists(path)) return;
        try { File.Delete(path); }
        catch (Exception e) { Debug.LogWarning($"[SaveManager] 파일 삭제 실패 ({path}): {e.Message}"); }
    }
}
