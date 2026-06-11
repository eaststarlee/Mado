using UnityEngine;

/// <summary>
/// 렛지 클라임 상태 (2단계 + Snap)
/// Phase 1: 수직 상승 (벽 옆에서 벽 끝 위까지)
/// Phase 2: 벽 위로 넘어가기 + 착지점 Snap
/// </summary>
public class PlayerLedgeClimbState : PlayerState
{
    private enum ClimbPhase
    {
        VerticalUp,    // Phase 1: 수직 상승
        OverLedge      // Phase 2: 벽 위로 + Snap
    }
    
    private Vector2 startPos;
    private Vector2 targetPos;     // 최종 착지점
    private float cornerY;         // 벽 끝 높이 (다리 안 겹치도록 여유 추가)
    private float ledgeSurfaceX;   // 벽 표면 X
    private ClimbPhase currentPhase;
    private int facingDir;
    
    // 안전 장치
    private float climbTimer = 0f;
    private const float MAX_CLIMB_TIME = 3f;
    
    // 물리 복구용
    private float originalGravityScale;
    
    // BoxCollider 캐싱
    private BoxCollider2D playerCollider;

    public PlayerLedgeClimbState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    /// <summary>
    /// 외부에서 목표 지점 설정 (L-Shape Scan 결과)
    /// </summary>
    public void SetTarget(Vector2 target)
    {
        this.targetPos = target;
    }
    
    /// <summary>
    /// 벽 표면 X 설정 (LedgeDetector에서 제공)
    /// </summary>
    public void SetLedgeSurfaceX(float surfaceX)
    {
        this.ledgeSurfaceX = surfaceX;
    }

    public override void Enter()
    {
        base.Enter();

        // 1. 물리 완전 차단
        originalGravityScale = player.RB.gravityScale;
        player.SetGravityScale(0f);
        player.RB.bodyType = RigidbodyType2D.Kinematic;
        player.RB.linearVelocity = Vector2.zero;
        
        facingDir = player.IsFacingRight ? 1 : -1;
        playerCollider = player.GetComponent<BoxCollider2D>();
        
        startPos = player.RB.position;
        climbTimer = 0f;
        
        // ========================================================
        // [핵심] cornerY = Target Y + 약간의 여유
        // 다리가 벽과 겹치지 않도록 캐릭터 높이의 일부 추가
        // ========================================================
        cornerY = targetPos.y + (playerCollider.size.y * 0.2f);
        
        // 벽 표면 X 기본값 설정
        if (ledgeSurfaceX == 0)
        {
            ledgeSurfaceX = startPos.x + (facingDir * (playerCollider.size.x * 0.5f + 0.05f));
        }
        
        // Phase 시작 결정
        if (targetPos.y > startPos.y)
        {
            currentPhase = ClimbPhase.VerticalUp;
            Debug.Log($"[LedgeClimb] Start: Current={startPos}, Target={targetPos}, CornerY={cornerY}");
        }
        else
        {
            currentPhase = ClimbPhase.OverLedge;
        }
        
        // LedgeDetector 비활성화
        player.LedgeDetector?.SetActive(false);
    }

    public override void Exit()
    {
        base.Exit();

        // [필수] 물리 완전 복구
        player.RB.bodyType = RigidbodyType2D.Dynamic;
        player.SetGravityScale(originalGravityScale);
        player.RB.linearVelocity = Vector2.zero;
        
        // 초기화
        ledgeSurfaceX = 0;

        player.LedgeDetector?.SetActive(true);
        player.StartLedgeFailCooldown(0.2f);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 타임아웃 안전 장치
        climbTimer += Time.deltaTime;
        if (climbTimer > MAX_CLIMB_TIME)
        {
            AbortClimb();
            return;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        float speed = player.ActiveFormData.wall.ledgeClimbSpeed;
        float dt = Time.fixedDeltaTime;
        Vector2 currentPos = player.RB.position;

        switch (currentPhase)
        {
            // ========================================================
            // Phase 1: 수직 상승 (벽 옆에서 cornerY까지)
            // ========================================================
            case ClimbPhase.VerticalUp:
            {
                // 천장 체크
                if (!CheckClearance())
                {
                    AbortClimb();
                    return;
                }

                // 수직 이동
                Vector2 nextPos = currentPos;
                nextPos.x = startPos.x; // X 고정
                nextPos.y += speed * dt;

                // cornerY 도달 체크
                if (nextPos.y >= cornerY)
                {
                    nextPos.y = cornerY;
                    player.RB.MovePosition(nextPos);
                    
                    currentPhase = ClimbPhase.OverLedge;
                    return;
                }

                player.RB.MovePosition(nextPos);
                break;
            }

            // ========================================================
            // Phase 2: 벽 위로 넘어가기 + 착지점 Snap
            // ledgeSurfaceX에 도달하면 바로 targetPos로 Snap
            // ========================================================
            case ClimbPhase.OverLedge:
            {
                Vector2 nextPos = currentPos;
                nextPos.y = cornerY; // Y 고정
                
                // 벽 표면 위로 수평 이동
                nextPos.x += speed * dt * facingDir;

                // 벽 표면 X 도달 체크
                bool passedLedge = (facingDir > 0 && nextPos.x >= ledgeSurfaceX) ||
                                   (facingDir < 0 && nextPos.x <= ledgeSurfaceX);

                if (passedLedge)
                {
                    // 바로 최종 목표로 Snap! (수평 이동 제거)
                    player.RB.MovePosition(targetPos);
                    FinishClimb();
                    return;
                }

                player.RB.MovePosition(nextPos);
                break;
            }
        }
    }

    /// <summary>
    /// 천장 체크
    /// </summary>
    private bool CheckClearance()
    {
        Vector2 checkOrigin = player.RB.position + Vector2.up * (playerCollider.size.y * 0.5f);
        Vector2 checkSize = new Vector2(playerCollider.size.x * 0.8f, 0.2f);

        LayerMask ceilingMask = player.GroundLayer;

        RaycastHit2D ceilingHit = Physics2D.BoxCast(
            checkOrigin,
            checkSize,
            0f,
            Vector2.up,
            0.3f,
            ceilingMask
        );

        return ceilingHit.collider == null;
    }

    /// <summary>
    /// 렛지 클라임 중단
    /// </summary>
    private void AbortClimb()
    {
        player.RB.position = new Vector2(startPos.x, player.RB.position.y);
        stateMachine.ChangeState(player.InAirState);
    }

    /// <summary>
    /// 렛지 클라임 완료
    /// </summary>
    private void FinishClimb()
    {
        stateMachine.ChangeState(player.IdleState);
    }
}
