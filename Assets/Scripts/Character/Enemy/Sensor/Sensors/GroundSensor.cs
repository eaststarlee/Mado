using UnityEngine;

/// <summary>
/// 지면/벽/낭떠러지 감지 센서.
/// Raycast 기반으로 MovementContext 갱신.
/// </summary>
public class GroundSensor : SensorBase
{
    [Header("Layer Config")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Ground Check")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    [SerializeField] private float groundCheckRadius = 0.2f;
    
    [Header("Wall Check")]
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private Vector2 wallCheckOffset = new Vector2(0f, 0f);
    
    [Header("Ledge Check")]
    [SerializeField] private float ledgeCheckDistance = 1.5f;
    [SerializeField] private float ledgeCheckForwardOffset = 1.0f;
    
    protected override void Evaluate(EnemyBlackboard bb)
    {
        Vector2 pos = transform.position;
        int facing = bb.Movement.facingDirection;
        if (facing == 0) facing = 1;
        
        LayerMask worldMask = groundLayer;
            
        // 1. 지면 체크
        Collider2D groundHit = Physics2D.OverlapCircle(pos + groundCheckOffset, groundCheckRadius, worldMask);
        bb.Movement.isGrounded = IsGroundSurface(groundHit);
        
        // 2. 벽 체크 — 진행 방향으로 Raycast
        Vector2 wallOrigin = pos + wallCheckOffset;
        RaycastHit2D wallHit = Physics2D.Raycast(wallOrigin, Vector2.right * facing, wallCheckDistance, worldMask);
        bb.Movement.wallAhead = IsWallSurface(wallHit.collider);
        
        // 3. 낭떠러지 체크 — 진행 방향 앞쪽 아래로 Raycast
        Vector2 ledgeOrigin = pos + new Vector2(ledgeCheckForwardOffset * facing, 0f);
        RaycastHit2D ledgeHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, ledgeCheckDistance, worldMask);
        
        bb.Movement.ledgeAhead = !IsGroundSurface(ledgeHit.collider) && bb.Movement.isGrounded;
    }

    private bool IsGroundSurface(Collider2D col)
    {
        if (col == null) return false;
        SurfaceInfo surface = col.GetComponentInParent<SurfaceInfo>();
        if (surface != null)
        {
            return surface.type == SurfaceType.Ground || surface.type == SurfaceType.ClimbGround;
        }
        return true; // SurfaceInfo가 없으면 기본 지형(밟을 수 있음)으로 간주
    }

    private bool IsWallSurface(Collider2D col)
    {
        if (col == null) return false;
        SurfaceInfo surface = col.GetComponentInParent<SurfaceInfo>();
        if (surface != null)
        {
            return surface.type == SurfaceType.Wall || 
                   surface.type == SurfaceType.BreakableWall || 
                   surface.type == SurfaceType.Devil_BreakableWall;
        }
        return false; // SurfaceInfo가 없으면 벽이 아님
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
