using UnityEngine;

namespace Modules.Reaction
{
    /// <summary>
    /// 피격 반응 모듈. EnemyHitState에서 마이그레이션.
    /// Channel: Interrupt — 모든 채널 강제 중단.
    /// 넉백 커브 기반 물리 적용 (Motor 경유).
    /// </summary>
    public class HitReactionModule : BehaviorModule
    {
        public override ModuleChannel Channel => ModuleChannel.Interrupt;
        public override int Priority => 10;
        public override string ModuleName => "HitReaction";
        
        /// <summary>
        /// 피격 중 추가 인터럽트 불가 (데미지만 적용).
        /// 대신 RefreshHit으로 재피격 처리.
        /// </summary>
        public override bool CanBeInterrupted() => false;
        
        public override bool CanExecute(EnemyBlackboard bb) => true;
        
        public override void Enter(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            base.Enter(entity, bb, context);
            
            Debug.Log($"[HitReaction] Enter Called. HasDamageInfo: {context.StoredDamageInfo.HasValue}");

            if (!context.StoredDamageInfo.HasValue) return;
            
            DamageInfo info = context.StoredDamageInfo.Value;
            var definition = entity.Definition;
            
            // 스턴 지속시간 결정 (더 긴 시간 적용)
            context.Duration = Mathf.Max(info.stunDuration, definition.CombatSettings.stunDuration);
            
            context.KnockbackTimer = 0f;
            
            // 넉백 방향 계산
            Vector2 dir = info.hitDirection.normalized;
            if (dir == Vector2.zero)
            {
                Vector2 diff = (Vector2)entity.transform.position - info.damageSource;
                dir = diff.normalized;
            }
            if (dir == Vector2.zero) dir = Vector2.right;
            
            float horizontalPush = info.knockbackForce.x;
            float verticalLift = info.knockbackForce.y;
            float facingDir = Mathf.Sign(dir.x);
            if (facingDir == 0) facingDir = 1;
            
            context.InitialKnockback = new Vector2(
                facingDir * horizontalPush * definition.CombatSettings.knockbackMultiplier,
                verticalLift * definition.CombatSettings.knockbackMultiplier
            );
            
            // Motor에 넉백 적용
            entity.Motor.Unfreeze();
            entity.Motor.SetVelocity(context.InitialKnockback);
            
            // HitStop
            if (HitStopManager.Instance != null)
                HitStopManager.Instance.Stop(definition.CombatSettings.hitStopDuration);
            
            // 애니메이션
            if (entity.Animator != null)
                entity.Animator.Play("Hit", -1, 0f);

            Debug.Log($"[HitReaction] Enter - Duration: {context.Duration}, Knockback: {context.InitialKnockback}, Force: {info.knockbackForce}");

            Debug.Log($"[HitReaction] Enter - Duration: {context.Duration}, Knockback: {context.InitialKnockback}, Force: {info.knockbackForce}");
        }
        
        public override void Execute(float deltaTime, EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            if (!context.StoredDamageInfo.HasValue)
            {
                State = ModuleState.Complete;
                return;
            }
            
            var definition = entity.Definition;
            
            context.KnockbackTimer += Time.fixedDeltaTime;
            
            if (context.KnockbackTimer < context.Duration)
            {
                float t = Mathf.Clamp01(context.KnockbackTimer / context.Duration);
                float curveValue = definition.CombatSettings.knockbackCurve != null
                    ? definition.CombatSettings.knockbackCurve.Evaluate(t)
                    : 1f - t;
                
                Vector2 currentVel = entity.Motor.GetVelocity();
                entity.Motor.SetVelocity(new Vector2(
                    context.InitialKnockback.x * curveValue,
                    currentVel.y
                ));
            }
            else
            {
                entity.Motor.Stop();
            }
            
            // 시간 초과 → 완료
            if (context.ElapsedTime >= context.Duration)
            {
                State = ModuleState.Complete;
            }
        }
        
        public override void Exit(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            entity.Motor.Stop();
            bb.ClearFlag(StatusFlag.IsHit);
            base.Exit(entity, bb, context);
        }
        
        /// <summary>
        /// 피격 중 재피격 시 호출. 기존 로직 보존 (RefreshStun).
        /// </summary>
        public void RefreshHit(DamageInfo newInfo, EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            context.StoredDamageInfo = newInfo;
            context.ElapsedTime = 0f;
            context.KnockbackTimer = 0f;
            
            context.Duration = Mathf.Max(newInfo.stunDuration, entity.Definition.CombatSettings.stunDuration);
            
            // 넉백 방향 재계산
            Vector2 dir = newInfo.hitDirection.normalized;
            if (dir == Vector2.zero)
            {
                Vector2 diff = (Vector2)entity.transform.position - newInfo.damageSource;
                dir = diff.normalized;
            }
            if (dir == Vector2.zero) dir = Vector2.right;
            
            float facingDir = Mathf.Sign(dir.x);
            if (facingDir == 0) facingDir = 1;
            
            context.InitialKnockback = new Vector2(
                facingDir * newInfo.knockbackForce.x * entity.Definition.CombatSettings.knockbackMultiplier,
                newInfo.knockbackForce.y * entity.Definition.CombatSettings.knockbackMultiplier
            );
            
            entity.Motor.SetVelocity(context.InitialKnockback);
            
            if (entity.Animator != null)
                entity.Animator.Play("Hit", -1, 0f);
        }
    }
}
