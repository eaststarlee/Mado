using UnityEngine;

public class PlayerSprintStopState : PlayerState
{
    public PlayerSprintStopState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        // We can add logic here for when we first enter the state.
    }

    public override void Exit()
    {
        base.Exit();
        // We can add logic here for when we exit the state.
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        
        // No dashing while stopping
        if (player.DashInput)
        {
            // Do nothing, or maybe play a sound/effect
        }

        // If player starts moving again, only transition to MoveState if speed has dropped enough
        // This ensures the "Sprint Stop" slide feel is preserved.
        if (player.InputX != 0 && Mathf.Abs(player.RB.linearVelocity.x) < player.ActiveFormData.run.maxSpeed * 0.8f)
        {
            stateMachine.ChangeState(player.MoveState);
        }
        
        // If speed is low enough, transition to Idle
        if (Mathf.Abs(player.RB.linearVelocity.x) < 0.1f)
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Apply deceleration using MoveTowards for a more controlled "slide" stop
        float currentSpeed = player.RB.linearVelocity.x;
        float accelRate = player.ActiveFormData.ability.sprintStopDeccelAmount;
        float newSpeed = Mathf.MoveTowards(currentSpeed, 0, accelRate * Time.fixedDeltaTime);
        player.RB.linearVelocity = new Vector2(newSpeed, player.RB.linearVelocity.y);
    }
}
