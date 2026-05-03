using UnityEngine;

public class PlayerDashState : PlayerState
{
    private float dashStartTime;

    public PlayerDashState(PlayerController player, PlayerStateMachine stateMachine, Mado.Character.Animation.PlayerAnimType animType) : base(player, stateMachine, animType)
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
