using UnityEngine;
using System.Collections.Generic;

public class PetRushState : PetFollowState
{
    private Collider2D currentTarget;
    private bool isHitStop;
    private float hitStopTimeRef;
    private Vector2 recoilVelocity;
    private float nextAttackTime;
    
    // 공격 중복 방지 (단일 돌진 단위)
    private HashSet<GameObject> hitTargetsInCurrentDash = new HashSet<GameObject>();

    public PetRushState(PetController pet, PetStateMachine stateMachine) : base(pet, stateMachine)
    {
    }

    public override void Enter()
    {
        // PetFollowState의 Enter가 호출되어 Follow에 필요한 앵커 초기화 등을 수행
        base.Enter();
        
        isHitStop = false;
        currentTarget = null;
        nextAttackTime = 0f; // 첫 타격은 즉시
        hitTargetsInCurrentDash.Clear();
        
        // 바로 첫 타겟 검색 (쿨타임 통과)
        FindNextTarget();
    }

    public override void Exit()
    {
        base.Exit();
        currentTarget = null;
        hitTargetsInCurrentDash.Clear();
    }

    public override void LogicUpdate()
    {
        // 타격 간 쿨타임(전역 쿨타임) 대기 중이면 새로운 타겟을 찾지 않고 철저히 Follow 로직만 수행
        if (Time.time < nextAttackTime)
        {
            currentTarget = null;
            base.LogicUpdate();
            return;
        }

        // 전투 대상이 없을 때에는 주변을 주시하며 Follow 행동(Stuck 감지 등 부모 로직)을 수행
        if (currentTarget == null)
        {
            FindNextTarget();
            
            if (currentTarget == null)
            {
                base.LogicUpdate();
                return;
            }
        }
        
        // --- 타겟이 존재하는 전투/돌진 모드 ---
        
        // 타겟이 유실되거나 비활성화(사망 등)된 경우 교체
        if (!currentTarget.gameObject.activeInHierarchy)
        {
            FindNextTarget();
            if (currentTarget == null)
            {
                base.LogicUpdate();
                return;
            }
        }

        // 역경직(Recoil) 시간 대기 체크
        if (isHitStop)
        {
            if (Time.time >= hitStopTimeRef)
            {
                isHitStop = false;
                pet.RushCharges--; // 타격 1회 인정
                
                // 횟수를 모두 소진하면 완전 해제
                if (pet.RushCharges <= 0)
                {
                    stateMachine.ChangeState(pet.FollowState);
                    return;
                }
                else
                {
                    // 타격 후 쿨다운 갱신 및 타겟 초기화. 다음 공격 전까지는 FollowState처럼 동작함.
                    nextAttackTime = Time.time + pet.PetData.rushGlobalCooldown;
                    currentTarget = null; 
                }
            }
            return;
        }

        // 좌우 반전
        float diffX = currentTarget.transform.position.x - pet.transform.position.x;
        pet.Flip(diffX > 0);
    }

    public override void PhysicsUpdate()
    {
        // 역경직 중: 정해진 반동 속도만 유지
        if (isHitStop)
        {
            pet.RB.linearVelocity = recoilVelocity;
            return;
        }

        // 돌진 중: 대상 위치로 엄청난 속도로 이동
        if (currentTarget != null)
        {
            Vector2 targetPos = currentTarget.transform.position;
            if (currentTarget.bounds != null)
            {
                targetPos = currentTarget.bounds.center;
            }
            
            Vector2 dir = (targetPos - (Vector2)pet.transform.position).normalized;
            pet.RB.linearVelocity = dir * pet.PetData.rushSpeed;
            
            CheckHit();
        }
        else
        {
            // 대상이 없으면 부모 클래스(FollowState) 작동을 그대로 모방하여 자연스럽게 배회/앵커 복귀
            base.PhysicsUpdate();
        }
    }

    private void CheckHit()
    {
        float hitRadius = pet.Collider.bounds.extents.x * 2.5f; 
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(pet.transform.position, hitRadius, pet.PetData.targetLayer);
        foreach (var col in hits)
        {
            if (col == currentTarget && !hitTargetsInCurrentDash.Contains(col.gameObject))
            {
                ExecuteHit(col);
                break;
            }
        }
    }

    private void ExecuteHit(Collider2D targetCol)
    {
        hitTargetsInCurrentDash.Add(targetCol.gameObject);
        
        IDamageable damageable = targetCol.GetComponent<IDamageable>();
        if (damageable != null && !damageable.IsInvincible)
        {
            Vector2 hitDir = (targetCol.transform.position - pet.transform.position).normalized;
            
            DamageInfo info = new DamageInfo
            {
                damage = pet.PetData.rushDamage,
                hitPoint = targetCol.ClosestPoint(pet.transform.position),
                hitDirection = hitDir,
                damageSource = pet.transform.position,
                knockbackForce = Vector2.zero, 
                stunDuration = pet.PetData.rushHitStopDuration,
                poiseDamage = 0f,
                damageType = DamageType.Physical,
                hitType = HitType.Light,
                ignoreInvincibility = false,
                ignoreArmor = false,
                canBeParried = false,
                source = pet.gameObject
            };
            
            damageable.TakeDamage(info);
            
            IEnemyReaction reaction = targetCol.GetComponent<IEnemyReaction>();
            reaction?.OnHitReaction(info, pet.gameObject);
        }
        
        // 반동 및 역경직 세팅
        isHitStop = true;
        hitStopTimeRef = Time.time + pet.PetData.rushHitStopDuration;
        
        Vector2 recoilDir = (pet.transform.position - targetCol.transform.position).normalized;
        recoilDir = (recoilDir + Vector2.up * 0.3f).normalized; 
        recoilVelocity = recoilDir * pet.PetData.rushRecoilForce; 
        pet.RB.linearVelocity = recoilVelocity;
    }

    private void FindNextTarget()
    {
        hitTargetsInCurrentDash.Clear();
        currentTarget = pet.FindNearestEnemyInSight();
    }
}
