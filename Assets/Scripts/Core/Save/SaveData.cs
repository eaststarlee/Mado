using System.Collections.Generic;

/// <summary>
/// 게임의 모든 진행 상태를 담는 직렬화 가능한 데이터 모델.
/// SaveManager가 단일 소유자이며, ISaveable 컴포넌트들이 OnSave/OnLoad를 통해 읽고 씁니다.
///
/// ■ Checkpoint 데이터  : 사망 시 마지막 SavePoint 기준으로 롤백
/// ■ Persistent 데이터  : 사망해도 유지 (재화, 해금 능력, 보스 처치 등)
/// </summary>
[System.Serializable]
public class SaveData
{
    // ── 메타 ──────────────────────────────────────────────────
    /// <summary>슬롯 인덱스 (0~2)</summary>
    public int    slotIndex;
    /// <summary>세이브 파일 버전. SaveMigrator가 읽어 마이그레이션 수행.</summary>
    public string saveVersion = "1.0";
    /// <summary>마지막 저장 시각 (UTC Unix 타임스탬프)</summary>
    public long   lastSavedAt;
    /// <summary>누적 플레이 시간(초). PlaytimeTracker가 갱신, SaveAsync 시점에만 기록.</summary>
    public float  totalPlayTime;

    // ── [Checkpoint] 사망 시 롤백되는 데이터 ──────────────────
    /// <summary>마지막 저장된 씬 이름</summary>
    public string lastSceneName = "TestRoom";
    /// <summary>마지막 저장된 스폰 포인트 ID</summary>
    public string lastSpawnId   = "Default";
    /// <summary>저장 시점의 현재 체력</summary>
    public int    currentHP;
    /// <summary>저장 시점의 현재 SP(스킬 게이지)</summary>
    public int    currentSP;

    // ── [Persistent] 사망해도 유지되는 기본 데이터 ────────────
    /// <summary>최대 체력 (아이템으로 증가)</summary>
    public int    maxHP = 5;
    /// <summary>최대 SP (아이템으로 증가)</summary>
    public int    maxSP = 100;
    /// <summary>재화(Geo) 보유량</summary>
    public int    geo;
    /// <summary>현재 활성 세계 ("Devil" / "Spirit")</summary>
    public string currentWorld  = "Devil";

    // ── [Persistent] 진행 플래그 (직렬화용 List) ─────────────
    // 런타임 검색은 SaveManager 내부의 HashSet 캐시를 사용합니다.
    // JsonUtility는 HashSet 직렬화를 지원하지 않으므로, 저장/로드 시에만 List로 변환합니다.

    /// <summary>처치한 보스 ID 목록</summary>
    public List<string> defeatedBosses    = new();
    /// <summary>해금된 능력 ID 목록 (예: "DoubleJump", "Dash")</summary>
    public List<string> unlockedAbilities = new();
    /// <summary>수집한 아이템 GUID 목록</summary>
    public List<string> collectedItems    = new();
    /// <summary>열린 문/이벤트 ID 목록</summary>
    public List<string> openedDoors       = new();
}
