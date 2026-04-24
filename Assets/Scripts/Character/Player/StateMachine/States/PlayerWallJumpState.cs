using UnityEngine;

public class PlayerWallJumpState : PlayerState
{
    private float wallJumpTimer;
    private bool jumpCut;

    public PlayerWallJumpState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        
        // LedgeDetector 강제 비활성화 (WallJump 직후 렛지 클라임 차단)
        player.LedgeDetector?.SetActive(false);
        
        player.OnWallJump();
        jumpCut = false;
        player.WasLongWallJump = false;
        player.IsSprintJumping = false; // 벽 점프 시 스프린트 점프 상태 해제

        // 이 상태는 Neutral Wall Jump만 처리합니다.
        Vector2 force = player.ActiveFormData.wall.neutralWallJumpForce;
        wallJumpTimer = player.ActiveFormData.wall.neutralWallJumpTime;

        // 마지막 벽의 반대 방향으로 점프 (LastWallDirection 기준)
        int wallDirection = -player.LastWallDirection;
        
        // 선택된 힘을 사용하여 속도를 직접 설정합니다.
        player.RB.linearVelocity = new Vector2(force.x * wallDirection, force.y);
        
        // 점프하는 순간, 플레이어가 벽 반대쪽을 바라보도록 방향을 전환합니다.
        player.CheckDirectionToFace(wallDirection > 0);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 점프 키를 떼면 속도를 줄여 점프를 짧게 만듦 (Jump Cut)
        if (!jumpCut && player.JumpInputUp)
        {
            if(player.RB.linearVelocity.y > 0) // 상승 중일 때만
            {
                player.RB.linearVelocity *= player.ActiveFormData.wall.neutralWallJumpCutMultiplier;
                jumpCut = true;
            }
        }

        // wallJumpTimer가 0이 되면, 플레이어가 다시 공중에서 자유롭게 움직일 수 있도록 InAirState로 전환합니다.
        wallJumpTimer -= Time.deltaTime;
        if (wallJumpTimer < 0)
        {
            stateMachine.ChangeState(player.InAirState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        // 이 상태에서는 플레이어의 입력을 무시하고 정해진 궤적을 그리므로, PhysicsUpdate는 비워둡니다.
    }
}
