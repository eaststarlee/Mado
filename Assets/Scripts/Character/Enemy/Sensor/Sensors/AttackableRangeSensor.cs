using UnityEngine;

/// <summary>
/// 박스 형태의 공격 사거리 체크 센서.
/// 감지(Detection)와 무관하게 "공격이 닿는지"만 엄격히 검사하여 Y축 떨림 해결.
/// </summary>
public class AttackableRangeSensor : SensorBase
{
    [Header("Range Check")]
    [SerializeField] private Vector2 boxSize = new Vector2(1.5f, 1.0f);
    [SerializeField] private Vector2 offset = new Vector2(0.75f, 0f); // 몬스터 기준 앞쪽
    [SerializeField] private LayerMask targetLayer;

    [Header("Debug")]
    [SerializeField] private Color rangeColor = new Color(1f, 0f, 0f, 0.4f);

    protected override void Evaluate(EnemyBlackboard bb)
    {
        // 타겟이 없으면 검사 불필요
        if (bb.Target.target == null)
        {
            bb.Target.isInMeleeRange = false;
            return;
        }

        Vector2 origin = transform.position;
        int facing = bb.Movement.facingDirection;
        if (facing == 0) facing = 1;

        // Facing에 따라 Offset X 반전
        Vector2 finalOffset = new Vector2(offset.x * facing, offset.y);
        Vector2 checkPos = origin + finalOffset;

        // Box Overlap
        Collider2D hit = Physics2D.OverlapBox(checkPos, boxSize, 0f, targetLayer);

        // 내 타겟이 박스 안에 있는지 확인
        if (hit != null && hit.transform == bb.Target.target)
        {
            bb.Target.isInMeleeRange = true;
        }
        else
        {
            bb.Target.isInMeleeRange = false;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = rangeColor;
        
        // 에디터에서는 Facing 정보를 알 수 없으므로(실행 중 아니면), 그냥 오른쪽 기준으로 그림
        Vector2 origin = transform.position;
        Vector2 finalOffset = offset;
        
        // 실행 중이면 Blackboard/Transform 참조 시도
        // (간단히 X scale로 추정하거나 실행 중 facing 사용)
        if (Application.isPlaying)
        {
            // 여기서는 EnemyEntity 접근이 어려우므로 Transform Scale 사용
            float sign = Mathf.Sign(transform.lossyScale.x);
             // 하지만 EnemyMotor가 Scale을 뒤집으므로 lossyScale 확인 가능
             finalOffset.x *= sign;
        }

        Gizmos.DrawWireCube(origin + finalOffset, boxSize);
    }
#endif
}
