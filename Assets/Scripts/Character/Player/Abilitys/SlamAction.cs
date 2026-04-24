using UnityEngine;

/// <summary>
/// 악마폼 Slam 공격 (Desolate Dive 스타일)
/// 4단계 Action Lifecycle: Anticipation → Descent → Impact → Recovery
/// </summary>
public class SlamAction : ISpecialAction
{
    public enum Phase { None, Anticipation, Descent, Impact, Recovery }
    
    public Phase CurrentPhase { get; private set; } = Phase.None;
    
    private SlamAttackData config;
    private PlayerController player;
    private ActionHandle handle;
    private float phaseTimer;
    
    public SlamAction(PlayerController player, SlamAttackData config)
    {
        this.player = player;
        this.config = config;
    }
    
    #region ISpecialAction Implementation
    
    public void Begin(ActionHandle handle)
    {
        this.handle = handle;
        
        // Phase 1: Anticipation (공중 정지)
        EnterAnticipation();
        
        // 착지 이벤트 구독 (Handle로 자동 해제)
        player.OnGroundedConfirmed += OnGroundedConfirmed;
        handle.Subscribe(
            null,  // 구독은 위에서 직접 처리
            () => player.OnGroundedConfirmed -= OnGroundedConfirmed
        );
    }
    public void Cancel()
    {
        // 모든 제약 해제
        Cleanup();
        CurrentPhase = Phase.None;
    }
    
    // Slam 도중에는 다른 입력 완전 차단 (Recovery 포함)
    public bool LocksInput => CurrentPhase != Phase.None;
    
    #endregion
    
    #region Phase Transitions
    
    private void EnterAnticipation()
    {
        CurrentPhase = Phase.Anticipation;
        phaseTimer = config.slamAnticipationDuration;
        
        // 공중 정지: 중력 0, 속도 0, 이동 잠금
        player.Combat.LockMovement = true;
        player.RequestGravityOverride(0f);
        player.RB.linearVelocity = Vector2.zero;
        
        Debug.Log("[SlamAction] Phase: Anticipation");
    }
    
    private void EnterDescent()
    {
        CurrentPhase = Phase.Descent;
        
        // 고정 속도 하강 (중력 무시)
        player.RequestGravityOverride(0f);  // 중력 0으로 설정
        
        // [Invincibility] 하강 무적 설정 (무제한)
        player.Health.SetInvincible(PlayerHealth.InvincibilitySource.SlamDescent, -1f);
        
        Debug.Log("[SlamAction] Phase: Descent");
    }
    
    public void Update(float deltaTime)
    {
        if (CurrentPhase == Phase.None) return;
        
        phaseTimer -= deltaTime;
        
        switch (CurrentPhase)
        {
            case Phase.Anticipation:
                // 공중 정지 유지
                player.RB.linearVelocity = Vector2.zero;
                if (phaseTimer <= 0)
                    EnterDescent();
                break;
                
            case Phase.Descent:
                // 고정 속도로 하강
                player.RB.linearVelocity = new Vector2(0, -config.slamDescentSpeed);
                break;
                
            case Phase.Recovery:
                if (phaseTimer <= 0)
                    End();
                break;
        }
    }
    
    private void OnGroundedConfirmed()
    {
        // Descent 중에만 Impact로 전환
        if (CurrentPhase != Phase.Descent) return;
        
        EnterImpact();
    }
    
    private void EnterImpact()
    {
        CurrentPhase = Phase.Impact;
        
        Debug.Log("[SlamAction] Phase: Impact");
        
        // [Invincibility] 하강 무적 해제 (충돌 시 즉시 해제 후 Impact 무적 적용)
        player.Health.RemoveInvincible(PlayerHealth.InvincibilitySource.SlamDescent);

        // [Invincibility] 착지 무적 설정
        player.Health.SetInvincible(PlayerHealth.InvincibilitySource.SlamImpact, config.slamPostInvincibilityDuration);
        
        // 충격파 처리 (HitResolver 사용)
        ExecuteImpactAttack();
        
        // VFX 생성 (선택)
        SpawnImpactVFX();
        
        // 즉시 Recovery로 전환
        EnterRecovery();
    }
    
    private void ExecuteImpactAttack()
    {
        if (config.slamImpactAttack == null)
        {
            Debug.LogWarning("[SlamAction] impactAttack이 설정되지 않았습니다.");
            return;
        }
        
        if (HitResolver.Instance == null)
        {
            Debug.LogWarning("[SlamAction] HitResolver를 찾을 수 없습니다.");
            return;
        }
        
        // AttackSession 생성 (HitResolver 파이프라인 사용)
        var session = new AttackSession
        {
            attack = config.slamImpactAttack,
            origin = player.transform.position,
            facing = 1,  // 대칭 히트박스이므로 고정
            targetLayer = player.Combat.EnemyLayer,
            attacker = player.gameObject,
            damageMultiplier = 1f,
            rangeMultiplier = 1f
        };
        
        HitResult result = HitResolver.Instance.ProcessAttack(session);
        
        if (result.HasHit)
        {
            Debug.Log($"[SlamAction] Impact hit {result.hitCount} targets!");
        }
    }
    
    private void SpawnImpactVFX()
    {
        if (config.slamImpactVFXPrefab != null)
        {
            Object.Instantiate(
                config.slamImpactVFXPrefab, 
                player.transform.position, 
                Quaternion.identity
            );
        }
    }
    
    private void EnterRecovery()
    {
        CurrentPhase = Phase.Recovery;
        phaseTimer = config.slamRecoveryDuration;
        
        // 중력/속도 제한 해제
        player.ClearGravityOverride();
        player.ClearFallSpeedClamp();
        
        Debug.Log("[SlamAction] Phase: Recovery");
    }
    
    private void End()
    {
        Cleanup();
        CurrentPhase = Phase.None;
        
        // ActionSystem에 종료 알림
        handle?.NotifyEnded();
        
        Debug.Log("[SlamAction] Ended");
    }
    
    #endregion
    
    #region Cleanup
    
    private void Cleanup()
    {
            // 모든 제약 해제
        if (player != null)
        {
            player.Combat.LockMovement = false;
            player.ClearGravityOverride();
            player.ClearFallSpeedClamp();
            
            // [Safety] 무적 해제
            player.Health.RemoveInvincible(PlayerHealth.InvincibilitySource.SlamDescent);
        }
    }
    
    #endregion
}
