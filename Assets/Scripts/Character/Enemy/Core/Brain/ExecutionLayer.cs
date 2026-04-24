using UnityEngine;

/// <summary>
/// 실행 계층. 채널별 모듈 Execute() 호출을 전담.
/// 가장 단순한 계층 — deltaTime 전달 + ElapsedTime 갱신만 담당.
/// </summary>
public class ExecutionLayer
{
    /// <summary>
    /// Movement + Action 채널의 현재 모듈 실행.
    /// </summary>
    public void Execute(
        float deltaTime,
        BehaviorModule movement, ModuleRuntimeContext movCtx,
        BehaviorModule action, ModuleRuntimeContext actCtx,
        EnemyEntity entity, EnemyBlackboard bb)
    {
        // Movement Channel (하체)
        if (movement != null && !movement.IsComplete())
        {
            movCtx.ElapsedTime += deltaTime;
            movement.Execute(deltaTime, entity, bb, movCtx);
        }
        
        // Action Channel (상체, 동시 실행)
        if (action != null && !action.IsComplete())
        {
            actCtx.ElapsedTime += deltaTime;
            action.Execute(deltaTime, entity, bb, actCtx);
        }
    }
}
