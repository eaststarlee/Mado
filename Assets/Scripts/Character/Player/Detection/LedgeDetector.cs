using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 렛지 클라임 감지 시스템 (Snap & Tween 방식)
/// 역할: L-Shape Scan으로 목표 좌표 계산.
///
/// [레이어 판별 방식]
/// - DimensionManager.CurrentWorldMask 기반으로 현재 세계 지형을 모두 감지.
/// - 클라임 가능 여부는 SurfaceInfo.type == Climbable 로 판별.
/// - SurfaceInfo는 Collider 인스턴스 ID 기반 Dictionary에 캐싱하여 GC 최소화.
///   (타일맵 루트에 SurfaceInfo가 부착되어 있어야 GetComponentInParent 동작)
/// </summary>
public class LedgeDetector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float wallCheckDist = 0.5f; // 벽 감지 거리

    private PlayerController player;
    private BoxCollider2D playerCollider;

    // 인스턴스 ID 기반 SurfaceInfo 캐시 (GC 최소화)
    private readonly Dictionary<int, SurfaceInfo> surfaceInfoCache
        = new Dictionary<int, SurfaceInfo>();

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        playerCollider = player.GetComponent<BoxCollider2D>();
    }

    private RaycastHit2D GetSolidBoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, LayerMask mask)
    {
        var hits = Physics2D.BoxCastAll(origin, size, angle, direction, distance, mask);
        foreach(var hit in hits) if (!hit.collider.isTrigger) return hit;
        return default(RaycastHit2D);
    }

    private RaycastHit2D GetSolidRaycast(Vector2 origin, Vector2 direction, float distance, LayerMask mask)
    {
        var hits = Physics2D.RaycastAll(origin, direction, distance, mask);
        foreach(var hit in hits) if (!hit.collider.isTrigger) return hit;
        return default(RaycastHit2D);
    }

    private bool CheckSolidOverlapBox(Vector2 point, Vector2 size, float angle, LayerMask mask)
    {
        var hits = Physics2D.OverlapBoxAll(point, size, angle, mask);
        foreach(var hit in hits) if (!hit.isTrigger) return true;
        return false;
    }

    /// <summary>
    /// 렛지 감지 및 착지 목표 지점 반환 (실패 시 null)
    /// L-Shape Scan: Wall Check + Ledge Check + Clearance Check
    /// </summary>
    public Vector2? ScanLedgeTarget()
    {
        int dir = player.IsFacingRight ? 1 : -1;
        Vector2 pos = transform.position;
        Vector2 boxSize = playerCollider.size;
        Vector2 center = (Vector2)transform.position + playerCollider.offset;

        // 데이터 참조
        float ledgeScanHeight = player.ActiveFormData.wall.ledgeScanHeight;
        float landOffset = player.ActiveFormData.wall.ledgeLandOffset;

        LayerMask worldMask = DimensionManager.Instance.CurrentWorldMask;

        // ========================================================
        // 1. [Wall Check] 앞에 벽이 있는가? + SurfaceInfo.Climbable 검증
        // ========================================================
        RaycastHit2D wallHit = GetSolidBoxCast(
            center,
            boxSize * 0.9f,
            0f,
            Vector2.right * dir,
            wallCheckDist,
            worldMask
        );

        if (wallHit.collider == null) return null; // 벽 없음

        // SurfaceInfo.type 검증: ClimbGround 속성이 아니면 렛지 클라임 불가
        SurfaceInfo wallSurface = GetCachedSurfaceInfo(wallHit.collider);
        if (wallSurface == null || wallSurface.type != SurfaceType.ClimbGround) return null;

        // ========================================================
        // 2. [Ledge Check] 벽 윗면(착지점)이 있는가?
        // ========================================================
        Vector2 ledgeRayOrigin = new Vector2(
            wallHit.point.x + (dir * 0.2f),
            pos.y + ledgeScanHeight
        );

        RaycastHit2D ledgeHit = GetSolidRaycast(
            ledgeRayOrigin,
            Vector2.down,
            ledgeScanHeight + 0.5f,
            worldMask
        );

        if (ledgeHit.collider == null) return null; // 윗면 없음 (무한 벽)

        // ========================================================
        // 3. [Clearance Check] 착지할 공간(머리 위)이 비었는가?
        // ========================================================
        Vector2 targetFloor = ledgeHit.point;
        Vector2 clearanceCenter = targetFloor + Vector2.up * (boxSize.y * 0.5f);
        Vector2 clearanceSize = new Vector2(boxSize.x * 0.8f, boxSize.y * 0.9f);

        bool hitCeiling = CheckSolidOverlapBox(
            clearanceCenter,
            clearanceSize,
            0f,
            worldMask
        );

        if (hitCeiling) return null; // 천장 막힘

        // ========================================================
        // 4. [최대 높이 체크] 너무 높은 벽은 스캔 단계에서 차단
        // ========================================================
        float climbHeight = ledgeHit.point.y - pos.y;
        if (climbHeight > player.ActiveFormData.wall.ledgeMaxClimbHeight)
        {
            return null; // 너무 높음
        }

        // ========================================================
        // 5. [Final Target Calculation] 최종 좌표 계산
        // ========================================================
        float targetX = ledgeHit.point.x + (dir * landOffset);
        float targetY = ledgeHit.point.y;

        // [핵심] Pivot 보정 (캐릭터 Pivot이 중앙일 경우)
        // 바닥 표면에서 캐릭터 절반 높이 위로 올려야 함
        targetY += boxSize.y * 0.5f;

        Vector2 finalPos = new Vector2(targetX, targetY);

        // ========================================================
        // 🔥 [Grid Snap] 0.5 Grid 정렬 (필수)
        // ========================================================
        finalPos = GridSnap05(finalPos);
        
        // ========================================================
        // [벽 표면 X 계산] Phase 1.5에서 사용
        // 벽 표면 X = ledgeHit.point.x (벽 윗면 가장자리)
        // 캐릭터 콜라이더 절반 더해서 벽 위에 서도록
        // ========================================================
        float ledgeSurfaceX = ledgeHit.point.x + (dir * boxSize.x * 0.5f);
        player.LedgeClimbState.SetLedgeSurfaceX(ledgeSurfaceX);
        
        // [디버그]
        Debug.Log($"[LedgeDetector] Target={finalPos}, LedgeSurfaceX={ledgeSurfaceX}");

        // 디버그
        Debug.DrawLine(pos, finalPos, Color.cyan, 1.0f);

        return finalPos;
    }

    /// <summary>
    /// 0.5 Grid Snap (부동소수 오차 제거)
    /// </summary>
    private Vector2 GridSnap05(Vector2 v)
    {
        return new Vector2(
            Mathf.Round(v.x * 2f) * 0.5f,
            Mathf.Round(v.y * 2f) * 0.5f
        );
    }

    /// <summary>
    /// 인스턴스 ID 기반 SurfaceInfo 캐싱 조회 (GC 최소화).
    /// 타일맵의 경우 CompositeCollider 오브젝트 자체에는 SurfaceInfo가 없으므로
    /// 부모(루트 오브젝트)를 TryGetComponent로 탐색.
    /// </summary>
    private SurfaceInfo GetCachedSurfaceInfo(Collider2D col)
    {
        if (col == null) return null;

        int id = col.GetInstanceID();
        if (surfaceInfoCache.TryGetValue(id, out SurfaceInfo cached))
            return cached; // null이 캐싱된 경우도 재탐색 방지

        // 1차: 콜라이더 자신에서 직접 탐색
        SurfaceInfo info = null;
        if (!col.TryGetComponent(out info) && col.transform.parent != null)
        {
            // 2차: 타일맵 루트 오브젝트(부모) 탐색
            col.transform.parent.TryGetComponent(out info);
        }

        surfaceInfoCache[id] = info; // null도 캐싱 (재탐색 방지)
        return info;
    }

    /// <summary>
    /// 트리거 활성화/비활성화 (호환성 유지)
    /// </summary>
    public void SetActive(bool active)
    {
        enabled = active;
    }

    /// <summary>
    /// L-Shape Scan 범위 시각화 (Scene View)
    /// </summary>
    private void OnDrawGizmos()
    {
        if (player == null) return;
        if (player.ActiveFormData == null) return;

        int dir = player.IsFacingRight ? 1 : -1;
        Vector2 pos = transform.position;
        
        // 데이터 참조
        float scanHeight = player.ActiveFormData.wall.ledgeScanHeight;
        float maxHeight = player.ActiveFormData.wall.ledgeMaxClimbHeight;
        float landOffset = player.ActiveFormData.wall.ledgeLandOffset;

        // ========== 1. Wall Check 범위 (노란색) ==========
        Gizmos.color = Color.yellow;
        Vector3 wallCheckStart = (Vector3)pos;
        Vector3 wallCheckEnd = (Vector3)pos + Vector3.right * dir * wallCheckDist;
        Gizmos.DrawLine(wallCheckStart, wallCheckEnd);
        
        // Wall Check 끝점 표시
        Gizmos.DrawWireSphere(wallCheckEnd, 0.05f);

        // ========== 2. Ledge Scan 높이 범위 (시안색) ==========
        Gizmos.color = Color.cyan;
        
        // Scan 시작점 (머리 위)
        Vector3 scanStart = new Vector3(pos.x + (dir * 0.2f), pos.y + scanHeight, 0);
        Vector3 scanEnd = new Vector3(pos.x + (dir * 0.2f), pos.y - 0.5f, 0);
        Gizmos.DrawLine(scanStart, scanEnd);
        
        // Scan 시작점 표시
        Gizmos.DrawWireSphere(scanStart, 0.08f);

        // ========== 3. 최대 등반 높이 (마젠타) ==========
        Gizmos.color = Color.magenta;
        Vector3 maxHeightStart = (Vector3)pos;
        Vector3 maxHeightEnd = (Vector3)pos + Vector3.up * maxHeight;
        
        // 세로선
        Gizmos.DrawLine(maxHeightStart, maxHeightEnd);
        
        // 가로선 (최대 높이 지점)
        Gizmos.DrawLine(maxHeightEnd + Vector3.left * 0.3f, maxHeightEnd + Vector3.right * 0.3f);

        // ========== 4. 착지 오프셋 (초록색) ==========
        Gizmos.color = Color.green;
        // 예상 착지 지점 (대략적 표시)
        Vector3 landPos = new Vector3(pos.x + (dir * landOffset), pos.y + scanHeight * 0.5f, 0);
        Gizmos.DrawWireCube(landPos, new Vector3(0.2f, 0.2f, 0));
    }
}
