using UnityEngine;

public class PlayerSprintImpactState : PlayerState
{
    private float impactEndTime;

    public PlayerSprintImpactState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        impactEndTime = Time.time + player.ActiveFormData.ability.sprintImpactDuration;

        // Apply knockback force
        int facingDirection = player.IsFacingRight ? 1 : -1;
        Vector2 knockbackForce = player.ActiveFormData.ability.sprintImpactKnockback;
        
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
