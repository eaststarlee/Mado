using UnityEngine;

namespace Modules.Combat
{
    /// <summary>
    /// 근접 공격 모듈. 전조 → 활성 → 후딜 3단계.
    /// Channel: Action (Movement와 동시 실행 가능).
    /// HitboxManager에 판정 위임, AnimationEventRouter로 이벤트 수신.
    /// </summary>
    public class MeleeSwingModule : BehaviorModule
    {
        public override ModuleChannel Channel => ModuleChannel.Action;
        public override int Priority => 10;
        public override string ModuleName => "MeleeSwing";
        
        private MeleeSwingModuleData data;
        
        public MeleeSwingModule(MeleeSwingModuleData data)
        {
            this.data = data;
        }
        
        /// <summary>
        /// 실행 조건: 근접 사거리 내 + 쿨다운 완료.
        /// </summary>
        public override bool CanExecute(EnemyBlackboard bb)
        {
            if (!bb.Target.IsDetected) return false;
            if (bb.Target.distance > data.attackRange) return false;
            
            // 쿨다운 체크
            float timeSinceLastAttack = Time.time - bb.Combat.lastAttackTime;
            if (timeSinceLastAttack < data.cooldown) return false;
            
            return true;
        }
        
        public override void Enter(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            base.Enter(entity, bb, context);
            
            // 전조 페이즈 결정 (preAttackDelay가 0보다 크면 PreDelay부터 시작)
            context.CurrentAttackPhase = data.preAttackDelay > 0f ? AttackPhase.PreDelay : AttackPhase.Anticipation;
            context.HitboxActive = false;
            context.HasAttacked = false;
            context.ElapsedTime = 0f;
            
            // 이동 정지 (옵션에 따라) - 선딜레이 대기 시간에도 멈춤 유지 (의도적 접근 타이밍 뺏기)
            if (data.stopMovementOnAttack)
            {
                entity.Motor.SetVelocity(Vector2.zero);
            }
            
            // 즉시 공격인 경우 애니메이션 바로 재생 
            if (context.CurrentAttackPhase == AttackPhase.Anticipation)
            {
                StartAnticipation(entity, bb);
            }
            else 
            {
                // PreDelay (뜸들이기) 동안에는 보통 Idle 상태 유지
                if (entity.Animator != null) entity.Animator.Play("Idle");
            }
            
            // 히트박스 정의 등록
            if (entity.HitboxManager != null)
            {
                entity.HitboxManager.RegisterHitbox("MeleeSwing", new HitboxDefinition
                {
                    offset = data.hitboxOffset,
                    size = data.hitboxSize,
                    targetLayer = data.targetLayer
                });
            }
            
            // 애니메이션 이벤트 구독 (타이머 기반 fallback도 지원)
            if (entity.AnimEventRouter != null)
            {
                entity.AnimEventRouter.Subscribe("HitboxOn", () => OnHitboxOn(entity, context), this);
                entity.AnimEventRouter.Subscribe("HitboxOff", () => OnHitboxOff(entity, context), this);
            }
        }
        
        private void StartAnticipation(EnemyEntity entity, EnemyBlackboard bb)
        {
            // 공격 방향으로 회전
            if (bb.Target.IsDetected)
            {
                int attackDir = bb.Target.direction.x >= 0 ? 1 : -1;
                entity.Motor.SetFacing(attackDir);
            }
            
            if (entity.Animator != null)
                entity.Animator.Play("Attack");
        }
        
