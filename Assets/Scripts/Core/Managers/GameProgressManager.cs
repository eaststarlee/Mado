using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 런타임 메모리 중앙 관리자
/// 게임 플레이 중 모든 상태(World State)를 이곳에 캐싱하며, 
/// 세이브 요청 시 CurrentData를 SaveSystem을 통해 파일에 기록합니다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    public GameData CurrentData { get; private set; }
    public int ActiveSlotIndex { get; private set; } = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 씬 로딩 초기에는 빈 데이터로 시작.
        // 메인 메뉴나 BootSequencer에서 LoadSlot()을 통해 실제 데이터를 채움.
        CurrentData = new GameData();
    }

    /// <summary>
    /// 게임 로드 또는 새 게임 시작 시 호출됩니다.
    /// </summary>
    public void LoadSlot(int slotIndex)
    {
        ActiveSlotIndex = slotIndex;
        var loadedData = SaveSystem.Load(slotIndex);
        
        if (loadedData != null)
        {
            CurrentData = loadedData;
        }
        else
        {
            // 새 게임인 경우
            CurrentData = new GameData();
            CurrentData.meta.slotIndex = slotIndex;
        }
        
        Debug.Log($"[GameProgressManager] 슬롯 {slotIndex} 데이터 로드 완료.");
    }

    /// <summary>
    /// 현재 상태의 스냅샷을 디스크에 저장합니다.
    /// </summary>
    public void SaveCurrentProgress()
    {
        if (ActiveSlotIndex < 0)
        {
            Debug.LogWarning("[GameProgressManager] 슬롯이 지정되지 않아 저장할 수 없습니다.");
            return;
        }
        
        CurrentData.meta.lastSavedAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        if (PlaytimeTracker.Instance != null)
        {
            CurrentData.meta.totalPlayTime = PlaytimeTracker.Instance.ElapsedSeconds;
        }
        
        _ = SaveSystem.SaveAsync(CurrentData, ActiveSlotIndex);
    }

    // ── EntityState 관리 ───────────────────────────────────────────

    public void SetEntityState(EntityIdSO entityId, EntityState state)
    {
        if (entityId == null) return;
        CurrentData.worldEntities[entityId.Guid] = state;
    }

    public EntityState GetEntityState(EntityIdSO entityId)
    {
        if (entityId == null) return new EntityState { active = false, state = 0 };
        
        if (CurrentData.worldEntities.TryGetValue(entityId.Guid, out var state))
        {
            return state;
        }
        
        return new EntityState { active = false, state = 0 };
    }

    // 간단한 Flag (bool) 지원용 확장
    public void SetFlag(EntityIdSO entityId, bool isActive)
    {
        if (entityId == null) return;
        
        var state = GetEntityState(entityId);
        state.active = isActive;
        SetEntityState(entityId, state);
    }

    public bool GetFlag(EntityIdSO entityId)
    {
        return GetEntityState(entityId).active;
    }
}
