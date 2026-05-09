#if false // [SPRINT_DISABLED] Sprint 기능 비활성화
using UnityEngine;

public class PlayerSprintImpactState : PlayerState
{
    private float impactEndTime;

    public PlayerSprintImpactState(PlayerController player, PlayerStateMachine stateMachine, Mado.Character.Animation.PlayerAnimType animType) : base(player, stateMachine, animType)
    {
    }

    public override void Enter()
    {
        base.Enter();

        impactEndTime = Time.time + 0.2f; // 임시 고정값 (미사용 상태)

        // Apply knockback force
        int facingDirection = player.IsFacingRight ? 1 : -1;
        Vector2 knockbackForce = Vector2.zero; // 임시 고정값 (미사용 상태)
        
        player.RB.linearVelocity = new Vector2(-facingDirection * knockbackForce.x, knockbackForce.y);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // After the stun duration, transition to InAirState
        if (Time.time >= impactEndTime)
        {
            stateMachine.ChangeState(player.InAirState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        // Player has no control during this state
    }
}
#endif // [SPRINT_DISABLED]
