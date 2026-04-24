using UnityEngine;

/// <summary>
/// 룸 경계 트리거 컴포넌트.
/// 플레이어가 이 트리거에 진입하면 SceneLoader.LoadNextRoom()을 호출하여 다음 룸으로 전환합니다.
///
/// ■ 배치 방법
///   1. 룸 씬의 출구 위치에 빈 GameObject 생성 (예: RoomTransition_Right)
///   2. BoxCollider2D(또는 PolygonCollider2D) 추가 → IsTrigger: true
///   3. 이 컴포넌트 부착 후 Inspector 설정
///      - transitionDirection : 이 출구의 방향 (Left/Right/Up/Down)
///      - nextSceneName       : 다음 룸의 씬 이름
///      - spawnIdInNextScene  : 다음 룸에서 스폰될 SpawnPoint ID
///
/// ■ SpawnId 자동 결정 규칙 (overrideSpawnId가 비어있으면 자동 계산)
///   Right 출구 → 다음 씬의 "Left"  SpawnPoint
///   Left  출구 → 다음 씬의 "Right" SpawnPoint
///   Up    출구 → 다음 씬의 "Down"  SpawnPoint
///   Down  출구 → 다음 씬의 "Up"    SpawnPoint
///
/// ■ 쿨타임
///   연속 전환 방지를 위해 transitionCooldown(기본 2초)이 적용됩니다.
/// </summary>
public class RoomTransition : MonoBehaviour
{
    // ── 방향 열거형 ────────────────────────────────────────
    public enum TransitionDirection { Left, Right, Up, Down }

    // ── Inspector ──────────────────────────────────────────
    [Header("출구 설정")]
    [Tooltip("이 출구의 방향. 도착 SpawnId 자동 결정에도 사용됩니다.")]
    [SerializeField] private TransitionDirection transitionDirection = TransitionDirection.Right;

    [Tooltip("이 출구로 나갔을 때 로드할 다음 룸 씬 이름 (확장자 없음).\n" +
             "예: Devil_Area01_002_GardenPath")]
    [SerializeField] private string nextSceneName;

    [Tooltip("다음 씬에서 플레이어가 스폰될 SpawnPoint ID.\n" +
             "비워두면 transitionDirection에 따라 자동 결정됩니다.\n" +
             "(Right→Left, Left→Right, Up→Down, Down→Up)")]
    [SerializeField] private string overrideSpawnId;

    [Header("진입 조건")]
    [Tooltip("이 트리거로 전환 가능한 레이어 태그. 기본값 Player.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("전환 쿨타임 (초). 연속 전환 방지용.")]
    [SerializeField] private float transitionCooldown = 2f;

    // ── 상태 ──────────────────────────────────────────────
    private float lastTransitionTime = -99f;

    // ── 공개 프로퍼티 ──────────────────────────────────────
    /// <summary>출구 방향</summary>
    public TransitionDirection Direction => transitionDirection;

    /// <summary>다음 씬 이름</summary>
    public string NextSceneName => nextSceneName;

    /// <summary>
    /// 다음 씬에서 사용할 SpawnId.
    /// overrideSpawnId가 있으면 그것을, 없으면 방향 반전 값을 사용합니다.
    /// </summary>
    public string SpawnIdInNextScene
    {
        get
        {
            if (!string.IsNullOrEmpty(overrideSpawnId))
                return overrideSpawnId;

            // 방향 반전: 내가 Right로 나가면 → 상대방 Left에 스폰
            return transitionDirection switch
            {
                TransitionDirection.Right => "Left",
                TransitionDirection.Left  => "Right",
                TransitionDirection.Up    => "Down",
                TransitionDirection.Down  => "Up",
                _                         => "Default"
            };
        }
    }

    // ── Unity Trigger ─────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        // 쿨타임 체크
        if (Time.time - lastTransitionTime < transitionCooldown)
        {
            Debug.Log($"[RoomTransition] 쿨타임 중 ({transitionCooldown - (Time.time - lastTransitionTime):F1}초 남음)");
            return;
        }

        // SceneLoader 체크
        if (SceneLoader.Instance == null)
        {
            Debug.LogError("[RoomTransition] SceneLoader.Instance가 없습니다. Master 씬에 SceneLoader가 배치되어 있는지 확인하세요.");
            return;
        }

        // 이미 전환 중이면 무시
        if (SceneLoader.Instance.IsTransitioning)
        {
            Debug.Log("[RoomTransition] 이미 전환 중입니다. 요청 무시.");
            return;
        }

        // 씬 이름 유효성 체크
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning($"[RoomTransition] '{gameObject.name}'의 nextSceneName이 비어 있습니다.", this);
            return;
        }

        lastTransitionTime = Time.time;

        // 진행 방향(X축) 도출
        int transitionDirX = 0;
        if (transitionDirection == TransitionDirection.Right) transitionDirX = 1;
        else if (transitionDirection == TransitionDirection.Left) transitionDirX = -1;

        Debug.Log($"[RoomTransition] 룸 전환: {nextSceneName} (SpawnId: {SpawnIdInNextScene}, transitionDirX: {transitionDirX})");
        SceneLoader.Instance.LoadNextRoom(nextSceneName, SpawnIdInNextScene, transitionDirX);
    }

#if UNITY_EDITOR
    // ── 에디터 기즈모 ──────────────────────────────────────
    private void OnDrawGizmos()
    {
        Color dirColor = transitionDirection switch
        {
            TransitionDirection.Left  => new Color(0.2f, 0.6f, 1f),
            TransitionDirection.Right => new Color(1f, 0.5f, 0.1f),
            TransitionDirection.Up    => new Color(0.2f, 1f, 0.4f),
            TransitionDirection.Down  => new Color(1f, 0.2f, 0.5f),
            _                         => Color.white
        };

        Gizmos.color = new Color(dirColor.r, dirColor.g, dirColor.b, 0.3f);

        // 트리거 영역 확인 (BoxCollider2D 크기 기준)
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(
                (Vector2)transform.position + col.offset,
                col.size
            );
        }

        // 방향 화살표
        Vector3 arrowDir = transitionDirection switch
        {
            TransitionDirection.Left  => Vector3.left,
            TransitionDirection.Right => Vector3.right,
            TransitionDirection.Up    => Vector3.up,
            TransitionDirection.Down  => Vector3.down,
            _                         => Vector3.zero
        };

        Gizmos.color = dirColor;
        Gizmos.DrawRay(transform.position, arrowDir * 0.8f);

        // 레이블
        string spawnLabel = string.IsNullOrEmpty(overrideSpawnId) ? SpawnIdInNextScene : overrideSpawnId;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.4f,
            $"→ {nextSceneName}\nSpawn: {spawnLabel}"
        );
    }

    private void OnDrawGizmosSelected()
    {
        // 선택 시 더 선명하게
        Color dirColor = transitionDirection switch
        {
            TransitionDirection.Left  => new Color(0.2f, 0.6f, 1f),
            TransitionDirection.Right => new Color(1f, 0.5f, 0.1f),
            TransitionDirection.Up    => new Color(0.2f, 1f, 0.4f),
            TransitionDirection.Down  => new Color(1f, 0.2f, 0.5f),
            _                         => Color.white
        };

        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(dirColor.r, dirColor.g, dirColor.b, 0.7f);
            Gizmos.DrawWireCube(
                (Vector2)transform.position + col.offset,
                col.size
            );
        }
    }
#endif
}
