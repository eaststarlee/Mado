using UnityEngine;

/// <summary>
/// 지면/벽/낭떠러지 감지 센서.
/// Raycast 기반으로 MovementContext 갱신.
/// </summary>
public class GroundSensor : SensorBase
{
    [Header("Ground Check")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    [SerializeField] private float groundCheckRadius = 0.2f;
    [Tooltip("이동 가능한 지면 레이어를 모두 선택하세요 (예: Ground, ClimbGround). 다중 선택 가능.")]
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Wall Check")]
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private Vector2 wallCheckOffset = new Vector2(0f, 0f);
    [Tooltip("이동을 막는 벽 레이어를 선택하세요 (예: Wall).")]
    [SerializeField] private LayerMask wallLayer;
    
    [Header("Ledge Check")]
    [SerializeField] private float ledgeCheckDistance = 1.5f;
    [SerializeField] private float ledgeCheckForwardOffset = 1.0f;
    
    protected override void Evaluate(EnemyBlackboard bb)
    {
        Vector2 pos = transform.position;
        int facing = bb.Movement.facingDirection;
        if (facing == 0) facing = 1;
        
        // 1. 지면 체크
        bb.Movement.isGrounded = Physics2D.OverlapCircle(
            pos + groundCheckOffset, 
            groundCheckRadius, 
            groundLayer
        ) != null;
        
        // 2. 벽 체크 — 진행 방향으로 Raycast (wallLayer 사용)
        Vector2 wallOrigin = pos + wallCheckOffset;
        RaycastHit2D wallHit = Physics2D.Raycast(
            wallOrigin, 
            Vector2.right * facing, 
            wallCheckDistance, 
            wallLayer
        );
        bb.Movement.wallAhead = wallHit.collider != null;
        
        // 3. 낭떠러지 체크 — 진행 방향 앞쪽 아래로 Raycast (groundLayer 사용)
        Vector2 ledgeOrigin = pos + new Vector2(ledgeCheckForwardOffset * facing, 0f);
        RaycastHit2D ledgeHit = Physics2D.Raycast(
            ledgeOrigin, 
            Vector2.down, 
            ledgeCheckDistance, 
            groundLayer
        );
        bb.Movement.ledgeAhead = ledgeHit.collider == null && bb.Movement.isGrounded;
        
        // Debug.Log line removed
    }

    private void Reset()
    {
        // 자동으로 Ground, ClimbGround 레이어 마스크 설정
        groundLayer = LayerMask.GetMask("Ground", "ClimbGround");
        wallLayer = LayerMask.GetMask("Wall");
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector2 pos = transform.position;
        
        // 지면 체크
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pos + groundCheckOffset, groundCheckRadius);
        
        // 벽 체크 (양방향 표시)
        Gizmos.color = Color.red;
        Vector2 wallOrigin = pos + wallCheckOffset;
        Gizmos.DrawLine(wallOrigin, wallOrigin + Vector2.right * wallCheckDistance);
        Gizmos.DrawLine(wallOrigin, wallOrigin + Vector2.left * wallCheckDistance);
        
        // 낭떠러지 체크 (양방향 표시)
        Gizmos.color = Color.yellow;
        Vector2 ledgeRight = pos + new Vector2(ledgeCheckForwardOffset, 0f);
        Vector2 ledgeLeft = pos + new Vector2(-ledgeCheckForwardOffset, 0f);
        Gizmos.DrawLine(ledgeRight, ledgeRight + Vector2.down * ledgeCheckDistance);
        Gizmos.DrawLine(ledgeLeft, ledgeLeft + Vector2.down * ledgeCheckDistance);
    }
#endif
}
