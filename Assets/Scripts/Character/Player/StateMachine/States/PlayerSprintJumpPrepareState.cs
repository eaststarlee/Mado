#if false // [SPRINT_DISABLED] Sprint 기능 비활성화
using UnityEngine;

public class PlayerSprintJumpPrepareState : PlayerState
{
    private float startTime;

    public PlayerSprintJumpPrepareState(PlayerController player, PlayerStateMachine stateMachine, Mado.Character.Animation.PlayerAnimType animType) : base(player, stateMachine, animType)
    {
    }

    public override void Enter()
    {
        base.Enter();
        startTime = Time.time;
        // Optional: Play a "prepare to jump" animation here
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // After the prepare time has passed, transition to the InAirState to perform the jump.
        if (Time.time >= startTime + player.ActiveFormData.jump.sprintJumpPrepareTime)
        {
            stateMachine.ChangeState(player.InAirState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        // 플레이어가 기존 속도를 유지하며 미끄러지도록 의도적으로 비워둡니다.
        // Intentionally left blank to allow the player to slide with existing momentum.
    }
}
#endif // [SPRINT_DISABLED]
