using UnityEngine;

// 모든 플레이어 상태의 기반이 되는 추상 클래스
public abstract class PlayerState
{
    // 상태가 참조할 플레이어 컨트롤러와 상태 머신
    protected PlayerController player;
    protected PlayerStateMachine stateMachine;
    
    // 클래스 이름에서 Player와 State를 제거하여 애니메이션 상태 이름 자동 생성 (예: PlayerIdleState -> Idle)
    protected virtual string AnimStateName => GetType().Name.Replace("Player", "").Replace("State", "");
    
    // 생성자
    protected PlayerState(PlayerController player, PlayerStateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }

    // 상태에 진입할 때 한 번 호출되는 함수
    public virtual void Enter() 
    {
        if (player.animationController != null)
        {
            player.animationController.SetBaseState(AnimStateName);
            // 상태 전환 시 트랜지션 애니메이션(Turn, RunStop 등)은 즉시 정리
            player.animationController.ClearAction(PlayerAnimationController.AnimPriority.Transition);
        }
    }

    // 상태를 빠져나갈 때 한 번 호출되는 함수
    public virtual void Exit() { }

    // 매 프레임 호출될 로직 업데이트 (MonoBehaviour의 Update)
    public virtual void LogicUpdate() { }

    // 매 물리 프레임 호출될 물리 업데이트 (MonoBehaviour의 FixedUpdate)
    public virtual void PhysicsUpdate() { }

    /// <summary>
    /// 공중 좌우 이동 처리 (순수 함수 - 상태 전환/체크 금지)
    /// AirState와 GlideState에서 공통으로 사용
    /// </summary>
    /// <param name="speedMultiplier">속도 배율 (활공 시 감속 등)</param>
    /// <param name="accelMultiplier">가속도 배율 (방향 전환 빠릿함 유지)</param>
    protected void HandleAirMove(float speedMultiplier = 1f, float accelMultiplier = 1f)
    {
        // 목표 속도 계산 (배율 적용)
        float targetSpeed = player.InputX * player.ActiveFormData.run.maxSpeed * speedMultiplier;
        
        // 지면 옆면 충돌 시 정지 (기존 로직 유지)
        if (player.IsTouchingGroundOnSide() && player.InputX != 0 && (player.IsFacingRight == (player.InputX > 0)))
        {
            // Static일 때는 속도를 수정할 수 없으므로 체크 (룸 전환 시 프리즈 대응)
            if (player.RB.bodyType != RigidbodyType2D.Static)
                player.RB.linearVelocity = new Vector2(0, player.RB.linearVelocity.y);
        }
        else
        {
            // 일반적인 공중 이동
            if (player.RB.bodyType != RigidbodyType2D.Static)
                player.RB.linearVelocity = new Vector2(targetSpeed, player.RB.linearVelocity.y);
        }
    }
}
