using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

/// <summary>
/// 비동기 씬 전환 총괄 싱글톤.
///
/// ■ LoadNextRoom    : RoomTransition 트리거 → 다음 룸 씬 언로드/로드 → 플레이어 스폰 이동
/// ■ SwitchDimensionRoom : DimensionManager 요청 → 현재 룸 언로드 → 대응 세계 룸 로드, 플레이어 위치 유지
///
/// [씬 구조 전제]
///   - "Master"  씬 : 영구 씬 (DontDestroyOnLoad 오브젝트 보관), 절대 언로드하지 않음.
///   - Room 씬   : "Devil_" 또는 "Spirit_" 로 시작하는 씬. Additive 로드, 1개씩 교체.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────
    public static SceneLoader Instance { get; private set; }

    // ── 상태 ───────────────────────────────────────────────
    /// <summary>현재 전환 애니메이션 진행 중 여부. 외부에서 입력 차단 등에 활용.</summary>
    public bool IsTransitioning { get; private set; }

    // ── Inspector ──────────────────────────────────────────
    [Header("Fade 설정")]
    [Tooltip("룸 전환 시 FadeOut/In 지속 시간 (초). FadeManager.fadeDuration보다 우선됩니다.")]
    [SerializeField] private float roomFadeDuration = 0.4f;

    [Tooltip("차원 전환 시 FadeOut/In 지속 시간 (초).")]
    [SerializeField] private float dimensionFadeDuration = 0.6f;

    [Header("마스터 씬 이름")]
    [Tooltip("절대 언로드하지 않을 영구 씬 이름. 정확히 일치해야 합니다.")]
    [SerializeField] private string masterSceneName = "Master";

    // ── Unity Lifecycle ────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ══════════════════════════════════════════════════════
    // 공개 API
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 룸 전환 요청 (RoomTransition 컴포넌트에서 호출).
    /// FadeOut → 현재 룸 언로드 → 다음 룸 로드 → 플레이어 스폰 이동 → FadeIn.
    /// </summary>
    /// <param name="nextSceneName">다음 룸 씬 이름 (확장자 없음, 예: "Devil_Area01_002_GardenPath")</param>
    /// <param name="spawnId">도착할 SpawnPoint 식별자 (예: "Left", "Right", "Default")</param>
    /// <summary>
    /// 룸 전환 요청 (RoomTransition 컴포넌트에서 호출).
    /// FadeOut → 현재 룸 언로드 → 다음 룸 로드 → 플레이어 스폰 이동 → FadeIn.
    /// </summary>
    /// <param name="nextSceneName">다음 룸 씬 이름 (확장자 없음, 예: "Devil_Area01_002_GardenPath")</param>
    /// <param name="spawnId">도착할 SpawnPoint 식별자 (예: "Left", "Right", "Default")</param>
    /// <param name="transitionDirX">가로로 이동 중인 방향 (1: Right, -1: Left, 0: 기타)</param>
    public void LoadNextRoom(string nextSceneName, string spawnId, int transitionDirX = 0)
    {
        if (IsTransitioning)
        {
            Debug.LogWarning("[SceneLoader] 이미 전환 중입니다. 요청 무시.");
            return;
        }
        StartCoroutine(LoadNextRoomRoutine(nextSceneName, spawnId, transitionDirX));
    }

    /// <summary>
    /// 차원 전환 요청 (DimensionManager에서 호출).
    /// FadeOut → 현재 룸 언로드 → 대응 세계 룸 로드 → 플레이어 위치 유지 → FadeIn.
    /// </summary>
    /// <param name="targetSceneName">대응 세계 룸 씬 이름</param>
    /// <param name="targetWorld">전환 후 세계 종류</param>
    public void SwitchDimensionRoom(string targetSceneName, WorldType targetWorld)
    {
        if (IsTransitioning)
        {
            Debug.LogWarning("[SceneLoader] 이미 전환 중입니다. 요청 무시.");
            return;
        }
        StartCoroutine(SwitchDimensionRoutine(targetSceneName, targetWorld));
    }

    // ══════════════════════════════════════════════════════
    // 에디터 지원 API
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 에디터 샌드박스 지원용. 이미 로드된 씬을 기반으로 매니저들을 바인딩시킵니다.
    /// 마스터 씬의 플레이어를 해당 방의 스폰 포인트로 이동키고, 카메라를 즉시 붙여줍니다.
    /// </summary>
    public void BindToAlreadyLoadedRoom(string roomSceneName)
    {
        Scene scene = SceneManager.GetSceneByName(roomSceneName);
        if (!scene.isLoaded)
        {
            Debug.LogWarning($"[SceneLoader] 바인딩 시도 중 '{roomSceneName}' 씬이 아직 로드되지 않았습니다.");
            return;
        }

        Debug.Log($"[SceneLoader] 에디터 샌드박스 방 바인딩: {roomSceneName}");
        
        SetActiveScene(roomSceneName);
        TeleportPlayerToSpawn("Default");
        RefreshCamera();
        RaiseRoomEnteredEvent();
    }

    // ══════════════════════════════════════════════════════
    // 내부 코루틴
    // ══════════════════════════════════════════════════════

    /// <summary>룸 전환 메인 코루틴.</summary>
    private IEnumerator LoadNextRoomRoutine(string nextSceneName, string spawnId, int transitionDirX)
    {
        IsTransitioning = true;
        
        PlayerController player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null && !player.gameObject.activeSelf) player.gameObject.SetActive(true);

        // 가로 이동일 경우, Freeze 대신 자동 걷기를 활성화
        if (transitionDirX != 0 && player != null)
        {
            player.SetAutoWalk(transitionDirX);
        }
        else
        {
            FreezePlayer(true); // 수직 이동이거나 방향이 없으면 안전을 위해 얼림
        }

        Debug.Log($"[SceneLoader] 룸 전환 시작: → {nextSceneName} (spawn: {spawnId}, autoWalk: {transitionDirX})");

        // ── 1. 페이드 아웃 (걷는 도중에 화면이 어두워짐) ───────────
        yield return StartCoroutine(FadeOut(roomFadeDuration));

        // ── 2. 페이드가 끝나 완전히 어두워진 시점 ───────────
        // 다음 씬 로딩/위치 교체 중 추락방지를 위해 잠시 물리엔진 강제 정지
        if (transitionDirX != 0) 
            FreezePlayer(true); 

        // ── 3. 현재/다음 룸 씬 교체 ───────────────────────
        string currentRoomScene = GetCurrentRoomSceneName();
        yield return StartCoroutine(LoadSceneAdditive(nextSceneName));
        
        // [방어 코드] 씬 로딩이 실패한 경우(Build Settings 누락 등) 전환 취소
        if (!SceneManager.GetSceneByName(nextSceneName).isLoaded)
        {
            Debug.LogError($"[SceneLoader] '{nextSceneName}' 로딩 실패! 전환을 취소하고 기존 씬을 유지합니다.");
            FreezePlayer(false);
            yield return StartCoroutine(FadeIn(roomFadeDuration));
            if (player != null) player.SetAutoWalk(0);
            IsTransitioning = false;
            yield break;
        }

        if (!string.IsNullOrEmpty(currentRoomScene))
        {
            yield return StartCoroutine(UnloadScene(currentRoomScene));
        }

        SetActiveScene(nextSceneName);

        // ── 4. 플레이어 스폰 위치로 텔레포트 ─────────────────
        TeleportPlayerToSpawn(spawnId);

        // 물리 엔진에 위치 변경을 즉시 알림
        Physics2D.SyncTransforms();

        RefreshCamera();
        RaiseRoomEnteredEvent();

        // ── 5. 물리 프레임 대기 (카메라 위치 동기화 및 물리 엔진 안정화) ──────────
        yield return new WaitForFixedUpdate();
        yield return new WaitForEndOfFrame(); // 한 프레임 더 대기하여 콜라이더 상태 확정
        yield return null;

        // ── 6. 페이드 인 준비 ──────────────────────────────
        FreezePlayer(false); // 수직이든 수평이든 이제 자유롭게 떨어지거나 걸어야 함
        
        // 가로 이동이었다면 텔레포트 한 직후 다시 안쪽으로 걷기 시작
        if (transitionDirX != 0 && player != null)
        {
            player.SetAutoWalk(transitionDirX);
        }

        // ── 7. 페이드 인 (걸어나오면서 밝아짐) ─────────────
        yield return StartCoroutine(FadeIn(roomFadeDuration));

        // ── 8. 전환 완전 종료 (오토 워킹 해제 및 조작 권한 반환) ─
        if (player != null)
        {
            player.SetAutoWalk(0);
        }
        
        IsTransitioning = false;
        Debug.Log($"[SceneLoader] 룸 전환 완료: {nextSceneName}");
    }

    /// <summary>차원 전환 메인 코루틴.</summary>
    private IEnumerator SwitchDimensionRoutine(string targetSceneName, WorldType targetWorld)
    {
        IsTransitioning = true;
        FreezePlayer(true); // 로딩 중 추락 방지
        Debug.Log($"[SceneLoader] 차원 전환 시작: → {targetSceneName} ({targetWorld})");

        // ── 1. 플레이어 현재 위치 캐싱 ─────────────────
        Vector3 cachedPlayerPos = GetPlayerPosition();

        // ── 2. FadeOut ─────────────────────────────────
        yield return StartCoroutine(FadeOut(dimensionFadeDuration));

        // ── 3. 현재 룸 씬 이름 확인 ────────────────────
        string currentRoomScene = GetCurrentRoomSceneName();

        // ── 4. 대응 룸 씬 로드 (Additive) ──────────────
        yield return StartCoroutine(LoadSceneAdditive(targetSceneName));

        // ── 5. 현재 룸 씬 언로드 ───────────────────────
        if (!string.IsNullOrEmpty(currentRoomScene))
        {
            yield return StartCoroutine(UnloadScene(currentRoomScene));
        }

        // ── 6. 활성 씬 설정 ────────────────────────────
        SetActiveScene(targetSceneName);

        // ── 7. 세계 상태 갱신 (DimensionManager에 통보) ─
        if (DimensionManager.Instance != null)
        {
            DimensionManager.Instance.SetCurrentWorld(targetWorld);
        }

        // ── 8. 플레이어 위치 유지 (차원 전환 = 같은 좌표) ─
        SetPlayerPosition(cachedPlayerPos);
        FreezePlayer(false); // 중력 및 물리 복구

        // ── 9. 카메라 재연결 ────────────────────────────
        RefreshCamera();

        // ── 10. 차원 전환 이벤트 발생 ──────────────────
        GameEvents.RaiseDimensionSwitched(targetWorld);

        // ── 11. 물리 프레임 대기 ───────────────────────
        yield return new WaitForFixedUpdate();
        yield return null;

        // ── 12. FadeIn ─────────────────────────────────
        yield return StartCoroutine(FadeIn(dimensionFadeDuration));

        IsTransitioning = false;
        Debug.Log($"[SceneLoader] 차원 전환 완료: {targetSceneName} ({targetWorld})");
    }

    // ══════════════════════════════════════════════════════
    // 씬 조작 유틸리티
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 현재 활성 룸 씬 이름 반환.
    /// Master 씬 / DontDestroyOnLoad 씬을 제외한 씬 이름을 반환.
    /// 룸 씬이 없으면 빈 문자열 반환.
    /// </summary>
    private string GetCurrentRoomSceneName()
    {
        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.name != masterSceneName)
            {
                return scene.name;
            }
        }
        Debug.LogWarning("[SceneLoader] 현재 활성 룸 씬을 찾을 수 없습니다.");
        return string.Empty;
    }

    /// <summary>씬을 Additive 방식으로 비동기 로드.</summary>
    private IEnumerator LoadSceneAdditive(string sceneName)
    {
        // 이미 로드되어 있는지 확인
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Debug.LogWarning($"[SceneLoader] '{sceneName}' 씬이 이미 로드되어 있습니다.");
            yield break;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError($"[SceneLoader] '{sceneName}' 씬을 LoadSceneAsync로 불러오지 못했습니다. Build Settings에 씬 이름이 정확히 등록되어 있는지, 대소문자가 맞는지 확인하세요.");
            yield break;
        }

        op.allowSceneActivation = true;
        while (!op.isDone)
        {
            yield return null;
        }
        Debug.Log($"[SceneLoader] '{sceneName}' 로드 완료.");
    }

    /// <summary>씬을 비동기 언로드.</summary>
    private IEnumerator UnloadScene(string sceneName)
    {
        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Debug.LogWarning($"[SceneLoader] '{sceneName}' 씬이 로드되어 있지 않습니다. 언로드 건너뜀.");
            yield break;
        }

        AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);
        if (op == null) yield break;

        while (!op.isDone)
        {
            yield return null;
        }
        Debug.Log($"[SceneLoader] '{sceneName}' 언로드 완료.");
    }

    /// <summary>지정 씬을 활성 씬으로 설정 (새 오브젝트 생성 위치 기준).</summary>
    private void SetActiveScene(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            SceneManager.SetActiveScene(scene);
            Debug.Log($"[SceneLoader] 활성 씬 설정: {sceneName}");
        }
        else
        {
            Debug.LogWarning($"[SceneLoader] 활성 씬 설정 실패: '{sceneName}' 씬이 유효하지 않습니다.");
        }
    }

    // ══════════════════════════════════════════════════════
    // 플레이어 유틸리티
    // ══════════════════════════════════════════════════════

    /// <summary>로딩 중 플레이어 물리 연산 강제 정지 (추락 방지 방어벽)</summary>
    private void FreezePlayer(bool freeze)
    {
        var player = GameManager.Instance?.Player;
        if (player != null && player.RB != null)
        {
            if (freeze) { player.RB.linearVelocity = Vector2.zero; player.RB.bodyType = RigidbodyType2D.Static; }
            else        { player.RB.bodyType = RigidbodyType2D.Dynamic; }
        }

        // 펫도 함께 물리 프리즈 적용 (전환 중 추락/오동작 방지)
        var pet = FindAnyObjectByType<PetController>(FindObjectsInactive.Include);
        if (pet != null && !pet.gameObject.activeSelf) pet.gameObject.SetActive(true);
        if (pet != null && pet.GetComponent<Rigidbody2D>() != null)
        {
            var petRb = pet.GetComponent<Rigidbody2D>();
            if (freeze)
            {
                // [수정] 속도를 먼저 0으로 만든 후 Static으로 전환
                petRb.linearVelocity = Vector2.zero;
                petRb.bodyType = RigidbodyType2D.Static;
            }
            else
            {
                petRb.bodyType = RigidbodyType2D.Dynamic;
            }
        }
    }

    /// <summary>플레이어 현재 위치 반환. 플레이어가 없으면 Vector3.zero.</summary>
    private Vector3 GetPlayerPosition()
    {
        var player = GameManager.Instance?.Player;
        if (player != null) return player.transform.position;

        Debug.LogWarning("[SceneLoader] PlayerController를 찾을 수 없습니다. 위치 캐싱 실패.");
        return Vector3.zero;
    }

    /// <summary>플레이어를 지정 위치로 순간이동.</summary>
    private void SetPlayerPosition(Vector3 position)
    {
        var player = GameManager.Instance?.Player;
        if (player != null)
        {
            player.transform.position = position;

            // 관성 제거
            if (player.RB != null && player.RB.bodyType != RigidbodyType2D.Static)
            {
                player.RB.linearVelocity = Vector2.zero;
                player.RB.angularVelocity = 0f;
            }

            // 펫도 플레이어와 동일한 위치로 이동 (GhostState 방지)
            var pet = FindAnyObjectByType<PetController>(FindObjectsInactive.Include);
            if (pet != null && !pet.gameObject.activeSelf) pet.gameObject.SetActive(true);
            if (pet != null)
            {
                pet.transform.position = position;
                var petRb = pet.GetComponent<Rigidbody2D>();
                if (petRb != null && petRb.bodyType != RigidbodyType2D.Static)
                {
                    petRb.linearVelocity = Vector2.zero;
                    petRb.angularVelocity = 0f;
                }
            }
        }
        else
        {
            Debug.LogWarning("[SceneLoader] PlayerController를 찾을 수 없습니다. 위치 설정 실패.");
        }
    }

    /// <summary>
    /// 플레이어를 spawnId에 해당하는 SpawnPoint로 이동.
    /// 씬 내의 SpawnPoint[] 중 spawnId가 일치하는 것을 탐색.
    /// 없으면 "Default" SpawnPoint를, 그것도 없으면 경고만 출력.
    /// </summary>
    private void TeleportPlayerToSpawn(string spawnId)
    {
        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning($"[SceneLoader] 현재 씬에서 SpawnPoint 컴포넌트를 가진 오브젝트를 하나도 찾지 못했습니다. (요청 Id: {spawnId})");
            return;
        }

        SpawnPoint target = null;
        SpawnPoint fallback = null;

        foreach (var sp in spawnPoints)
        {
            if (sp.SpawnId == spawnId)    { target   = sp; break; }
            if (sp.SpawnId == "Default")  { fallback = sp; }
        }

        SpawnPoint chosen = target ?? fallback;
        if (chosen == null)
        {
            Debug.LogWarning($"[SceneLoader] 요청한 SpawnId '{spawnId}' 또는 'Default' 포인트를 찾지 못했습니다. 위치 이동을 건너뜁니다.");
            return;
        }

        SetPlayerPosition(chosen.transform.position);
        Debug.Log($"[SceneLoader] 플레이어 스폰 성공: '{chosen.SpawnId}' 포인트로 이동 완료 (좌표: {chosen.transform.position})");
    }

    // ══════════════════════════════════════════════════════
    // 카메라 유틸리티
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// CameraManager에 카메라 목록 갱신 및 활성 씬의 RoomCamera 활성화 요청.
    /// CameraManager가 씬에 없으면 경고만 출력.
    /// </summary>
    private void RefreshCamera()
    {
        if (CameraManager.Instance == null)
        {
            Debug.LogWarning("[SceneLoader] CameraManager.Instance가 없습니다. 카메라 갱신 건너뜀.");
            return;
        }

        // Follow 타겟 재연결
        var player = GameManager.Instance?.Player;
        if (player != null) CameraManager.Instance.SetFollowTarget(player.transform);

        // 룸 카메라 활성화
        CameraManager.Instance.ActivateRoomCamera();
    }

    // ══════════════════════════════════════════════════════
    // 이벤트 유틸리티
    // ══════════════════════════════════════════════════════

    /// <summary>현재 활성 씬의 RoomData를 읽어 OnRoomEntered 이벤트 발생.</summary>
    private void RaiseRoomEnteredEvent()
    {
        var roomData = FindFirstObjectByType<RoomData>();
        if (roomData != null)
        {
            GameEvents.RaiseRoomEntered(roomData);
        }
    }

    // ══════════════════════════════════════════════════════
    // FadeManager 래퍼
    // ══════════════════════════════════════════════════════

    private IEnumerator FadeOut(float duration)
    {
        if (FadeManager.Instance != null)
            yield return FadeManager.Instance.FadeOut(duration);
        else
            yield return new WaitForSeconds(duration);
    }

    private IEnumerator FadeIn(float duration)
    {
        if (FadeManager.Instance != null)
            yield return FadeManager.Instance.FadeIn(duration);
        else
            yield return new WaitForSeconds(duration);
    }
}
