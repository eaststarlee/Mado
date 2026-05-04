using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    private List<CinemachineCamera> cameras = new List<CinemachineCamera>();
    private const int defaultPriority = 0;
    private const int activePriority = 10;
    
    public RoomData CurrentRoomData { get; private set; }

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

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Start()
    {
        RefreshCameraList();
        ActivateRoomCamera();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneLoader.Instance == null) RefreshCameraList();
    }

    public void RefreshCameraList()
    {
        cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None).ToList();
    }

    public void SetFollowTarget(Transform playerTransform)
    {
        RefreshCameraList();
        foreach (var cam in cameras)
        {
            if (cam != null) cam.Follow = playerTransform;
        }
    }

    public void SwitchCamera(CinemachineCamera targetCam)
    {
        if (targetCam == null) return;
        foreach (var cam in cameras)
        {
            if (cam != null) cam.Priority = defaultPriority;
        }
        targetCam.Priority = activePriority;
    }

    public void ActivateRoomCamera()
    {
        RefreshCameraList();
        Scene activeScene = SceneManager.GetActiveScene();

        CinemachineCamera roomCam = FindRoomCameraInScene(activeScene);
        if (roomCam == null) roomCam = FindRoomCameraInAnyScene();

        if (roomCam != null)
        {
            CurrentRoomData = FindRoomDataInScene(activeScene);
            if (CurrentRoomData == null) CurrentRoomData = FindRoomDataInAnyScene();

            SwitchCamera(roomCam);

            Collider2D boundary = FindRoomBoundaryInScene(activeScene);
            if (boundary == null) boundary = FindRoomBoundaryInAnyScene();

            if (boundary != null)
            {
                RebindConfiner2D(roomCam, boundary);
            }

            // 방 설정 적용 및 워프
            ApplyRoomSettings(roomCam, CurrentRoomData);
            if (roomCam.Follow != null)
            {
                WarpCamera(roomCam, roomCam.Follow);
            }
        }
    }

    private void ApplyRoomSettings(CinemachineCamera roomCam, RoomData data)
    {
        if (data == null) return;
        var composer = roomCam.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
        if (composer == null) return;

        // lockCameraY가 켜져 있으면 Y축 댐핑을 높여서 고정함
        composer.Damping.y = data.lockCameraY ? 20f : 0.5f;
        Debug.Log($"[CameraManager] Applied settings for {data.roomId} (LockY: {data.lockCameraY})");
    }

    public void WarpCamera(CinemachineCamera roomCam, Transform target)
    {
        if (roomCam == null || target == null) return;
        roomCam.OnTargetObjectWarped(target, target.position - roomCam.transform.position);
        roomCam.ForceCameraPosition(target.position, roomCam.transform.rotation);
    }

    private void RebindConfiner2D(CinemachineCamera roomCam, Collider2D boundary)
    {
        var confiner = roomCam.GetComponent<CinemachineConfiner2D>();
        if (confiner == null) confiner = roomCam.GetComponentInChildren<CinemachineConfiner2D>();

        if (confiner != null)
        {
            confiner.BoundingShape2D = boundary;
            confiner.InvalidateBoundingShapeCache();
        }
    }

    private CinemachineCamera FindRoomCameraInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            var cam = root.GetComponentInChildren<CinemachineCamera>(true);
            if (cam != null) return cam;
        }
        return null;
    }

    private CinemachineCamera FindRoomCameraInAnyScene()
    {
        // Master 씬 포함 전체 씬 탐색 (카메라는 Master 씬에 있을 수 있음)
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;
            var cam = FindRoomCameraInScene(s);
            if (cam != null) return cam;
        }

        // SceneManager로 못 찾으면 목록에서 첫 번째 반환
        if (cameras.Count > 0) return cameras[0];
        return null;
    }

    private Collider2D FindRoomBoundaryInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "RoomBoundary")
            {
                var col = root.GetComponent<Collider2D>();
                if (col != null) return col;
            }

            Transform found = root.transform.Find("RoomBoundary");
            if (found != null)
            {
                var col = found.GetComponent<Collider2D>();
                if (col != null) return col;
            }
        }
        return null;
    }

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

    private RoomData FindRoomDataInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            var data = root.GetComponentInChildren<RoomData>(true);
            if (data != null) return data;
        }
        return null;
    }

    private RoomData FindRoomDataInAnyScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded || s.name == "Master") continue;
            var data = FindRoomDataInScene(s);
            if (data != null) return data;
        }
        return null;
    }
}
