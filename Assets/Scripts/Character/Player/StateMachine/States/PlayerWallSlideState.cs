using UnityEngine;

public class PlayerWallSlideState : PlayerState
{
    private float wallStickTimer;

    public PlayerWallSlideState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.IsSprintJumping = false; // 스프린트 점프 상태 초기화
        
        // Enter 단계에서 즉시 벽 방향으로 밀착시킴
        // Vector2.zero로 하면 PhysicsUpdate 전에 LogicUpdate의 IsTouchingWall 체크에서 실패할 수 있음
        int direction = player.IsFacingRight ? 1 : -1;
        player.RB.linearVelocity = new Vector2(direction * 0.5f, 0);
        
        player.SetGravityScale(0);
        wallStickTimer = player.ActiveFormData.wall.stickTime;
    }

    public override void Exit()
    {
        base.Exit();
        player.SetGravityScale(player.ActiveFormData.gravity.scale);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        wallStickTimer -= Time.deltaTime;
        
        // 벽에서 떨어졌는지 최우선으로 확인
        if (!player.IsTouchingWall())
        {
            stateMachine.ChangeState(player.InAirState);
            return;
        }
        // ========== 렛지 클라임 체크 ==========
        // 중요: 이미 LedgeClimbState에 있다면 체크하지 않음 (한 번 시작하면 끝까지 완료)
        if (stateMachine.CurrentState != player.LedgeClimbState)
        {
            // ===== 렛지 클라임 (Snap & Tween) =====
            // 위쪽(↑) 입력 시 스캔 시도
            float inputY = player.InputY;
            
            if (inputY > 0)
            {
                Vector2? climbTarget = player.LedgeDetector.ScanLedgeTarget();
                
                if (climbTarget.HasValue)
                {
                    player.LedgeClimbState.SetTarget(climbTarget.Value);
                    stateMachine.ChangeState(player.LedgeClimbState);
                    return;
                }
            }
        }

        // [개선] 벽점프 시도 (Try 패턴) - 성공 시 즉시 return
        if (player.TryWallJump())
        {
            return; // 벽점프 성공 시 아래 로직 실행 안 함
        }
        
        // 벽 반대방향 키 입력 시 떨어짐 (벽 기준 판정)
        // 단, 점프 의도가 있으면 (HasBufferedWallJump) 떨어지지 않음 (실크송 스타일)
        if (player.InputX != 0 && Mathf.Sign(player.InputX) != player.WallDirection)
        {
            // 점프 버퍼가 있으면 벽점프 우선 (떨어지지 않음)
            if (player.HasBufferedWallJump())
            {
                // 점프 의도가 명확하므로 return (다음 프레임에 TryWallJump 재시도)
                return;
            }
            
            // 점프 의도 없이 벽 반대로 입력 → 떨어짐
            stateMachine.ChangeState(player.InAirState);
        }
        else if (player.IsGrounded())
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (wallStickTimer <= 0)
        {
            // wallStickTimer가 끝나면 가속도 모델을 사용하여 슬라이딩 시작
            float targetSpeed = -player.ActiveFormData.wall.slideSpeed;
            float newYVelocity = Mathf.MoveTowards(player.RB.linearVelocity.y,
                                                 targetSpeed,
                                                 player.ActiveFormData.wall.slideAccel * Time.fixedDeltaTime);
            
            // 벽 방향으로 계속 밀어줘서 접촉 유지
            int direction = player.IsFacingRight ? 1 : -1;
            player.RB.linearVelocity = new Vector2(direction * 0.5f, newYVelocity);
        }
        else
        {
             // Timer가 아직 안 끝났을 때도 접촉 유지 필요
             int direction = player.IsFacingRight ? 1 : -1;
             player.RB.linearVelocity = new Vector2(direction * 0.5f, 0);
        }
    }
}
