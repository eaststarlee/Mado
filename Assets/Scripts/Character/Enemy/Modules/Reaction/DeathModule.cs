using UnityEngine;
using System.Collections;

namespace Modules.Reaction
{
    /// <summary>
    /// 사망 모듈. EnemyDeathState에서 마이그레이션.
    /// Channel: Interrupt — 최고 우선순위. 모든 것 무조건 중단.
    /// Collider 비활성화, 사망 애니메이션 후 오브젝트 풀 반환.
    /// </summary>
    public class DeathModule : BehaviorModule
    {
        public override ModuleChannel Channel => ModuleChannel.Interrupt;
        public override int Priority => 100; // 최고 우선순위
        public override string ModuleName => "Death";
        
        /// <summary>
        /// 사망 중 인터럽트 절대 불가.
        /// </summary>
        public override bool CanBeInterrupted() => false;
        
        public override bool CanExecute(EnemyBlackboard bb) => true;
        
        private float deathAnimDuration = 0.5f;
        
        public override void Enter(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            base.Enter(entity, bb, context);
            
            // 즉시 정지
            entity.Motor.SetVelocity(Vector2.zero);
            
            // Collider 비활성화
            if (entity.Collider != null)
                entity.Collider.enabled = false;
            
            // 사망 애니메이션
            if (entity.Animator != null)
                entity.Animator.Play("Die");
            
            // 사망 코루틴 시작 (오브젝트 풀 반환)
            entity.StartCoroutine(DeathRoutine(entity));
        }
        
        public override void Execute(float deltaTime, EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            // 사망 코루틴이 처리하므로 여기서는 대기
            // 코루틴 완료 후 State = Complete
        }
        
        public override void Exit(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            base.Exit(entity, bb, context);
        }
        
        private IEnumerator DeathRoutine(EnemyEntity entity)
        {
            yield return new WaitForSeconds(deathAnimDuration);
            
            State = ModuleState.Complete;
            
            // 오브젝트 풀 반환
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnToPool("Enemy", entity.gameObject);
            }
            else
            {
                GameObject.Destroy(entity.gameObject);
            }
        }
    }
}
