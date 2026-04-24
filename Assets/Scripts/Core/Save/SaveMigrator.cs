/// <summary>
/// SaveData의 버전 간 마이그레이션을 전담하는 정적 유틸리티 클래스.
/// SaveManager.LoadSlot() 내부에서 JSON 파싱 직후 즉시 실행됩니다.
///
/// ■ 규칙:
///   - 새 필드 추가 시, 해당 버전의 마이그레이션 메서드에서 기본값을 주입합니다.
///   - 마이그레이션은 항상 순차 체인으로 처리합니다 (1.0→1.1→1.2 순서).
///   - 마이그레이션 완료 후 saveVersion을 CurrentVersion으로 업데이트합니다.
/// </summary>
public static class SaveMigrator
{
    private const string CurrentVersion = "1.0";

    /// <summary>
    /// 로드된 SaveData를 현재 버전 구조로 변환합니다.
    /// 버전이 동일하면 즉시 반환합니다.
    /// </summary>
    public static SaveData Migrate(SaveData data)
    {
        if (data.saveVersion == CurrentVersion) return data;

        // ── 순차 마이그레이션 체인 ──────────────────────────
        if (string.IsNullOrEmpty(data.saveVersion))
            data = MigrateFrom_None_To_1_0(data);

        // 향후 버전 추가 예시:
        // if (data.saveVersion == "1.0") data = MigrateFrom_1_0_To_1_1(data);

        data.saveVersion = CurrentVersion;
        return data;
    }

    // ── 마이그레이션 메서드 ────────────────────────────────

    /// <summary>버전 정보가 없는 구버전 파일 → 1.0 호환 처리</summary>
    private static SaveData MigrateFrom_None_To_1_0(SaveData data)
    {
        // 구버전에 없던 필드에 안전한 기본값 주입
        if (data.maxHP  <= 0) data.maxHP  = 5;
        if (data.maxSP  <= 0) data.maxSP  = 100;
        if (string.IsNullOrEmpty(data.currentWorld))  data.currentWorld  = "Devil";
        if (string.IsNullOrEmpty(data.lastSceneName)) data.lastSceneName = "TestRoom";
        if (string.IsNullOrEmpty(data.lastSpawnId))   data.lastSpawnId   = "Default";

        // 리스트 null 방호
        data.defeatedBosses    ??= new();
        data.unlockedAbilities ??= new();
        data.collectedItems    ??= new();
        data.openedDoors       ??= new();

        return data;
    }

    // private static SaveData MigrateFrom_1_0_To_1_1(SaveData data)
    // {
    //     // 예: 1.1에서 추가된 필드 초기화
    //     return data;
    // }
}
