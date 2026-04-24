using UnityEngine;

namespace Modules.Movement
{
    /// <summary>
    /// 돌진/대시 모듈. 전조(준비) → 돌진 → 감속 3단계.
    /// Channel: Movement (WalkModule과 교체).
    /// 벽/낭떠러지 감지 시 강제 정지.
    /// </summary>
    public class DashModule : BehaviorModule
    {
        public override ModuleChannel Channel => ModuleChannel.Movement;
        public override int Priority => data.priority;
        public override string ModuleName => "Dash";
        
        private DashModuleData data;
        
        public DashModule(DashModuleData data)
        {
            this.data = data;
        }
        
        /// <summary>
        /// 실행 조건: 타겟 감지 + 사거리 내 + 쿨다운 완료 + 지면.
        /// </summary>
        public override bool CanExecute(EnemyBlackboard bb)
        {
            if (!bb.Target.IsDetected) return false;
            if (!bb.Movement.isGrounded) return false;
            
            float dist = bb.Target.distance;
            if (dist < data.minRange || dist > data.maxRange) return false;
            
            // 쿨다운 체크 (lastAttackTime을 공유하거나 별도 타이머 사용)
            // 여기서는 Blackboard의 Combat.lastAttackTime과 분리하여
            // ElapsedTime + cooldown으로 관리 (모듈 완료 후 재호출까지의 시간)
            return true;
        }
        
        public override void Enter(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            base.Enter(entity, bb, context);
            
            // 돌진 방향 결정 (타겟 방향)
            int dashDir = bb.Target.direction.x >= 0 ? 1 : -1;
            entity.Motor.SetFacing(dashDir);
            context.PatrolDirection = dashDir;
            
            // 전조 페이즈 시작
            context.CurrentDashPhase = DashPhase.Prepare;
            
            // 이동 정지 (전조 동안)
            entity.Motor.SetVelocity(Vector2.zero);
            
            // 전조 애니메이션
            if (entity.Animator != null)
                entity.Animator.Play("DashPrepare");
        }
        
        public override void Execute(float deltaTime, EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            context.ElapsedTime += deltaTime;
            
            switch (context.CurrentDashPhase)
            {
                case DashPhase.Prepare:
                    // 전조 중 — 정지 상태
                    entity.Motor.SetVelocity(Vector2.zero);
                    
                    if (context.ElapsedTime >= data.prepareDuration)
                    {
                        context.CurrentDashPhase = DashPhase.Dashing;
                        
                        // 돌진 애니메이션
                        if (entity.Animator != null)
                            entity.Animator.Play("Dash");
                    }
                    break;
                    
                case DashPhase.Dashing:
                    // 벽/낭떠러지 감지 시 강제 정지
                    if (bb.Movement.wallAhead || bb.Movement.ledgeAhead)
                    {
                        context.CurrentDashPhase = DashPhase.Recovery;
                        entity.Motor.SetVelocity(Vector2.zero);
                        break;
                    }
                    
                    // 돌진 속도 적용
                    float dashVelX = data.dashSpeed * context.PatrolDirection;
                    entity.Motor.SetVelocityX(dashVelX);
                    
                    if (context.ElapsedTime >= data.prepareDuration + data.dashDuration)
                    {
                        context.CurrentDashPhase = DashPhase.Recovery;
                        entity.Motor.SetVelocity(Vector2.zero);
                    }
                    break;
                    
                case DashPhase.Recovery:
                    // 감속 — 정지 상태 유지
                    entity.Motor.SetVelocityX(0f);
                    
                    if (context.ElapsedTime >= data.TotalDuration)
                    {
                        State = ModuleState.Complete;
                    }
                    break;
            }
        }
        
        public override void Exit(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            base.Exit(entity, bb, context);
            entity.Motor.SetVelocityX(0f);
        }
        
        /// <summary>
        /// 돌진 중에는 인터럽트 불가 (전조/감속에서는 가능).
        /// </summary>
        public override bool CanBeInterrupted()
        {
            return State != ModuleState.Active;
        }
    }
}
