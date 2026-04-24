using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 차원(세계) 전환 담당 싱글톤 — 2단계 전면 재작성.
///
/// ■ 역할 (단일 책임 원칙)
///   1. 현재 활성 세계(CurrentWorld) 상태 보관
///   2. 대응 씬 이름 계산 ("Devil_" ↔ "Spirit_" 접두어 교체)
///   3. 차원 전환 요청을 SceneLoader에 위임
///   4. CurrentWorldMask / CommonWorldMask 캐싱
///      (PlayerController, LedgeDetector, GrappleDetector, HitResolver 등이 참조)
///
/// ■ 삭제된 레거시 기능 (씬 분리 방식으로 불필요)
///   - devilWorldRoot / spiritWorldRoot SetActive 토글
///   - ApplyVisuals() — Tilemap/SpriteRenderer 알파 제어
///   - ApplyLayerCollision() — Physics2D.IgnoreLayerCollision
///   - RefreshSceneReferences() — 태그 기반 오브젝트 탐색
///   - colorTransitionCoroutine — 배경색 Fade 코루틴
///   - RecalculateMask() — 내부 전용 (SetCurrentWorld에 통합)
/// </summary>
public class DimensionManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────
    public static DimensionManager Instance { get; private set; }

    // ── 상태 ───────────────────────────────────────────────
    /// <summary>현재 활성화된 세계.</summary>
    public WorldType CurrentWorld { get; private set; }

    /// <summary>
    /// 캐싱된 레이어마스크 (현재 세계 레이어 단독).
    /// PlayerController, LedgeDetector, GrappleDetector, HitResolver에서 매 프레임 참조.
    /// SetCurrentWorld 호출 시 자동 갱신.
    ///
    /// [씬 분리 방식 전환 후]
    /// 각 룸 씬이 자체 콜라이더를 보유하므로, 이 마스크는
    /// "현재 세계의 레이어를 가진 오브젝트" 감지에 계속 사용됩니다.
    /// </summary>
    public LayerMask CurrentWorldMask { get; private set; }

    /// <summary>
    /// Common_World 레이어만을 포함하는 마스크.
    /// 씬 분리 방식 전환 후 현재 직접 참조하는 코드는 없으나,
    /// 향후 Common 지형 기반 기능 추가 시 활용 가능하도록 유지합니다.
    /// </summary>
    public LayerMask CommonWorldMask { get; private set; }

    // ── Inspector ──────────────────────────────────────────
    [Header("초기 설정")]
    [Tooltip("게임 시작 시 초기 세계")]
    [SerializeField] private WorldType initialWorld = WorldType.Devil;

    [Header("레이어 인덱스")]
    [Tooltip("Common_World 레이어 인덱스 (기본 14)")]
    [SerializeField] private int commonWorldLayer = 14;

    [Tooltip("Devil_World 레이어 인덱스 (기본 15)")]
    [SerializeField] private int devilWorldLayer = 15;

    [Tooltip("Spirit_World 레이어 인덱스 (기본 16)")]
    [SerializeField] private int spiritWorldLayer = 16;



    // ── Unity Lifecycle ────────────────────────────────────
    private void Awake()
    {
        // 싱글톤 처리
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 초기 세계 설정 + 레이어마스크 초기화
        CurrentWorld = initialWorld;
        RefreshLayerMasks();
    }

    // ══════════════════════════════════════════════════════
    // 공개 API
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 차원 전환 요청.
    /// 대응 씬 이름을 계산하여 SceneLoader.SwitchDimensionRoom()에 위임합니다.
    ///
    /// [호출 위치] PlayerController.HandleWorldSwitch()에서 호출.
    /// </summary>
    public void RequestDimensionSwitch()
    {
        // 1. 상태 체크 (전환 중인지)
        if (SceneLoader.Instance != null && SceneLoader.Instance.IsTransitioning)
        {
            Debug.LogWarning("[DimensionManager] 씬 전환 중입니다. 차원 전환 요청 무시.");
            return;
        }

        // 2. 현재 방의 RoomData 탐색
        RoomData currentRoomData = FindFirstObjectByType<RoomData>();
        if (currentRoomData == null)
        {
            Debug.LogWarning("[DimensionManager] 현재 씬에서 RoomData를 찾을 수 없어 차원 전환이 불가능합니다.");
            return;
        }

        // 3. 대응 씬 이름 확인
        string targetSceneName = currentRoomData.otherWorldSceneName;
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning($"[DimensionManager] 현재 룸({currentRoomData.roomId})에 'Other World Scene Name'이 설정되어 있지 않습니다.");
            return;
        }

        // 4. 목표 세계 종류 결정 (현재의 반대)
        WorldType targetWorld = currentRoomData.world == WorldType.Devil ? WorldType.Spirit : WorldType.Devil;

        Debug.Log($"[DimensionManager] 차원 전환 요청: {currentRoomData.roomId} → {targetSceneName} ({targetWorld})");

        // 5. 실행 위임
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.SwitchDimensionRoom(targetSceneName, targetWorld);
        }
        else
        {
            Debug.LogError("[DimensionManager] SceneLoader.Instance가 없습니다.");
        }
    }

    /// <summary>
    /// SceneLoader가 씬 전환 완료 후 호출하여 현재 세계 상태를 갱신합니다.
    /// </summary>
    public void SetCurrentWorld(WorldType world)
    {
        if (CurrentWorld == world) return;
        CurrentWorld = world;
        RefreshLayerMasks();
        Debug.Log($"[DimensionManager] 현재 세계 갱신: {world}");
    }


    /// <summary>
    /// CurrentWorld 기준으로 레이어마스크를 갱신합니다.
    /// Awake 및 SetCurrentWorld에서만 호출됩니다.
    /// </summary>
    private void RefreshLayerMasks()
    {
        CommonWorldMask = 1 << commonWorldLayer;

        if (CurrentWorld == WorldType.Devil)
            CurrentWorldMask = CommonWorldMask | (1 << devilWorldLayer);
        else
            CurrentWorldMask = CommonWorldMask | (1 << spiritWorldLayer);
    }
}
