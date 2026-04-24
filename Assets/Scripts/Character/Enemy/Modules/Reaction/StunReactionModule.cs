using UnityEngine;

namespace Modules.Reaction
{
    /// <summary>
    /// 스턴 반응 모듈. EnemyStunState에서 마이그레이션.
    /// Channel: Interrupt — 모든 채널 강제 중단.
    /// 타이머 기반 스턴, 종료 시 포이즈 리셋.
    /// </summary>
    public class StunReactionModule : BehaviorModule
    {
        public override ModuleChannel Channel => ModuleChannel.Interrupt;
        public override int Priority => 20; // Hit보다 높은 우선순위
        public override string ModuleName => "StunReaction";
        
        /// <summary>
        /// 스턴 중에는 인터럽트 불가 (사망 제외).
        /// </summary>
        public override bool CanBeInterrupted() => false;
        
        public override bool CanExecute(EnemyBlackboard bb) => true;
        
        public override void Enter(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            base.Enter(entity, bb, context);
            
            var definition = entity.Definition;
            context.Duration = definition.CombatSettings.stunStateDuration;
            
            // 스턴 애니메이션
            if (entity.Animator != null)
                entity.Animator.Play("Stun");
            
            // 즉시 정지
            entity.Motor.SetVelocity(Vector2.zero);
            entity.Motor.Freeze();
        }
        
        public override void Execute(float deltaTime, EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            // 시간 초과 → 완료
            if (context.ElapsedTime >= context.Duration)
            {
                State = ModuleState.Complete;
            }
        }
        
        public override void Exit(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            // 포이즈 리셋
            if (entity.Health != null)
            {
                entity.Health.ResetPoise();
            }
            
            entity.Motor.Unfreeze();
            bb.ClearFlag(StatusFlag.IsStunned);
            base.Exit(entity, bb, context);
        }
    }
}
