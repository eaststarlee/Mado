using System;
using System.Collections.Generic;

[Serializable]
public class GameData
{
    // 메타데이터
    public SaveMetaData meta = new SaveMetaData();

    // 위치 및 씬 정보 (Death Loop 방지를 위해 SavePoint 기준 좌표 사용)
    public string lastSceneName = "TestRoom";
    public string lastSpawnId = "Default";
    public float[] lastPosition = new float[3] { 0f, 0f, 0f }; // SavePoint의 위치

    // 플레이어 기본 스탯
    public int currentHP = 5;
    public int maxHP = 5;
    public int currentSP = 100;
    public int maxSP = 100;

    // 재화 및 기타 고정 데이터
    public int geo = 0;
    public string currentWorld = "Devil";

    // 월드 진행 상태 (EntityState 통합형)
    public Dictionary<string, EntityState> worldEntities = new Dictionary<string, EntityState>();

    // 해금된 능력이나 범용 상태들
    public List<string> unlockedAbilities = new List<string>();
}
