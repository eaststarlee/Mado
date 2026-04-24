using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cinemachine 카메라 관리 싱글톤 — 4단계 업데이트.
///
/// ■ 역할
///   1. 씬에 있는 모든 CinemachineCamera 목록 관리 (Priority 기반 전환)
///   2. 씬 전환 후 SceneLoader가 호출:
///      - SetFollowTarget()       : 모든 VCam의 Follow 타겟 재연결
///      - ActivateRoomCamera()    : 활성 씬의 RoomCamera 탐색 + Priority 활성화
///                                   + CinemachineConfiner2D.BoundingShape2D 재연결
///                                   + InvalidateBoundingShapeCache() 호출
///
/// ■ Cinemachine 3.1.4 Confiner2D 주의사항
///   - BoundingShape2D = public Collider2D  (런타임 재할당 가능)
///   - 씬 전환 후 RoomBoundary 오브젝트가 새 씬에 있으므로, 재연결하지 않으면
///     Confiner가 null 참조로 동작을 멈춤
///   - BoundingShape2D 재할당 후 반드시 InvalidateBoundingShapeCache() 호출
/// </summary>
public class CameraManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────
    public static CameraManager Instance { get; private set; }

    // ── 카메라 목록 ────────────────────────────────────────
    private List<CinemachineCamera> cameras = new List<CinemachineCamera>();

    // ── 우선순위 상수 ──────────────────────────────────────
    private const int defaultPriority = 0;
    private const int activePriority  = 10;

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        RefreshCameraList();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // SceneLoader가 있으면 SceneLoader가 직접 ActivateRoomCamera()를 호출하므로 중복 방지
        // SceneLoader가 없는 환경(단독 씬 테스트)에서는 여기서 갱신
        if (SceneLoader.Instance == null)
        {
            RefreshCameraList();
        }
    }

    // ══════════════════════════════════════════════════════
    // 공개 API — 카메라 목록 관리
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 현재 로드된 모든 CinemachineCamera를 탐색하여 목록을 갱신합니다.
    /// DontDestroyOnLoad 포함 전체 씬을 탐색합니다.
    /// </summary>
    public void RefreshCameraList()
    {
        cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None).ToList();
        Debug.Log($"[CameraManager] 카메라 목록 갱신: {cameras.Count}개 (활성 씬: '{SceneManager.GetActiveScene().name}')");
    }

    /// <summary>
    /// 지정된 카메라를 활성 카메라로 전환합니다 (Priority 방식).
    /// </summary>
    public void SwitchCamera(CinemachineCamera targetCam)
    {
        if (targetCam == null)
        {
            Debug.LogWarning("[CameraManager] SwitchCamera: targetCam이 null입니다.");
            return;
        }

        // 목록에 없으면 한 번 갱신 후 재확인
        if (!cameras.Contains(targetCam))
        {
            RefreshCameraList();
            if (!cameras.Contains(targetCam))
            {
                Debug.LogWarning($"[CameraManager] '{targetCam.name}'을 목록에서 찾을 수 없습니다. 강제 추가.");
                cameras.Add(targetCam);
            }
        }

        // 모든 카메라 Priority 초기화 후 대상만 활성화
        foreach (var cam in cameras)
        {
            if (cam != null)
                cam.Priority = defaultPriority;
        }
        targetCam.Priority = activePriority;
    }

    /// <summary>관리 목록에 새 카메라를 추가합니다.</summary>
    public void AddCamera(CinemachineCamera newCam)
    {
        if (newCam != null && !cameras.Contains(newCam))
            cameras.Add(newCam);
    }

    /// <summary>관리 목록에서 카메라를 제거합니다.</summary>
    public void RemoveCamera(CinemachineCamera camToRemove)
    {
        if (camToRemove != null)
            cameras.Remove(camToRemove);
    }

    // ══════════════════════════════════════════════════════
    // 공개 API — SceneLoader 연동
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 씬 전환 후 SceneLoader가 호출.
    /// 모든 관리 카메라의 Follow 타겟을 재연결합니다.
    /// </summary>
    public void SetFollowTarget(Transform playerTransform)
    {
        RefreshCameraList();
        foreach (var cam in cameras)
        {
            if (cam != null)
                cam.Follow = playerTransform;
        }
        Debug.Log($"[CameraManager] Follow 타겟 재연결: {playerTransform?.name}");
    }

    /// <summary>
    /// 씬 전환 후 SceneLoader가 호출.
    /// 현재 활성 씬에 있는 CinemachineCamera(RoomCamera)를 탐색하여:
    ///   1. SwitchCamera()로 활성화 (Priority 10)
    ///   2. CinemachineConfiner2D.BoundingShape2D 재연결
    ///   3. InvalidateBoundingShapeCache() 호출 (캐시 갱신 강제)
    /// </summary>
    public void ActivateRoomCamera()
    {
        RefreshCameraList();

        Scene activeScene = SceneManager.GetActiveScene();
        CinemachineCamera roomCam = FindRoomCameraInScene(activeScene);

        // 1. 카메라 탐색
        if (roomCam == null)
        {
            // 활성 씬에 없으면 전체 씬에서 룸 카메라 탐색 시도
            roomCam = FindRoomCameraInAnyScene();

            if (roomCam == null && cameras.Count > 0)
            {
                // 최후의 수단: 글로벌 카메라 재사용
                roomCam = cameras[0];
                Debug.Log($"[CameraManager] 어느 씬에서도 룸 전용 카메라를 찾지 못해 글로벌 카메라({roomCam.name})를 재사용합니다.");
            }
            else if (roomCam != null)
            {
                Debug.Log($"[CameraManager] 활성 씬({activeScene.name})이 아닌 '{roomCam.gameObject.scene.name}'에서 카메라를 찾았습니다.");
            }
        }

        if (roomCam != null)
        {
            SwitchCamera(roomCam);
            
            // 2. Confiner2D 재연결
            // 카메라가 속한 씬을 우선적으로 사용하여 경계 탐색
            RebindConfiner2D(roomCam, roomCam.gameObject.scene);
        }
        else
        {
            Debug.LogWarning($"[CameraManager] 전체 프로젝트에서 CinemachineCamera를 찾지 못했습니다.");
        }
    }

    // ══════════════════════════════════════════════════════
    // 내부 — Confiner2D 재연결
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 지정 VCam의 CinemachineConfiner2D를 찾아 BoundingShape2D를 재연결합니다.
    ///
    /// [재연결 전략]
    ///   - RoomCamera 오브젝트 또는 그 하위에서 CinemachineConfiner2D 탐색
    ///   - 같은 씬 내에서 "RoomBoundary" 이름의 Collider2D 탐색
    ///   - 찾으면 BoundingShape2D 재할당 + InvalidateBoundingShapeCache() 호출
    ///
    /// [Cinemachine 3.1.4 주의사항]
    ///   BoundingShape2D 재할당만으로는 내부 캐시가 갱신되지 않으므로
    ///   반드시 InvalidateBoundingShapeCache()를 호출해야 합니다.
    /// </summary>
    private void RebindConfiner2D(CinemachineCamera roomCam, Scene targetScene)
    {
        // Confiner2D는 CinemachineCamera와 같은 GameObject에 Extension으로 부착됨
        var confiner = roomCam.GetComponent<CinemachineConfiner2D>();
        if (confiner == null)
        {
            // 자식 오브젝트에 있을 수도 있음
            confiner = roomCam.GetComponentInChildren<CinemachineConfiner2D>(includeInactive: false);
        }

        if (confiner == null)
        {
            // Confiner가 없는 씬(예: 보스 씬에서 의도적으로 제거)은 정상 케이스
            Debug.Log($"[CameraManager] '{roomCam.name}'에 CinemachineConfiner2D가 없습니다. (의도된 씬인지 확인)");
            return;
        }

        // 우선 제공된 씬에서 탐색
        Collider2D boundary = FindRoomBoundaryInScene(targetScene);

        // 못 찾으면 활성 씬에서도 탐색
        if (boundary == null && targetScene != SceneManager.GetActiveScene())
        {
            boundary = FindRoomBoundaryInScene(SceneManager.GetActiveScene());
        }

        // 그래도 못 찾으면 로드된 모든 씬에서 탐색 (최후의 수단)
        if (boundary == null)
        {
            boundary = FindRoomBoundaryInAnyScene();
        }

        if (boundary == null)
        {
            Debug.LogWarning($"[CameraManager] 어느 씬에서도 RoomBoundary Collider2D를 찾지 못했습니다. " +
                             "씬의 경계 오브젝트 이름이 'RoomBoundary'인지 확인하세요.");
            return;
        }

        // Confiner 갱신 오류를 방지하기 위해 껐다 켜기 (유니티 6 / 시네머신 3.x 버그 회피)
        confiner.enabled = false;
        
        // BoundingShape2D 재연결
        confiner.BoundingShape2D = boundary;

        // 캐시 강제 갱신 (씬 교체 후 필수)
        confiner.InvalidateBoundingShapeCache();
        
        confiner.enabled = true;

        // 카메라 순간이동(워프) 시 카메라 위치를 타겟으로 즉시 갱신하도록 처리
        roomCam.PreviousStateIsValid = false;

        Debug.Log($"[CameraManager] Confiner2D 재연결 완료: {roomCam.name} ← {boundary.name} (씬: {targetScene.name})");
    }

    // ══════════════════════════════════════════════════════
    // 내부 — 씬 탐색 유틸리티
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 지정 씬의 루트 오브젝트에서 CinemachineCamera를 탐색합니다.
    /// </summary>
    private CinemachineCamera FindRoomCameraInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            var cam = root.GetComponentInChildren<CinemachineCamera>(includeInactive: false);
            if (cam != null) return cam;
        }
        return null;
    }

    /// <summary>로드된 모든 씬을 순회하며 CinemachineCamera를 탐색합니다.</summary>
    private CinemachineCamera FindRoomCameraInAnyScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded || s.name == "Master") continue;

            var cam = FindRoomCameraInScene(s);
            if (cam != null) return cam;
        }
        return null;
    }

    /// <summary>
    /// 지정 씬에서 "RoomBoundary" 이름의 Collider2D를 탐색합니다.
    /// 이름이 다를 경우: RoomBoundaryTag 또는 컴포넌트 마커로 확장 가능.
    /// </summary>
    private Collider2D FindRoomBoundaryInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // 이름 기반 탐색 (룸 씬 네이밍 규칙: "RoomBoundary")
            var boundaryObj = FindChildByName(root.transform, "RoomBoundary");
            if (boundaryObj != null)
            {
                var col = boundaryObj.GetComponent<Collider2D>();
                if (col != null) return col;
            }

            // 이름 탐색 실패 시 RoomBoundaryMarker 컴포넌트 탐색 (폴백)
            var marker = root.GetComponentInChildren<RoomBoundaryMarker>(includeInactive: false);
            if (marker != null)
            {
                var col = marker.GetComponent<Collider2D>();
                if (col != null) return col;
            }
        }
        return null;
    }

    /// <summary>로드된 모든 씬을 순회하며 RoomBoundary를 탐색합니다.</summary>
    private Collider2D FindRoomBoundaryInAnyScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded || s.name == "Master") continue;

            var col = FindRoomBoundaryInScene(s);
            if (col != null) return col;
        }
        return null;
    }

    /// <summary>자식 계층에서 특정 이름의 Transform을 DFS 탐색합니다.</summary>
    private Transform FindChildByName(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var result = FindChildByName(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
