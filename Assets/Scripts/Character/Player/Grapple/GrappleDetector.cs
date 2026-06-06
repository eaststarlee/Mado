using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// GrappleTarget 레이어의 모든 Collider2D를 감지하는 컴포넌트.
/// 
/// 포인트 쿨타임 키 전략:
///   Tilemap 콜라이더 → Tilemap.WorldToCell + 타일 내부 nudge → 셀 해시 (안정)
///   일반 콜라이더    → col.GetInstanceID()                   → 인스턴스 ID (완전 안정)
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class GrappleDetector : MonoBehaviour
{
    [SerializeField] private GrappleData data;
    [Tooltip("Linecast 시작점 수직 오프셋. 발(0)에서 시작하면 지면이 즉시 막힘 → 중심(0.5~1) 권장")]
    [SerializeField] private float losOriginOffset = 0.7f;
    [Tooltip("Grapple 감지 및 렌더링 원의 중심 오프셋 (몸통 위치 보정용)")]
    [SerializeField] private Vector2 detectionCircleOffset = new Vector2(0f, 0.9f);

    [Header("시각화 (Debug/Visuals)")]
    [Tooltip("게임 뷰에서 감지 반경 원을 그릴지 여부")]
    [SerializeField] private bool showDetectionCircle = true;
    [SerializeField] private Color circleColorNoTarget = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] private Color circleColorHasTarget = new Color(0f, 1f, 0.4f, 0.4f);
    [SerializeField] private int circleSegments = 36;
    [SerializeField] private float circleLineWidth = 0.05f;

    // ==================== 공개 상태 ====================

    /// <summary>
    /// 감지된 가장 가까운 그래플 포인트의 위치.
    /// [주의] 이 위치로 이동하는 것이 아님. 포인트 존재 여부(HasTarget) 확인 및
    ///        GrappleVisualizer 등 시각 요소에만 사용.
    ///        실제 이동 방향은 PlayerGrappleAimState에서 화살표 입력으로 결정됨.
    /// </summary>
    public Vector2? NearestTargetPosition { get; private set; }

    /// <summary>
    /// 가장 가까운 유효 포인트의 쿨타임 키 (int).
    /// RegisterKey()에 전달하여 포인트별 쿨타임 등록에 사용.
    /// </summary>
    public int NearestKey { get; private set; }

    /// <summary>유효한 그래플링 포인트가 감지 중인지 여부 (그래플링 발동 조건)</summary>
    public bool HasTarget => NearestTargetPosition.HasValue;

    // ==================== 내부 ====================

    // 코요테 타임 용 저장 변수
    private float coyoteTimer;
    private Vector2 lastNearestPosition;
    private int lastNearestKey;

    // Gizmo용: 유효/LOS차단 목적지 리스트 (쿨타임 포인트는 표시 안 함)
    private readonly List<Vector2> validPositions    = new List<Vector2>();
    private readonly List<int>     validKeys         = new List<int>(); // validPositions 병렬 키
    private readonly List<Vector2> blockedPositions  = new List<Vector2>(); // LOS 차단만

    // 포인트별 쿨타임: int 키 → 쿨타임 종료 시각
    private readonly Dictionary<int, float> pointCooldowns = new Dictionary<int, float>();

    // 시각화용 LineRenderer
    private LineRenderer circleRenderer;

    private void Start()
    {
        if (showDetectionCircle)
        {
            circleRenderer = gameObject.AddComponent<LineRenderer>();
            circleRenderer.positionCount = circleSegments + 1;
            circleRenderer.startWidth = circleLineWidth;
            circleRenderer.endWidth = circleLineWidth;
            circleRenderer.useWorldSpace = true;
            circleRenderer.loop = true;
            circleRenderer.sortingOrder = 100; // 위로 표시
            
            // 그림자를 쓰지 않는 기본 머티리얼
            Material mat = new Material(Shader.Find("Sprites/Default"));
            circleRenderer.material = mat;
        }
    }

    private void Update()
    {
        if (coyoteTimer > 0f)
        {
            coyoteTimer -= Time.deltaTime;
        }

        DetectTargets();
        UpdateCircleGrahpic();
    }

    // ==================== 감지 ====================

    private void DetectTargets()
    {
        validPositions.Clear();
        validKeys.Clear();
        blockedPositions.Clear();

        if (data == null)
        {
            NearestTargetPosition = null;
            return;
        }

        // 1단계: 주변 콜라이더 모두 감지
        // 기존 GrappleTarget 레이어(하위 호환) + 현재 세계의 모든 지형 레이어 혼합 검색
#pragma warning disable CS0618
        LayerMask searchMask = data.grappleTargetLayer;
        if (DimensionManager.Instance != null)
            searchMask |= DimensionManager.Instance.CurrentWorldMask;
#pragma warning restore CS0618

        Vector2 searchCenter = (Vector2)transform.position + detectionCircleOffset;
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            searchCenter,
            data.detectionRadius,
            searchMask
        );

        if (hits.Length == 0)
        {
            HandleNoTargetFound();
            return;
        }

        // 2단계: 각 콜라이더에서 가장 가까운 점 계산 → LOS 체크 → 쿨타임 필터
        Vector2 myPos    = transform.position;
        Vector2 losOrigin = myPos + Vector2.up * losOriginOffset;

        foreach (var col in hits)
        {
            if (col == null) continue;

            // [필터링]
            // 1. 기존 GrappleTarget 레이어면 무조건 허용 (하위 호환)
            // 2. 그 외 지형 레이어면 SurfaceInfo.type == GrapplePoint 인지 확인
            bool isLegacyGrapple = ((1 << col.gameObject.layer) & data.grappleTargetLayer) != 0;
            if (!isLegacyGrapple)
            {
                SurfaceInfo info;
                if (!col.TryGetComponent(out info))
                {
                    if (col.transform.parent != null)
                        col.transform.parent.TryGetComponent(out info);
                }

                if (info == null || info.type != SurfaceType.GrapplePoint)
                    continue; // 일반 지형이므로 무시
            }

            Vector2 closestPoint = col.ClosestPoint(myPos);

            // [수정] 밀착 차단 로직 제거
            // 현재 그래플링은 포인트로 끌려가는 방식이 아니라 8방향 대시 방식이므로
            // 플레이어가 포인트에 완전히 겹쳐 있어도 감지되어야 함.

            // LOS 체크
            // losOrigin이 myPos + 오프셋이므로, 포인트와 완전히 겹친 경우 dirToTarget이 zero가 될 수 있음.
            // 이때는 LOS 방해 없이 통과로 처리 (빈 방향 = 막힌 것 없음)
            Vector2 toClosest = closestPoint - losOrigin;
            Vector2 dirToTarget = toClosest.sqrMagnitude > 0.001f ? toClosest.normalized : Vector2.up;
            Vector2 checkPoint  = closestPoint - dirToTarget * 0.2f;
            RaycastHit2D hit    = Physics2D.Linecast(losOrigin, checkPoint, data.losBlockerLayer);

            if (hit.collider != null)
            {
                // LOS 차단 → 기즈모 빨간 선 표시
                blockedPositions.Add(closestPoint);
                continue;
            }

            // 안정적인 쿨타임 키 계산
            int key = ComputeKey(col, closestPoint, myPos);

            if (data.pointCooldown > 0f &&
                pointCooldowns.TryGetValue(key, out float endTime) &&
                Time.time < endTime)
            {
                // 쿨타임 중 → 표시 없음 (화살표/기즈모 모두 숨김)
            }
            else
            {
                validPositions.Add(closestPoint);
                validKeys.Add(key);
            }
        }

        // 3단계: 유효 포인트 중 최근접 선택
        if (validPositions.Count == 0)
        {
            HandleNoTargetFound();
            return;
        }

        int nearestIdx     = 0;
        float minSqrDist   = (validPositions[0] - myPos).sqrMagnitude;

        for (int i = 1; i < validPositions.Count; i++)
        {
            float sqrDist = (validPositions[i] - myPos).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                nearestIdx = i;
            }
        }

        NearestTargetPosition = validPositions[nearestIdx];
        NearestKey            = validKeys[nearestIdx];

        // 유효 타겟 갱신 시 코요테 변수 저장
        lastNearestPosition = NearestTargetPosition.Value;
        lastNearestKey      = NearestKey;
        
        if (data != null)
        {
            coyoteTimer = data.coyoteTime;
        }
    }

    private void HandleNoTargetFound()
    {
        if (coyoteTimer > 0f)
        {
            NearestTargetPosition = lastNearestPosition;
            NearestKey = lastNearestKey;
        }
        else
        {
            NearestTargetPosition = null;
        }
    }

    // ==================== 시각화 ====================

    private void UpdateCircleGrahpic()
    {
        if (circleRenderer == null || data == null) return;

        circleRenderer.enabled = showDetectionCircle;
        if (!showDetectionCircle) return;

        Color targetColor = HasTarget ? circleColorHasTarget : circleColorNoTarget;
        circleRenderer.startColor = targetColor;
        circleRenderer.endColor = targetColor;

        float radius = data.detectionRadius;
        Vector3 center = transform.position + (Vector3)detectionCircleOffset;
        float angleStep = 2f * Mathf.PI / circleSegments;

        for (int i = 0; i <= circleSegments; i++)
        {
            float currentAngle = i * angleStep;
            float x = center.x + Mathf.Cos(currentAngle) * radius;
            float y = center.y + Mathf.Sin(currentAngle) * radius;
            circleRenderer.SetPosition(i, new Vector3(x, y, center.z));
        }
    }

    // ==================== 키 계산 ====================

    /// <summary>
    /// 콜라이더 종류에 따라 플레이어 위치와 완전히 무관한 안정적 키를 반환.
    ///
    /// [Tilemap 계열] Tilemap.WorldToCell + 내부 nudge → 셀 해시
    ///   - closestPoint는 타일 경계에 위치 → nudge로 내부 샘플링하여 안정화
    ///
    /// [일반 Collider] col.GetInstanceID()
    ///   - 런타임 동안 완전히 고정된 값 (플레이어 위치와 무관)
    /// </summary>
    private int ComputeKey(Collider2D col, Vector2 closestPoint, Vector2 playerPos)
    {
        Tilemap tilemap = col.GetComponentInParent<Tilemap>();
        if (tilemap != null)
        {
            // 타일 표면(경계)에서 내부 방향으로 살짝 밀어 안정적인 셀 내부 좌표 샘플링
            Vector2 inward     = (closestPoint - playerPos).normalized * 0.05f;
            Vector3Int cell    = tilemap.WorldToCell(closestPoint + inward);
            // 셀 좌표를 int 해시로 변환 (좌표 범위 ±16383 안에서 충돌 없음)
            return unchecked(cell.x * 73856093 ^ cell.y * 19349663);
        }

        // 일반 Collider: 인스턴스 ID (런타임 동안 절대 변하지 않음)
        return col.GetInstanceID();
    }

    // ==================== 포인트 쿨타임 ====================

    /// <summary>
    /// 사용된 그래플 포인트의 키를 등록 → pointCooldown 동안 재감지 차단.
    /// PlayerController의 GrappleFreezeRoutine에서 대쉬 직전 호출.
    /// NearestKey를 캡처하여 전달할 것.
    /// </summary>
    public void RegisterKey(int key)
    {
        if (data == null || data.pointCooldown <= 0f) return;
        pointCooldowns[key] = Time.time + data.pointCooldown;
    }

    // ==================== Gizmos ====================

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (data == null) return;

        // 감지 원
        Gizmos.color = HasTarget
            ? new Color(0f, 1f, 0.4f, 0.12f)
            : new Color(1f, 1f, 0f, 0.07f);
        Vector3 searchCenter = transform.position + (Vector3)detectionCircleOffset;
        Gizmos.DrawWireSphere(searchCenter, data.detectionRadius);

        if (!Application.isPlaying) return;

        // 차단 포인트 시각화 제거 (선택적)
        // 유효 포인트 시각화 제거 (선택적)

        // 최근접 유효 포인트 → 흰 구체
        if (NearestTargetPosition.HasValue)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(NearestTargetPosition.Value, 0.2f);
        }
    }
#endif
}
