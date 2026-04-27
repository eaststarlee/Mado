using UnityEngine;

public class PlayerParryState : PlayerState
{
    private float timer;
    private bool hasSucceeded;
    private CharacterFormData.ParryData parryData;

    // 패링 시 단계 관리
    private enum ParryPhase { Startup, Active, Recovery, Finished }
    private ParryPhase currentPhase;

    public bool IsActiveWindow => currentPhase == ParryPhase.Active;

    public PlayerParryState(PlayerController player, PlayerStateMachine stateMachine, Mado.Character.Animation.PlayerAnimType animType = 0)
        : base(player, stateMachine, animType)
    {
    }

    public override void Enter()
    {
        base.Enter();
        timer = 0f;
        hasSucceeded = false;
        currentPhase = ParryPhase.Startup;

        parryData = player.ActiveFormData.parry;

        // 공중에서 패링을 하더라도 물리 상태를 덮어쓰지 않습니다. 자연스럽게 점프/낙하를 이어가도록 둡니다.
        // 강제 정지를 원할 경우 지상에서만 X를 멈출 수도 있습니다. 이번에는 게임 피드백을 위해 수평 이동만 잠급니다.
        // [Fix] StartRecoil로 인한 넉백(IsRecoiling) 중이 아닐 때만 속도 덮어쓰기
        if (player.IsGrounded() && !player.IsRecoiling)
        {
            player.RB.linearVelocity = new Vector2(0f, player.RB.linearVelocity.y);
        }
    }

    public override void Exit()
    {
        base.Exit();
        // 쿨다운 시작점 기록
        player.LastParryEndTime = Time.time;
        
        // 중력 스케일 복구 (공중 패링 중 변경되었을 수 있음)
        player.SetGravityScale(player.ActiveFormData.gravity.scale);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        timer += Time.deltaTime;

        // --- 상 변환 로직 ---
        if (currentPhase == ParryPhase.Startup)
        {
            if (timer >= parryData.startupTime)
            {
                currentPhase = ParryPhase.Active;
            }
        }
        else if (currentPhase == ParryPhase.Active)
        {
            if (timer >= parryData.startupTime + parryData.activeTime)
            {
                currentPhase = ParryPhase.Recovery;
            }
        }
        else if (currentPhase == ParryPhase.Recovery)
        {
            if (timer >= parryData.startupTime + parryData.activeTime + parryData.recoveryTime)
            {
                currentPhase = ParryPhase.Finished;
            }
        }

        // --- 상태 종료 (성공 혹은 자연 완료) ---
        if (hasSucceeded && !player.IsRecoiling)
        {
            // 성공 시 딜레이 없이 즉시 다음 상태로 전환
            // 추가 방어: 넉백 중이면 상태가 즉시 풀리지 않게 대기!
            TransitionOut();
        }
        else if (currentPhase == ParryPhase.Finished)
        {
            // 실패 후 회수기간 종료되어 자연스레 전이
            TransitionOut();
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        // [Fix] 넉백 루틴(RecoilRoutine)이 제어권을 가져간 경우 물리 조작을 건너뛰어 허용합니다.
        if (player.IsRecoiling) return;
        
        // 지상과 공중의 패링 물리 조작 분리
        if (player.IsGrounded())
        {
            // 지상에서는 패링 시 제자리에 멈추도록 X축 일시 정지
            player.RB.linearVelocity = new Vector2(0f, player.RB.linearVelocity.y);
            player.SetGravityScale(player.ActiveFormData.gravity.scale);
        }
        else
        {
            // 공중에서는 패링 시에도 좌우 조작(InputX)을 유지할 수 있도록 허용
            // 일반 공중 이동과 동일하게 runSpeed를 곱해 속도를 적용
            float targetSpeed = player.InputX * player.ActiveFormData.run.maxSpeed;
            player.RB.linearVelocity = new Vector2(targetSpeed, player.RB.linearVelocity.y);
            
            // PlayerInAirState와 동일한 중력 배율 및 낙하 속도 제한 적용
            if (Mathf.Abs(player.RB.linearVelocity.y) < player.ActiveFormData.jump.jumpHangTimeThreshold)
            {
                player.SetGravityScale(player.ActiveFormData.gravity.scale * player.ActiveFormData.jump.jumpHangGravityMult);
            }
            else if (player.RB.linearVelocity.y > 0)
            {
                player.SetGravityScale(player.ActiveFormData.gravity.scale);
            }
            else
            {
                player.SetGravityScale(player.ActiveFormData.gravity.scale * player.ActiveFormData.gravity.fallGravityMult);
            }
            
            if (player.RB.linearVelocity.y < -player.ActiveFormData.gravity.maxFallSpeed)
            {
                player.RB.linearVelocity = new Vector2(player.RB.linearVelocity.x, -player.ActiveFormData.gravity.maxFallSpeed);
            }
        }
    }

    public void SetSuccess()
    {
        hasSucceeded = true;
    }

    private void TransitionOut()
    {
        // 공중이면 InAirState, 지상이면 IdleState로 트랜지션
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