        public override void Execute(float deltaTime, EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            context.ElapsedTime += deltaTime;
            
            // 이동 정지 옵션 처리
            if (data.stopMovementOnAttack && context.CurrentAttackPhase != AttackPhase.Active)
            {
                // 인스펙터에 설정한 정지 시간이 0보다 크면 그 시간 동안만 멈춤
                if (data.stopMovementDuration > 0f)
                {
                    if (context.ElapsedTime < data.stopMovementDuration)
                    {
                        entity.Motor.SetVelocityX(0f);
                    }
                }
                // 정지 시간이 0이면(기본값) 런지(Active) 전까지 계속 멈춤 유지
                else
                {
                    entity.Motor.SetVelocityX(0f);
                }
            }
            
            // 타이머 기반 페이즈 전환 (AnimationEvent fallback)
            switch (context.CurrentAttackPhase)
            {
                case AttackPhase.PreDelay:
                    if (context.ElapsedTime >= data.preAttackDelay)
                    {
                        // 뜸들이기 종료 -> 진짜 공격 준비
                        context.CurrentAttackPhase = AttackPhase.Anticipation;
                        context.ElapsedTime = 0f; // 타이머 리셋
                        StartAnticipation(entity, bb);
                    }
                    break;
                    
                case AttackPhase.Anticipation:
                    if (context.ElapsedTime >= data.anticipationDuration)
                    {
                        // AnimationEvent가 아직 도착 안 했으면 타이머로 활성화
                        if (!context.HitboxActive)
                        {
                            OnHitboxOn(entity, context);
                        }
                    }
                    break;
                    
                case AttackPhase.Active:
                    if (context.ElapsedTime >= data.anticipationDuration + data.activeDuration)
                    {
                        // 히트박스 OFF
                        if (context.HitboxActive)
                        {
                            OnHitboxOff(entity, context);
                        }
                    }
                    break;
                    
                case AttackPhase.Recovery:
                    if (context.ElapsedTime >= data.anticipationDuration + data.activeDuration + data.recoveryDuration)
                    {
                        State = ModuleState.Complete;
                    }
                    break;
            }
        }
        
        public override void Exit(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            base.Exit(entity, bb, context);
            
            // 안전장치: 히트박스 반드시 OFF
            entity.HitboxManager?.RequestDisable("MeleeSwing");
            
            // 애니메이션 이벤트 구독 해제
            entity.AnimEventRouter?.UnsubscribeAll(this);
            
            // 마지막 공격 시간 기록
            bb.Combat.lastAttackTime = Time.time;
        }
        
        /// <summary>
        /// 공격 중 인터럽트 불가 (전조/활성 단계에서).
        /// 후딜에서는 인터럽트 가능.
        /// </summary>
        public override bool CanBeInterrupted()
        {
            // PreDelay 중에는 맞아도 캔슬될 수 있게 하려면 (State != ModuleState.Active) 유지
            return State != ModuleState.Active;
        }
        
        // --- 히트박스 이벤트 핸들러 ---
        
        private void OnHitboxOn(EnemyEntity entity, ModuleRuntimeContext context)
        {
            if (context.HitboxActive) return; // 중복 방지
            
            context.CurrentAttackPhase = AttackPhase.Active;
            context.HitboxActive = true;
            
            // 액티브 되는 순간 전진 이동 (Lunge) 적용
            if (data.forwardDashDistance > 0f && data.activeDuration > 0f)
            {
                float dashVelocityX = (data.forwardDashDistance / data.activeDuration) * entity.Motor.FacingDirection;
                entity.Motor.SetVelocityX(dashVelocityX);
            }
            
            // DamageInfo 생성
            var damageInfo = new DamageInfo
            {
                damage = data.damage,
                hitPoint = entity.transform.position,
                hitDirection = new Vector2(entity.Motor.FacingDirection, 0f),
                knockbackForce = new Vector2(data.knockbackForce, 0f),
                hitType = data.hitType,
                source = entity.gameObject,
                canBeParried = true
            };
            
            entity.HitboxManager?.RequestEnable("MeleeSwing", damageInfo);
        }
        
        private void OnHitboxOff(EnemyEntity entity, ModuleRuntimeContext context)
        {
            context.CurrentAttackPhase = AttackPhase.Recovery;
            context.HitboxActive = false;
            
            // 전진 이동을 했다면 후딜 진입 시 속도 0으로 (옵션이 켜져 있는 경우만)
            if (data.forwardDashDistance > 0f && data.stopMovementOnAttack)
            {
                entity.Motor.SetVelocityX(0f);
            }
            
            entity.HitboxManager?.RequestDisable("MeleeSwing");
        }
        
        // Gizmo 표시 상태 동기화용 (EnemyEntity 등에서 읽을 수 있게)
        public bool IsHitting => State == ModuleState.Active && HitboxActive(null);
        private bool HitboxActive(ModuleRuntimeContext ctx) => true; // (Refactoring note: using HitboxManager IsActive is better)
    }
}
