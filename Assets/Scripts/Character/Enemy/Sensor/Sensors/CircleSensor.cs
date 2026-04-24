using UnityEngine;

/// <summary>
/// 근접 감지 센서 (원형).
/// 가장 기본적인 감지 수단으로, 물리적 거리를 기반으로 감지합니다.
/// </summary>
public class CircleSensor : SensorBase
{
    [Header("Proximity Settings")]
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private float midRange = 5f; // AttackableRangeSensor가 있지만 거리 체크용으로 유지
    [SerializeField] private LayerMask targetLayer;
    
    // Memory 관련 필드는 TargetContext로 이동됨.
    
    protected override void Evaluate(EnemyBlackboard bb)
    {
        // OverlapCircle로 타겟 감지
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, targetLayer);
        
        if (hit != null)
        {
            Transform target = hit.transform;
            bb.Target.target = target;
            
            // 거리/방향 정보 갱신
            float dist = Vector2.Distance(transform.position, target.position);
            Vector2 dir = (target.position - transform.position).normalized;
            
            bb.Target.distance = dist;
            bb.Target.direction = dir;
            bb.Target.lastKnownPosition = target.position;
            
            // Flag 설정 (감지 성공)
            bb.Target.source |= DetectionSource.Proximity;
            
            // 거리 기반 추가 정보
            bb.Target.isInMidRange = dist <= midRange;
        }
        else
        {
            // Flag 해제 (감지 실패)
            bb.Target.source &= ~DetectionSource.Proximity;
            bb.Target.isInMidRange = false;
            
            // Memory Decay는 Blackboard.Target.UpdateDecay()에서 일괄 처리되므로 삭제
        }
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 감지 범위
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // 근접 범위

        
        // 중거리 범위
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, midRange);
    }
#endif
}
