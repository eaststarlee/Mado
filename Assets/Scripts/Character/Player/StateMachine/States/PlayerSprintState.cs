#if false // [SPRINT_DISABLED] Sprint 기능 비활성화
using UnityEngine;

public class PlayerSprintState : PlayerState
{
    private float lastDirection;
    private float sprintStartTime; // 스프린트 시작 시간

    public PlayerSprintState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        lastDirection = player.IsFacingRight ? 1f : -1f;
        sprintStartTime = Time.time; // 스프린트 시작 시간 기록
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // C 키(스프린트)를 떼면 상태 전환
        if (!player.SprintInputHeld)
        {
            stateMachine.ChangeState(player.SprintStopState);
            return; // 상태가 변경되었으므로 즉시 종료
        }

        // [LedgeClimb] Sprint 중에는 전진 상태이므로, 앞쪽에 턱이 감지되면 방향키(InputX) 입력 없이도 전환
        Vector2? climbTarget = player.LedgeDetector.ScanLedgeTarget();
        if (climbTarget.HasValue)
        {
            player.LedgeClimbState.SetTarget(climbTarget.Value);
            stateMachine.ChangeState(player.LedgeClimbState);
            return;
        }



        // 방향 전환 감지 (Hybrid Sprint Turn)
        // 1. 방향 입력이 반대일 때
        if (player.InputX * (player.IsFacingRight ? 1 : -1) < 0)
        {
            float speedThreshold = player.ActiveFormData.run.maxSpeed * 0.5f; // 최고 속도의 50%
            float currentAbsSpeed = Mathf.Abs(player.RB.linearVelocity.x);

            // 2. 속도가 충분히 빠르면 -> 관성 턴 (슬라이드)
            if (currentAbsSpeed > speedThreshold)
            {
                stateMachine.ChangeState(player.SprintTurnState);
                return;
            }
            // 3. 속도가 느리면 -> 즉시 턴 (반응성 위주)
            else
            {
                player.CheckDirectionToFace(player.InputX > 0);
            }
        }



        if (player.InputX != 0)
        {
            lastDirection = player.InputX > 0 ? 1f : -1f;
        }

        // 점프 또는 낙하 조건 확인 (착지 쿨타임 및 선딜레이 적용)
        if (player.LastPressedJumpTime > 0 && player.timeSinceLanded >= player.ActiveFormData.jump.sprintJumpLandCooldown)
        {
            // [New] Sprint Turn 이후 점프 제한 조건 체크
            float timeSinceTurn = Time.time - player.lastSprintTurnTime;
            float currentAbsSpeed = Mathf.Abs(player.RB.linearVelocity.x);
            float speedThreshold = player.ActiveFormData.ability.sprintSpeed * player.ActiveFormData.ability.sprintTurnJumpSpeedThreshold;

            if (timeSinceTurn >= player.ActiveFormData.ability.sprintTurnJumpLockDuration && currentAbsSpeed >= speedThreshold)
            {
                player.SprintJumpVelocityX = player.RB.linearVelocity.x; // 점프 직전 속도를 미리 저장
                stateMachine.ChangeState(player.SprintJumpPrepareState); // InAirState 대신 PrepareState로 전환
            }
        }
        else if (!player.IsGrounded())
        {
            // 낭떠러지에서 떨어질 때: 수평 속도를 유지하기 위해 저장
            player.SprintJumpVelocityX = player.RB.linearVelocity.x;
            player.IsSprintJumping = true; // 스프린트 낙하 플래그 설정
            stateMachine.ChangeState(player.InAirState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // [Fix] 반동(Recoil) 중이면 물리 엔진에 맡기고 스프린트 강제 이동을 스킵하여 X 넉백 보존
        if (player.IsRecoiling)
        {
            return;
        }

        float currentSpeed = player.RB.linearVelocity.x;
        float inputDirection = (player.InputX != 0) ? player.InputX : lastDirection;
        float targetSpeed = inputDirection * player.ActiveFormData.ability.sprintSpeed;
        float accelRate = player.ActiveFormData.run.accelAmount;

        // Sprint는 항상 가속만 함. 감속은 SprintTurnState에서 처리.
        if (targetSpeed > 0)
        {
            float newSpeed = Mathf.Min(targetSpeed, currentSpeed + accelRate * Time.fixedDeltaTime);
            player.RB.linearVelocity = new Vector2(newSpeed, player.RB.linearVelocity.y);
        }
        else
        {
            float newSpeed = Mathf.Max(targetSpeed, currentSpeed - accelRate * Time.fixedDeltaTime);
            player.RB.linearVelocity = new Vector2(newSpeed, player.RB.linearVelocity.y);
        }
    }
}
#endif // [SPRINT_DISABLED]
