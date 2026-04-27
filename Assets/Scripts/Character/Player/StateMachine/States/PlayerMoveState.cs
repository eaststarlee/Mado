using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(PlayerController player, PlayerStateMachine stateMachine, Mado.Character.Animation.PlayerAnimType animType) : base(player, stateMachine, animType)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.IsSprintJumping = false; // 스프린트 점프 상태 초기화
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 변신 입력 체크 (↑ + A)
        if (stateMachine.CurrentState != player.TransformState 
            && player.InputY > 0.5f // [Modified] Use InputY
            && player.ButtonAInput) // [Modified] Use ButtonAInput
        {
            FormType nextForm = player.CurrentForm == FormType.Normal 
                ? FormType.Devil 
                : FormType.Normal;
            
            player.TransformState.SetTransform(nextForm, this);
            stateMachine.ChangeState(player.TransformState);
            return;
        }

        // [LedgeClimb] Walk 중 벽 방향으로 이동하다 턱이 감지되면 LedgeClimb 전환
        bool inputTowardsWall = (player.InputX > 0 && player.IsFacingRight)
                             || (player.InputX < 0 && !player.IsFacingRight);
        if (inputTowardsWall)
        {
            Vector2? climbTarget = player.LedgeDetector.ScanLedgeTarget();
            if (climbTarget.HasValue)
            {
                player.LedgeClimbState.SetTarget(climbTarget.Value);
                stateMachine.ChangeState(player.LedgeClimbState);
                return;
            }
        }

        if (player.LastPressedDashTime > 0 && player.CanDash())
        {
            player.LastPressedDashTime = 0f;
            stateMachine.ChangeState(player.DashState);
        }
        else if (player.LastPressedJumpTime > 0)
        {
            stateMachine.ChangeState(player.InAirState);
        }
        else if (player.InputX == 0)
        {
            stateMachine.ChangeState(player.IdleState);
        }
        else if (!player.IsGrounded())
        {
            stateMachine.ChangeState(player.InAirState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // 1. 반동(Recoil) 중이면 물리 엔진에 전적으로 맡김 (이동 로직 무시)
        if (player.IsRecoiling)
        {
            return;
        }

        // 2. 공격 중 이동 잠금 (반동이 끝났는데 아직 잠금 상태라면 정지)
        if (player.Combat != null && player.Combat.LockMovement)
        {
            player.RB.linearVelocity = new Vector2(0, player.RB.linearVelocity.y);
            return;
        }

        float targetSpeed = player.InputX * player.ActiveFormData.run.maxSpeed;
        float currentSpeed = player.RB.linearVelocity.x;
        float newSpeed = currentSpeed;
        float accelRate;

        // 간단하고 빠릿한 이동 로직 (할로우 나이트 스타일)
        // 방향 전환 시 감속 없이 즉시 가속 적용
        accelRate = player.ActiveFormData.run.accelAmount;
        
        // 목표 속도로 부드럽게 가속 (Mathf.MoveTowards 사용)
        // 가속도는 runAccelAmount를 사용
        newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.fixedDeltaTime);

        player.RB.linearVelocity = new Vector2(newSpeed, player.RB.linearVelocity.y);
    }
}
