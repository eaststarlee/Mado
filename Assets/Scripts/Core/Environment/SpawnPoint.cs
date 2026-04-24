using UnityEngine;

/// <summary>
/// 플레이어 스폰 위치 마커 컴포넌트.
/// 룸 씬 내 "SpawnPoints" 하위에 배치한다.
///
/// ■ SpawnId 규칙
///   "Left"    — 왼쪽 RoomTransition을 통해 진입 시 스폰 위치
///   "Right"   — 오른쪽 RoomTransition을 통해 진입 시 스폰 위치
///   "Up"      — 위쪽 RoomTransition을 통해 진입 시 스폰 위치
///   "Down"    — 아래쪽 RoomTransition을 통해 진입 시 스폰 위치
///   "Default" — 씬 기본 스폰 위치, 폴백으로도 사용됨
///
/// ■ SceneLoader 연동
///   SceneLoader.TeleportPlayerToSpawn(spawnId) 에서 씬 내 SpawnPoint[]를 탐색,
///   SpawnId가 일치하는 것으로 플레이어를 이동시킵니다.
///   일치하는 SpawnId가 없으면 "Default" SpawnPoint로 폴백합니다.
///
/// ■ 배치 방법
///   1. 룸 씬에 "SpawnPoints" 빈 GameObject 생성
///   2. 하위에 방향별 SpawnPoint GameObject 생성 (예: SpawnPoint_Left)
///   3. 이 컴포넌트 부착 후 SpawnId 설정
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("스폰 지점 식별자. SceneLoader.TeleportPlayerToSpawn()에서 매칭에 사용됩니다.\n" +
             "규칙: Left / Right / Up / Down / Default")]
    [SerializeField] private string spawnId = "Default";

    /// <summary>외부(SceneLoader)에서 읽을 수 있는 스폰 ID.</summary>
    public string SpawnId => spawnId;

#if UNITY_EDITOR
    // ── 에디터 전용 ──────────────────────────────────────
    [Header("에디터 전용")]
    [Tooltip("기즈모 표시 색상. SpawnId에 따라 자동 설정됩니다.")]
    [SerializeField] private bool useAutoColor = true;

    private Color GetGizmoColor()
    {
        if (!useAutoColor) return Color.cyan;
        return spawnId switch
        {
            "Left"    => new Color(0.2f, 0.6f, 1f),   // 파란색
            "Right"   => new Color(1f, 0.5f, 0.1f),   // 주황색
            "Up"      => new Color(0.2f, 1f, 0.4f),   // 초록색
            "Down"    => new Color(1f, 0.2f, 0.5f),   // 분홍색
            "Default" => Color.yellow,                 // 노란색
            _         => Color.white
        };
    }

    private void OnDrawGizmos()
    {
        Color color = GetGizmoColor();
        Gizmos.color = color;

        // 스폰 위치 구체
        Gizmos.DrawSphere(transform.position, 0.18f);

        // 방향 화살표 (Left/Right/Up/Down)
        Vector3 arrowDir = spawnId switch
        {
            "Left"  => Vector3.left,
            "Right" => Vector3.right,
            "Up"    => Vector3.up,
            "Down"  => Vector3.down,
            _       => Vector3.zero
        };

        if (arrowDir != Vector3.zero)
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.6f);
            Gizmos.DrawRay(transform.position, arrowDir * 0.4f);
        }

        // 레이블
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.35f,
            $"Spawn: {spawnId}"
        );
    }

    private void OnDrawGizmosSelected()
    {
        // 선택 시 플레이어 예상 크기(캡슐) 미리보기
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawCube(transform.position + Vector3.up * 0.5f, new Vector3(0.6f, 1f, 0.1f));
    }
#endif
}
