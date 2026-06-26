using UnityEngine;

public class PlayerRestState : PlayerState
{
    private float targetX;
    private float restTimer;
    private const float MIN_REST_TIME = 1.0f; // 1초 대기

    public PlayerRestState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public void SetTargetX(float x)
    {
        targetX = x;
    }

    public override void Enter()
    {
        base.Enter();
        
        // 정중앙 이동 (Y축은 그대로)
        player.RB.position = new Vector2(targetX, player.RB.position.y);
        
        // 이동 멈춤
        player.RB.linearVelocity = new Vector2(0, player.RB.linearVelocity.y);
        
        // 타이머 초기화
        restTimer = 0f;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        
        // 휴식 중에는 중력이나 외부 충돌 외에 플레이어 주도적인 X축 이동 차단
        if (player.RB.bodyType != RigidbodyType2D.Static)
        {
            player.RB.linearVelocity = new Vector2(0, player.RB.linearVelocity.y);
        }

        restTimer += Time.deltaTime;

        // 1초가 지난 후 입력이 들어오면 해제
        if (restTimer >= MIN_REST_TIME)
        {
            // 이동, 점프, 대쉬 입력 확인
            if (Mathf.Abs(player.InputX) > 0.1f || player.JumpInputDown || player.DashInput || player.ButtonAInput || player.ParryInput)
            {
                // 지상에 있으면 Idle로, 공중에 있으면 InAir로 (보통 벤치는 지상이지만)
                if (player.IsGrounded())
                {
                    stateMachine.ChangeState(player.IdleState);
                }
                else
                {
                    stateMachine.ChangeState(player.InAirState);
                }
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
        // 애니메이션은 새 상태 Enter()에서 덮어씌워지므로 추가 작업 불필요
    }
}
