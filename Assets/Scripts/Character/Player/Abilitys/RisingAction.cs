using System;
using UnityEngine;

/// <summary>
/// 상승 공격 (Rising Attack) 특수 행동
/// 더블 점프(PlayerInAirState)의 Anticipation -> Fire 매커니즘을 동일하게 차용합니다.
/// 발사 직후 행동을 스스로 종료(End)하여 플레이어가 조작권을 빠르게 회복하도록 합니다.
/// </summary>
public class RisingAction : ISpecialAction
{
    private readonly PlayerController player;
    private readonly RisingAttackData data;
    
    private ActionHandle handle;
    
    private enum Phase { Anticipation, Fire, Release }
    private Phase currentPhase;
    private float stateTimer;
    
    // 타격 관련
    private AttackSession attackSession;
    private HitResolver hitResolver;

    // 인터페이스 구현부
    public bool LocksInput => currentPhase == Phase.Anticipation; // 선딜레이 동안만 입력 잠금

    public RisingAction(PlayerController player, RisingAttackData data)
    {
        this.player = player;
        this.data = data;
        this.hitResolver = HitResolver.Instance;
    }

    public void Begin(ActionHandle handle)
    {
        this.handle = handle;
        
        // 1. 공격 세션 준비
        attackSession = new AttackSession
        {
            attack = data,
            origin = player.transform.position,
            facing = player.IsFacingRight ? 1 : -1,
            targetLayer = player.Combat.EnemyLayer,
            attacker = player.gameObject,
            damageMultiplier = player.ActiveFormData.attackProfile.damageMultiplier,
            rangeMultiplier = player.ActiveFormData.attackProfile.rangeMultiplier
        };

        // 2. 애니메이션 실행
        Mado.Character.Animation.PlayerAnimType animType = Mado.Character.Animation.PlayerAnimType.AttackUp;
        player.PlayAnimation(animType, force: true);

        // 3. Anticipation 시작 (체공 및 역경직 효과)
        currentPhase = Phase.Anticipation;
        stateTimer = data.risingAnticipationDelay;
        
        // X/Y 속도 초기 감쇠 적용
        ApplyDamping();
        
        // 이벤트 발생
        CombatEvents.RaiseAttackStart(data);
    }

    public void Update(float deltaTime)
    {
        switch (currentPhase)
        {
            case Phase.Anticipation:
                UpdateAnticipation(deltaTime);
                break;
                
            case Phase.Fire:
                ExecuteHit();
                break;
                
            case Phase.Release:
                // 이미 끝났지만 정리 대기 중
                break;
        }
    }

    private void UpdateAnticipation(float deltaTime)
    {
        stateTimer -= deltaTime;
        
        // Damping 지속 (더블 점프 딜레이와 동일)
        ApplyDamping();

        if (stateTimer <= 0f)
        {
            Fire();
        }
    }

    private void ApplyDamping()
    {
        // 공중에서 멈칫하는 '체공감' 연출 (반중력)
        Vector2 vel = player.RB.linearVelocity;
        
        // X축 저항
        float xDamping = Mathf.Pow(0.95f, Time.deltaTime * 60f);
        vel.x *= xDamping;

        // Y축: 완전히 멈춰서(역중력) '기를 모으는' 연출
        vel.y = 0f;
        
        player.RB.linearVelocity = vel;
        
        // 추가로, 혹시나 중력 가속도가 붙는 것을 완벽하게 막기 위해 오버라이드 
        player.RequestGravityOverride(0f);
    }

    private void Fire()
    {
        currentPhase = Phase.Fire;
        
        // 반중력 오버라이드 해제
        player.ClearGravityOverride();
        
        // 1. 위로 튕기는 힘 적용 (Rising Force)
        Vector2 vel = player.RB.linearVelocity;
        vel.y = data.risingForce;
        player.RB.linearVelocity = vel;
        
        // 2. 공중 상태에 상승 공격임을 알림 (JumpCut 방지)
        player.InAirState.OnRisingAttack();
        
        // 낙하 속도 제한 초기화
        player.ClearFallSpeedClamp();
    }

    private void ExecuteHit()
    {
        // 한 프레임에 타격을 처리하고 바로 Release로 전환
        if (hitResolver != null && attackSession != null)
        {
            attackSession.origin = player.transform.position;
            HitResult result = hitResolver.ProcessAttack(attackSession);
            
            if (result.HasHit)
            {
                if (data.hitStopDuration > 0f)
                {
                    player.StartHitStop(data.hitStopDuration);
                }
            }
        }
        
        End(); // 타격 1회 후 즉시 종료하여 조작권 반환
    }

    private void End()
    {
        currentPhase = Phase.Release;
        
        player.ClearGravityOverride(); // 안전망
        
        // 조작권 즉시 반환 (공중 자유 조작 상태)
        handle.NotifyEnded();
    }

    public void Cancel()
    {
        currentPhase = Phase.Release;
        player.ClearGravityOverride(); // 피격되거나 취소되었을 때 안전망
    }
}
