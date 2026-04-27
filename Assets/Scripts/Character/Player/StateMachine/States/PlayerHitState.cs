using UnityEngine;

/// <summary>
/// 피격 시 플레이어 상태 - 완전히 새로운 접근 (Physics-Based with Continuous Detection)
/// </summary>
public class PlayerHitState : PlayerState
{
    private DamageInfo damageInfo;
    private float startTime;
    
    private const float MIN_VELOCITY_THRESHOLD = 0.5f;
    private Vector2 initialKnockbackVelocity;
    
    public PlayerHitState(PlayerController player, PlayerStateMachine stateMachine, Mado.Character.Animation.PlayerAnimType animType) : base(player, stateMachine, animType)
    {
    }
    
    public override void Enter()
    {
        base.Enter();
        startTime = Time.time;
        
        // ✅ 핵심 1: Continuous Collision Detection 활성화 (고속 이동 시 터널링 방지)
        player.RB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // ✅ 핵심 2: 중력만 끄기 (수평 이동에 집중)
        player.RequestGravityOverride(0f);
        
        // ✅ 핵심 3: Velocity로 넉백 (물리 엔진이 충돌 처리)
        // 방향 계산: hitDirection이 있으면 사용, 없으면 위치 차이 (오른쪽이 1, 왼쪽이 -1)
        Vector2 rawDir = damageInfo.hitDirection != Vector2.zero ? damageInfo.hitDirection.normalized : 
                         ((Vector2)player.transform.position - damageInfo.damageSource).normalized;
                         
        float hitSign = Mathf.Sign(rawDir.x);
        if (hitSign == 0) hitSign = player.IsFacingRight ? 1 : -1; // Fallback to facing

        // Source-based Knockback (우선)
        // DamageDealer가 (X, Y)를 보냈으므로, X는 밀려나는 힘, Y는 띄우는 힘
        initialKnockbackVelocity = new Vector2(
            hitSign * damageInfo.knockbackForce.x, 
            damageInfo.knockbackForce.y
        );

        // Fallback removed as per user request to rely on Source
        
        // 즉시 속도 적용
        player.RB.linearVelocity = initialKnockbackVelocity;
        
        // Hit Stop & Shake
        if (player.ActiveFormData != null && player.ActiveFormData.reaction != null)
        {
            player.StartHitStop(player.ActiveFormData.reaction.hitStopDuration);
            var cam = Object.FindFirstObjectByType<PlayerCamera>();
            if (cam != null)
            {
                cam.Shake(player.ActiveFormData.reaction.screenShakePower, 
                          player.ActiveFormData.reaction.screenShakeDuration);
            }
        }
    }
    
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        // 시간 제한 (DamageInfo의 stunDuration 사용)
        float duration = damageInfo.stunDuration > 0 ? damageInfo.stunDuration : 0.3f;

        if (Time.time >= startTime + duration)
        {
            CheckTransition();
            return;
        }
        
        // ✅ 벽 충돌 감지 (속도가 급격히 줄었는지 확인)
        // X축 속도가 거의 0이 되었다면 벽에 박힌 것
        if (Mathf.Abs(player.RB.linearVelocity.x) < 0.1f)
        {
            CheckTransition();
            return;
        }
        
        // 감속 적용 (자연스러운 넉백)
        // Lerp로 서서히 0으로 수렴
        player.RB.linearVelocity = Vector2.Lerp(
            player.RB.linearVelocity, 
            Vector2.zero, 
            5f * Time.fixedDeltaTime
        );
        
        // 속도가 너무 작으면 종료
        if (player.RB.linearVelocity.sqrMagnitude < MIN_VELOCITY_THRESHOLD * MIN_VELOCITY_THRESHOLD)
        {
            CheckTransition();
        }
    }
    
    public override void Exit()
    {
        base.Exit();
        
        // ✅ 원래 collision detection 모드로 복구 (성능 최적화)
        player.RB.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        
        player.ClearGravityOverride();
        player.RB.linearVelocity = Vector2.zero;
    }
    
    private void CheckTransition()
    {
        if (player.IsGrounded()) 
            stateMachine.ChangeState(player.IdleState);
        else 
            stateMachine.ChangeState(player.InAirState);
    }
    
    public void SetDamageInfo(DamageInfo info)
    {
        damageInfo = info;
    }
}
