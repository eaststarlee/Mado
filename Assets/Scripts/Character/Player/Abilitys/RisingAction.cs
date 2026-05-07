using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상승 이동 공격 (Rising Fire Dash) 특수 행동
/// 주인공이 불꽃으로 변하여 좌상/상/우상 방향으로 빠르게 돌진하며 타격합니다.
/// 공중에서 1회만 사용 가능하며, 돌진 중 무적 판정을 가질 수 있습니다.
/// </summary>
public class RisingAction : ISpecialAction
{
    private readonly PlayerController player;
    private readonly RisingAttackData data;
    
    private ActionHandle handle;
    
    private enum Phase { Anticipation, Dash, Release }
    private Phase currentPhase;
    private float stateTimer;
    
    // 이동 관련
    private Vector2 dashDirection;
    
    // 타격 관련
    private AttackSession attackSession;
    private HitResolver hitResolver;

    // 인터페이스 구현부
    public bool LocksInput => currentPhase == Phase.Anticipation || currentPhase == Phase.Dash;

    public RisingAction(PlayerController player, RisingAttackData data)
    {
        this.player = player;
        this.data = data;
        this.hitResolver = HitResolver.Instance;
    }

    public void Begin(ActionHandle handle)
    {
        this.handle = handle;
        
        // 1. 방향 결정 (입력 기반)
        CalculateDashDirection();

        // 2. 공격 세션 준비
        attackSession = new AttackSession
        {
            attack = data,
            origin = player.transform.position,
            facing = player.IsFacingRight ? 1 : -1,
            targetLayer = player.Combat.EnemyLayer,
            attacker = player.gameObject,
            damageMultiplier = player.ActiveFormData.attackProfile.damageMultiplier,
            rangeMultiplier = player.ActiveFormData.attackProfile.rangeMultiplier,
            alreadyHit = new HashSet<GameObject>()
        };

        // 3. 애니메이션 실행 (불꽃 대시 전용 애니메이션이 없을 경우 AttackUp 사용)
        Mado.Character.Animation.PlayerAnimType animType = Mado.Character.Animation.PlayerAnimType.AttackUp;
        player.PlayAnimation(animType, force: true);

        // 4. Anticipation 시작 (기를 모으는 단계)
        currentPhase = Phase.Anticipation;
        stateTimer = data.risingAnticipationDelay;
        
        ApplyAnticipationDamping();
        
        CombatEvents.RaiseAttackStart(data);
    }

    private void CalculateDashDirection()
    {
        float inputX = player.inputReader.InputX;
        
        // 기본은 수직 상승
        dashDirection = Vector2.up;

        if (inputX > 0.1f)
        {
            // 우상단
            dashDirection = new Vector2(1f * data.diagonalMultiplier, 1f).normalized;
            player.CheckDirectionToFace(true);
        }
        else if (inputX < -0.1f)
        {
            // 좌상단
            dashDirection = new Vector2(-1f * data.diagonalMultiplier, 1f).normalized;
            player.CheckDirectionToFace(false);
        }
    }

    public void Update(float deltaTime)
    {
        switch (currentPhase)
        {
            case Phase.Anticipation:
                UpdateAnticipation(deltaTime);
                break;
                
            case Phase.Dash:
                UpdateDash(deltaTime);
                break;
        }
    }

    private void UpdateAnticipation(float deltaTime)
    {
        stateTimer -= deltaTime;
        ApplyAnticipationDamping();

        if (stateTimer <= 0f)
        {
            StartDash();
        }
    }

    private void ApplyAnticipationDamping()
    {
        Vector2 vel = player.RB.linearVelocity;
        float xDamping = Mathf.Pow(0.9f, Time.deltaTime * 60f);
        vel.x *= xDamping;
        vel.y = 0f;
        player.RB.linearVelocity = vel;
        player.RequestGravityOverride(0f);
    }

    private void StartDash()
    {
        currentPhase = Phase.Dash;
        stateTimer = data.dashDuration;
        
        // 무적 판정 설정
        if (data.isInvincibleDuringDash && player.Health != null)
        {
            player.Health.SetInvincible(PlayerHealth.InvincibilitySource.RisingAttack, data.dashDuration + 0.05f);
        }

        // 타격 판정은 UpdateDash에서 실시간으로 수행하도록 변경
        // StartDash에서의 즉시 타격 로직 제거
        
        // 공중 상태 동기화 (JumpCut 방지)
        player.InAirState.OnRisingAttack();
    }

    private void UpdateDash(float deltaTime)
    {
        stateTimer -= deltaTime;
        
        // 일정한 속도로 돌진 (중력 무시)
        player.RB.linearVelocity = dashDirection * data.dashSpeed;
        player.RequestGravityOverride(0f);

        // [New] 돌진 중 지속적으로 타격 판정 시도
        // AlreadyHit 목록 덕분에 돌진 경로에 있는 적은 각각 1회씩만 맞게 됨
        ExecuteHit();

        if (stateTimer <= 0f)
        {
            End();
        }
    }

    private void ExecuteHit()
    {
        if (hitResolver != null && attackSession != null)
        {
            attackSession.origin = player.transform.position;
            HitResult result = hitResolver.ProcessAttack(attackSession);
            
            if (result.HasHit && data.hitStopDuration > 0f)
            {
                player.StartHitStop(data.hitStopDuration);
            }
        }
    }

    private void End()
    {
        currentPhase = Phase.Release;
        player.ClearGravityOverride();
        
        // [Fix] 돌진 종료 시 속도를 감쇄시켜 "슝" 하고 날아가는 현상 방지
        // 설정된 비율만큼만 속도를 남기고 나머지는 제거하여 공중 조작권을 즉시 회복함
        player.RB.linearVelocity *= data.momentumRetention;

        // 무적 해제 (타이머가 있지만 즉시 해제 안전장치)
        if (data.isInvincibleDuringDash && player.Health != null)
        {
            player.Health.RemoveInvincible(PlayerHealth.InvincibilitySource.RisingAttack);
        }

        handle.NotifyEnded();
    }

    public void Cancel()
    {
        End();
    }
}
