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
        float dashDirection = player.IsFacingRight ? 1f : -1f;
        player.RB.linearVelocity = new Vector2(dashDirection * player.ActiveFormData.ability.dashSpeed, 0f);
    }

    public override void Exit()
    {
        base.Exit();
        player.SetGravityScale(player.ActiveFormData.gravity.scale);
        player.RB.linearVelocity = new Vector2(player.RB.linearVelocity.x, player.RB.linearVelocity.y * player.ActiveFormData.ability.dashEndYMultiplier);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (Time.time >= dashStartTime + player.ActiveFormData.ability.dashTime)
        {
            // 대시 시간 종료. 다음 상태 결정
            if (player.ActiveFormData.ability.canDashToSprint && player.SprintInputHeld && player.IsGrounded())
            {
                stateMachine.ChangeState(player.SprintState);
            }
            else if (player.IsGrounded())
            {
                player.RB.linearVelocity = new Vector2(0, player.RB.linearVelocity.y); // 대시 종료 시 지상에서 즉시 정지
                stateMachine.ChangeState(player.IdleState);
            }
            else
            {
                stateMachine.ChangeState(player.InAirState);
            }
        }
    }
}
