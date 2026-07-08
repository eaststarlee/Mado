using UnityEngine;
using Mado.Visual.Environment;

/// <summary>
/// 룸 씬 메타데이터 컴포넌트.
/// 각 룸 씬 내 "RoomData" GameObject에 부착한다.
///
/// ■ 역할
///   - 씬 식별 정보 보관 (roomId, world, area, roomIndex)
///   - 미니맵 연동 데이터 (gridPosition, gridSize, roomType)
///   - 인접 룸 연결 씬 이름 (RoomTransition 자동 검증용)
///   - 씬 로드 후 GameEvents.OnRoomEntered 자동 발생
///
/// ■ 배치 방법
///   1. 룸 씬 루트에 빈 GameObject "RoomData" 생성
///   2. 이 컴포넌트를 부착
///   3. Inspector에서 roomId = 씬 파일명 (예: Devil_Area01_001_EntranceHall)
///   4. world, areaName, areaIndex, roomIndex 등 설정
/// </summary>
public class RoomData : MonoBehaviour
{
    // ── 방 분위기 (Atmosphere) ──────────────────────────────
    [Header("방 분위기 (Atmosphere)")]
    [Tooltip("이 룸에 진입했을 때 자동으로 깔릴 기본 분위기 프로필 (트리거 없이 적용됨)")]
    public BiomeAtmosphereProfile defaultBiomeProfile;

    // ── 방 식별 ────────────────────────────────────────────
    [Header("방 식별")]
    [Tooltip("씬 파일명과 동일하게 입력 (예: Devil_Area01_001_EntranceHall)\n" +
             "SceneLoader, DimensionManager가 이 값을 기준으로 씬을 식별합니다.")]
    public string roomId;

    [Tooltip("이 룸이 속한 세계")]
    public WorldType world;

    [Tooltip("반대 차원(세계)의 대응 씬 이름 (접두어 규칙 없이 직접 연결)\n" +
             "입력하지 않으면 이 룸에서는 차원 전환이 불가능합니다.")]
    public string otherWorldSceneName;

    [Tooltip("미니맵 구역 표시용 이름 (예: 황야, 악마성)")]
    public string areaName;

    [Tooltip("구역 번호 (Area 코드의 숫자 부분, 예: 1 → Area01)")]
    public int areaIndex;

    [Tooltip("룸 번호 (같은 Area 내 순번, 예: 2 → _002_)")]
    public int roomIndex;

    // ── 미니맵 ────────────────────────────────────────────
    [Header("미니맵")]
    [Tooltip("미니맵 그리드 좌표 (X=가로, Y=세로 위치)")]
    public Vector2Int gridPosition;

    [Tooltip("룸이 차지하는 그리드 크기 (1x1=일반, 2x1=가로로 넓은 룸 등)")]
    public Vector2Int gridSize = Vector2Int.one;

    [Tooltip("룸 종류 (미니맵 아이콘 및 색상 결정에 사용)")]
    public RoomType roomType = RoomType.Normal;

    // ── 인접 룸 연결 ──────────────────────────────────────
    [Header("인접 룸 연결 씬 이름")]
    [Tooltip("왼쪽 출구와 연결된 씬 이름 (없으면 빈 칸)\n예: Devil_Area01_002_GardenPath")]
    public string connectedScene_Left;

    [Tooltip("오른쪽 출구와 연결된 씬 이름")]
    public string connectedScene_Right;

    [Tooltip("위쪽 출구와 연결된 씬 이름")]
    public string connectedScene_Up;

    [Tooltip("아래쪽 출구와 연결된 씬 이름")]
    public string connectedScene_Down;

    [Header("카메라 설정")]
    [Tooltip("이 방에서 카메라의 Y축 추적을 고정할지 여부")]
    public bool lockCameraY = false;

    // ── Unity Lifecycle ────────────────────────────────────
    private void Start()
    {
        // SceneLoader가 없는 환경(에디터 단독 씬 테스트 등)에서도
        // OnRoomEntered 이벤트가 반드시 발생하도록 폴백 처리.
        // SceneLoader가 있는 정상 플레이 시에는 SceneLoader.RaiseRoomEnteredEvent()가 담당.
        if (SceneLoader.Instance == null)
        {
            GameEvents.RaiseRoomEntered(this);
        }

#if UNITY_EDITOR
        ValidateRoomId();
#endif
    }

#if UNITY_EDITOR
    // ── 에디터 검증 ─────────────────────────────────────
    private void ValidateRoomId()
    {
        if (string.IsNullOrEmpty(roomId))
        {
            Debug.LogWarning($"[RoomData] '{gameObject.name}' roomId가 비어 있습니다. " +
                             "씬 파일명과 동일하게 입력하세요.", this);
            return;
        }

        // roomId가 씬 이름과 다를 때 경고
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene != "Master" && roomId != currentScene)
        {
            Debug.LogWarning($"[RoomData] roomId '{roomId}' 가 현재 씬 이름 '{currentScene}' 과 다릅니다. " +
                             "오타가 아닌지 확인하세요.", this);
        }
    }

    private void OnDrawGizmos()
    {
        // 에디터에서 RoomData 위치를 쉽게 확인
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f); // 반투명 노란색
        Gizmos.DrawCube(transform.position, Vector3.one * 0.4f);

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            $"[RoomData]\n{roomId}\n{world} | {roomType}"
        );
    }
#endif
}

/// <summary>룸 종류 열거형.</summary>
public enum RoomType
{
    Normal,
    Boss,
    SavePoint,
    Shop,
    Secret,
    Transition
}
