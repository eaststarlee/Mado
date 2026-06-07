#if false // [SPRINT_DISABLED] Sprint 기능 비활성화
using UnityEngine;

public class PlayerSprintTurnState : PlayerState
{
    public PlayerSprintTurnState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // [Fix] 상태 진입 시점의 점프 버퍼 소모 (안전장치)
        player.ConsumeJumpBuffer();
        player.lastSprintTurnTime = Time.time;

        // 2. Visual Flip: Immediately look in the NEW direction (the direction of Input).
        // Using InputX to determine direction.
        if (player.InputX != 0)
        {
             player.CheckDirectionToFace(player.InputX > 0);
        }
    }

    public override void Exit()
    {
        base.Exit();
    }

    // Minimum time to stay in turn state to allow physics to act
    private const float MIN_TURN_DURATION = 0.1f;

    public override void LogicUpdate()
    {
        // [Fix] Turn 동작 중 점프 시도 차단 (입력 소모)
        // 중요: 부모(PlayerSprintState 등)의 점프 판정 로직이 실행되기 전에
        // 버퍼를 소모해야 함. 그렇지 않으면 의도치 않은 점프 발생.
        player.ConsumeJumpBuffer();

        base.LogicUpdate();

        // 1. Stop Input: If C key is released, transition to Stop state.
        if (!player.SprintInputHeld)
        {
            stateMachine.ChangeState(player.SprintStopState);
            return;
        }

        // [핵심 수정] 상태 진입 후 0.1초가 지나지 않았다면, 속도가 0이어도 절대 나가지 않음!
        if (Time.time < player.lastSprintTurnTime + MIN_TURN_DURATION)
        {
            return; 
        }
        // 2. Turn Complete: If velocity reaches 0 (or very close), the slide is done.
        // Transition to SprintState to start accelerating in the new facing direction.
        if (Mathf.Abs(player.RB.linearVelocity.x) < 0.1f)
        {
            stateMachine.ChangeState(player.SprintState);
            return;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Moonwalk Logic:
        // The player is facing the NEW direction, but moving in the OLD direction.
        // We apply deceleration to bring the speed to 0 (Skid).
        
        float currentSpeed = player.RB.linearVelocity.x;
        float accelRate = player.ActiveFormData.ability.sprintTurnDeccelAmount;

        // Use MoveTowards for linear, consistent deceleration (sliding friction).
        // This avoids the issue of AddForce (exponential) or instability.
        float newSpeed = Mathf.MoveTowards(currentSpeed, 0, accelRate * Time.fixedDeltaTime);
        
        player.RB.linearVelocity = new Vector2(newSpeed, player.RB.linearVelocity.y);
    }
}
#endif // [SPRINT_DISABLED]
