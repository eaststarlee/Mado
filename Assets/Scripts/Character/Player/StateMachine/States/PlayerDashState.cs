using UnityEngine;

public class PlayerDashState : PlayerState
{
    private float dashStartTime;

    public PlayerDashState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.OnDash();
        dashStartTime = Time.time;
        player.SetGravityScale(0);
        
        float dashSpeed = player.ActiveFormData.ability.dashSpeed;
        float dashTime = player.ActiveFormData.ability.dashTime;
        float dashDirection = player.IsFacingRight ? 1f : -1f;
        
        FormType stateForm = player.CurrentForm;
        FormType dataForm = player.ActiveFormData.formType;



        player.RB.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
    }

    public override void Exit()
    {
        base.Exit();
        player.SetGravityScale(player.ActiveFormData.gravity.scale);
        
        // 대쉬 종료 시 미끄러지는 현상 방지: 
        // 이동키를 누르고 있으면 즉시 일반 이동 속도로, 아니면 0으로 설정
        float targetSpeedX = player.InputX * player.ActiveFormData.run.maxSpeed;
        player.RB.linearVelocity = new Vector2(targetSpeedX, player.RB.linearVelocity.y * player.ActiveFormData.ability.dashEndYMultiplier);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (Time.time >= dashStartTime + player.ActiveFormData.ability.dashTime)
        {

            // 대시 시간 종료. 다음 상태 결정
            // [SPRINT_DISABLED] 대시 후 스프린트 전환 비활성화
            // if (player.ActiveFormData.ability.canDashToSprint && player.SprintInputHeld && player.IsGrounded())
            // {
            //     stateMachine.ChangeState(player.SprintState);
            // }
            // else
            if (player.IsGrounded())
            {
                if (player.InputX != 0)
                {
                    // 이동 입력이 있으면 자연스럽게 MoveState로 전환
                    stateMachine.ChangeState(player.MoveState);
                }
                else
                {
                    // 입력이 없으면 즉시 정지하여 쫀득한 느낌 부여
                    player.RB.linearVelocity = new Vector2(0, player.RB.linearVelocity.y);
                    stateMachine.ChangeState(player.IdleState);
                }
            }
            else
            {
                stateMachine.ChangeState(player.InAirState);
            }
        }
    }
}
