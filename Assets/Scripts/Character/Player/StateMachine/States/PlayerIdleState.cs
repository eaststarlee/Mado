using UnityEngine;

public class PlayerIdleState : PlayerState
{
    // 생성자에서 Idle 애니메이션 해시 전달
    public PlayerIdleState(PlayerController player, PlayerStateMachine stateMachine) 
        : base(player, stateMachine)
    {
    }

    private float lookTimer;
    private const float LOOK_DELAY = 0.1f;

    public override void Enter()
    {
        base.Enter(); // 애니메이션 재생 (SetBaseState)
        lookTimer = 0f;
        
        if (player.RB != null && player.RB.bodyType != RigidbodyType2D.Static)
        {
            player.RB.linearVelocity = new Vector2(0, player.RB.linearVelocity.y); // 즉시 수평 이동 정지
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        
        if (player.InputY > 0.5f)
        {
            lookTimer += Time.deltaTime;
            if (lookTimer > LOOK_DELAY)
            {
                player.animationController?.SetBaseState("LookUp");
            }
        }
        else if (player.InputY < -0.5f)
        {
            lookTimer += Time.deltaTime;
            if (lookTimer > LOOK_DELAY)
            {
                player.animationController?.SetBaseState("LookDown");
            }
        }
        else
        {
            lookTimer = 0f;
            player.animationController?.SetBaseState(AnimStateName);
        }
        
        // DashInput 체크 (대쉬가 모든 스프린트의 시작점)
        if (player.LastPressedDashTime > 0 && player.CanDash())
        {
            player.LastPressedDashTime = 0f;
            stateMachine.ChangeState(player.DashState);
            return;
        }
        

        if (player.LastPressedJumpTime > 0)
        {
            stateMachine.ChangeState(player.InAirState);
        }
        else if (player.InputX != 0)
        {
            stateMachine.ChangeState(player.MoveState);
        }
        else if (!player.IsGrounded())
        {
            stateMachine.ChangeState(player.InAirState);
        }
    }


    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // 1. 반동(Recoil) 중이면 물리 엔진에 전적으로 맡김
        if (player.IsRecoiling)
        {
            return;
        }
        
        // 2. 공격 중 이동 잠금 (반동 끝났는데 잠금이면 정지 - Idle이라 원래 정지지만 확실하게)
        if (player.Combat != null && player.Combat.LockMovement)
        {
            player.RB.linearVelocity = new Vector2(0, player.RB.linearVelocity.y);
            return;
        }

        // 2. 그 외(평소)에는 수동 마찰력 적용 (PM_NoFriction 대응)
        // 미세하게 움직이는 경우 완전히 정지시킴
        if (Mathf.Abs(player.RB.linearVelocity.x) > 0.01f)
        {
            player.RB.linearVelocity = new Vector2(0f, player.RB.linearVelocity.y);
        }
    }
}
