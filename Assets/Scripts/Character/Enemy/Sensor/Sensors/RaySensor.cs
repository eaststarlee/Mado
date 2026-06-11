using UnityEngine;

/// <summary>
/// 시야 기반 감지 센서.
/// Raycast로 벽 투시를 방지하고, 박스 범위(Box Overlap)를 사용합니다.
/// </summary>
public class RaySensor : SensorBase
{
    [Header("Vision Settings")]
    [Tooltip("감지 영역의 크기 (가로, 세로)")]
    [SerializeField] private Vector2 boxSize = new Vector2(10f, 5f);
    [Tooltip("감지 영역의 중심 오프셋 (X는 보는 방향 기준)")]
    [SerializeField] private Vector2 boxOffset = new Vector2(5f, 1f); 
    [SerializeField] private LayerMask targetLayer;   // 플레이어 레이어
    [SerializeField] private LayerMask obstacleLayer; // 벽/지형 레이어

    [Header("Debug")]
    [SerializeField] private Color visionColor = new Color(1f, 1f, 0f, 0.2f);
    [SerializeField] private bool showGizmos = true;

    protected override void Evaluate(EnemyBlackboard bb)
    {
        // 최적화: 타겟이 이미 있고, 거리/시야 내에 있는지 검증
        if (bb.Target.target != null)
        {
            if (CheckVisibility(bb.Target.target, bb))
            {
                return;
            }
        }

        // 타겟이 없거나 잃어버렸으면 재탐색 (Box Overlap)
        ScanForTarget(bb);
    }

    private void ScanForTarget(EnemyBlackboard bb)
    {
        Vector2 checkPos = GetCheckPosition();
        
        // 사각형 범위 내 타겟 검색
        Collider2D[] targets = Physics2D.OverlapBoxAll(checkPos, boxSize, 0f, targetLayer);
        
        foreach (var col in targets)
        {
            // 거리/장애물 최종 검증
            if (CheckVisibility(col.transform, bb))
            {
                // 하나라도 보이면 성공
                return; 
            }
        }
        
        // 아무것도 안 보임 -> 플래그 해제
        if (bb.Target.target != null)
        {
            // 기존 타겟이 시야에서 벗어남
            UpdateFlag(bb, false);
        }
    }

    private bool CheckVisibility(Transform target, EnemyBlackboard bb)
    {
        Vector2 myPos = transform.position;
        Vector2 targetPos = target.position;
        
        // 1. 박스 범위 체크 (단순 거리보다 정확한 사각형 판정)
        // 현재 내 facing에 맞춰 박스를 계산
        Vector2 checkPos = GetCheckPosition();
        Bounds bounds = new Bounds(checkPos, new Vector3(boxSize.x, boxSize.y, 1f));
        
        if (!bounds.Contains(targetPos))
        {
            UpdateFlag(bb, false);
            return false;
        }

        // 2. 장애물(벽) 투시 방지 (Raycast)
        // 월드 마스크 사용
        LayerMask worldMask = obstacleLayer;
            
        Vector2 eyePos = myPos + new Vector2(0f, boxOffset.y); 
        Vector2 dirToTarget = (targetPos - eyePos).normalized;
        float distToTarget = Vector2.Distance(eyePos, targetPos);

        RaycastHit2D hit = Physics2D.Raycast(eyePos, dirToTarget, distToTarget, worldMask);
        if (hit.collider != null)
        {
            UpdateFlag(bb, false);
            return false;
        }

        // 감지 성공
        UpdateFlag(bb, true);
        
        // 정보 갱신
        bb.Target.target = target;
        bb.Target.lastKnownPosition = targetPos;
        bb.Target.distance = distToTarget;
        bb.Target.direction = dirToTarget;
        
        return true;
    }

    private Vector2 GetCheckPosition()
    {
        float facingDir = 1f;

        // 1. Blackboard (Runtime)
        var entity = GetComponentInParent<EnemyEntity>(); // Parent or Self
        if (entity != null && entity.Blackboard != null)
        {
            facingDir = entity.Blackboard.Movement.facingDirection;
            if (facingDir == 0) facingDir = 1f;
        }
        else
        {
            // 2. SpriteRenderer (Editor/Fallback)
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                facingDir = sr.flipX ? -1f : 1f;
            }
        }
        
        Vector2 offset = new Vector2(boxOffset.x * facingDir, boxOffset.y);
        return (Vector2)transform.position + offset;
    }

    private void UpdateFlag(EnemyBlackboard bb, bool isVisible)
    {
        if (isVisible)
            bb.Target.source |= DetectionSource.Vision;
        else
            bb.Target.source &= ~DetectionSource.Vision;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = visionColor;
        
        // Gizmo용 Facing 계산 (GetCheckPosition 로직 복제/최적화)
        float facingDir = 1f;
        
        // Editor에서는 GetComponent 사용이 안전
        var entity = GetComponentInParent<EnemyEntity>();
        if (Application.isPlaying && entity != null && entity.Blackboard != null)
        {
             facingDir = entity.Blackboard.Movement.facingDirection;
             if (facingDir == 0) facingDir = 1f;
        }
        else
        {
             var sr = GetComponentInChildren<SpriteRenderer>();
             if (sr != null)
             {
                 facingDir = sr.flipX ? -1f : 1f;
             }
        }

        Vector2 offset = new Vector2(boxOffset.x * facingDir, boxOffset.y);
        Vector2 center = (Vector2)transform.position + offset;
        
        Gizmos.DrawWireCube(center, boxSize);
        
        // 채워진 박스 (투명도)
        Gizmos.color = new Color(visionColor.r, visionColor.g, visionColor.b, 0.1f);
        Gizmos.DrawCube(center, boxSize);
    }
#endif
}
