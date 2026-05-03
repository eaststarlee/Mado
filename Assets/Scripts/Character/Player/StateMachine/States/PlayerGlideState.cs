using UnityEngine;

public class PlayerGlideState : PlayerState
{
    // SmoothDamp 전용 ref 변수 (상태 전환 시 속도 꼬임 방지)
    private float glideVelocityRef;

    public PlayerGlideState(PlayerController player, PlayerStateMachine stateMachine, Mado.Character.Animation.PlayerAnimType animType) : base(player, stateMachine, animType)
    {
    }

    public override void Enter()
    {
        base.Enter();
        
        player.BeginGlide(); // Controller에 요청 (이벤트 발생 포함)
        
        // 중력 충돌 방지 (Unity 중력과 SmoothDamp가 싸우지 않도록)
        player.RB.gravityScale = 0f;
        
        // Ref 변수 초기화
        glideVelocityRef = 0f;
    }

    public override void Exit()
    {
        base.Exit();
        
        // Fail-safe 먼저 호출 (상태 종료 시점 명확화)
        player.ForceEndGlide();
        
        // 중력 복구
        player.RB.gravityScale = player.ActiveFormData.gravity.scale;

        // 활공 종료 시 점프 버퍼 비우기 (의도치 않은 자동 점프 방지)
        player.LastPressedJumpTime = 0;
        
        // (선택) Y속도 클램프 - 툭 떨어지는 느낌 방지
        player.RB.linearVelocity = new Vector2(
            player.RB.linearVelocity.x, 
            Mathf.Min(player.RB.linearVelocity.y, 0f)
        );
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        
        // 1. 더블 점프 (활공보다 우선!)
        // 활공 중이라도 더블 점프 기회가 있다면 즉시 점프로 전환
        if (player.JumpInputDown && player.CanDoubleJump)
        {
            player.CanDoubleJump = false;
            player.LastPressedJumpTime = 0;
            stateMachine.ChangeState(player.InAirState); // DoubleJump는 InAirState에서 처리
            return;
        }
        
        // 2. Dash transition
        if (player.DashInput && player.CanDash())
        {
            stateMachine.ChangeState(player.DashState);
            return;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        // [Fix] 반동(Recoil) 중이면 물리 엔진에 맡기고 활공 속도 제어를 스킵하여 X 넉백 보존
        if (player.IsRecoiling)
        {
            return;
        }
        
        // 1. Y축 속도 제어 (부드러운 하강)
        float newY = Mathf.SmoothDamp(
            player.RB.linearVelocity.y,
            -player.ActiveFormData.ability.glideFallSpeed,
            ref glideVelocityRef,
            player.ActiveFormData.ability.glideSmoothTime
        );
        player.RB.linearVelocity = new Vector2(player.RB.linearVelocity.x, newY);
        
        // 2. X축 이동 (감속 적용)
        HandleAirMove(
            player.ActiveFormData.ability.glideHorizontalMultiplier,
            player.ActiveFormData.ability.glideAccelerationMultiplier
        );
        
        // 3. 상태 전환 (우선순위: 착지 > 벽타기 > 활공 해제)
        
        // 착지
        if (player.IsGrounded() && player.RB.linearVelocity.y < 0.01f)
        {
            if (player.InputX != 0)
            {
                stateMachine.ChangeState(player.MoveState);
            }
            else
            {
                stateMachine.ChangeState(player.IdleState);
            }
            return;
        }
        
        // 벽 타기: 벽에 접촉 중이고 벽 쪽으로 입력 중일 때만
        if (player.IsTouchingWall())
        {
            // 벽 쪽으로 입력하고 있는지 확인
            bool isInputTowardsWall = (player.InputX > 0 && player.IsFacingRight) || 
                                     (player.InputX < 0 && !player.IsFacingRight);
            
            if (isInputTowardsWall)
            {
                stateMachine.ChangeState(player.WallSlideState);
                return;
            }
        }
        
        // Z키를 놓으면 일반 낙하로 복귀
        if (!player.JumpInput)
        {
            stateMachine.ChangeState(player.InAirState);
            return;
        }
    }
}
