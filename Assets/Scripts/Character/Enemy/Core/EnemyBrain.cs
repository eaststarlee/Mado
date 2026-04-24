using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI 오케스트레이터. 판단/실행/인터럽트를 직접 구현하지 않고,
/// 3개 Layer에 위임하는 순수 조율자.
/// 
/// 흐름:
/// ① InterruptLayer.ProcessQueue() → NeedsForceStop 확인 → 채널 중단
/// ② InterruptLayer.IsInterrupted? → 인터럽트 실행 → return
/// ③ DecisionLayer.Evaluate() → DecisionResult 적용
/// ④ ExecutionLayer.Execute() → 모듈 Tick
/// </summary>
public class EnemyBrain
{
    // --- 3계층 ---
    private InterruptLayer interruptLayer = new InterruptLayer();
    private DecisionLayer decisionLayer = new DecisionLayer();
    private ExecutionLayer executionLayer = new ExecutionLayer();
    
    // --- 모듈 목록 ---
    private List<BehaviorModule> movementModules = new List<BehaviorModule>();
    private List<BehaviorModule> actionModules = new List<BehaviorModule>();
    
    // --- 현재 실행 중 모듈 ---
    private BehaviorModule currentMovement;
    private BehaviorModule currentAction;
    
    // --- 런타임 컨텍스트 ---
    private ModuleRuntimeContext movementContext = new ModuleRuntimeContext();
    private ModuleRuntimeContext actionContext = new ModuleRuntimeContext();
    
    // --- 외부 접근 프로퍼티 ---
    public bool IsInterrupted => interruptLayer.IsInterrupted;
    public BehaviorModule CurrentMovement => currentMovement;
    public BehaviorModule CurrentAction => currentAction;
    public BehaviorModule CurrentInterrupt => interruptLayer.Current;
    
    // ========================================================
    // 초기화
    // ========================================================
    
    public void Initialize(
        IDecisionMaker movementSelector,
        IDecisionMaker actionSelector,
        BehaviorModule hitReaction,
        BehaviorModule stunReaction,
        BehaviorModule death,
        int abortThreshold = 0)
    {
        interruptLayer.Initialize(hitReaction, stunReaction, death);
        decisionLayer.Initialize(movementSelector, actionSelector, abortThreshold);
    }
    
    /// <summary>
    /// 모듈 등록.
    /// </summary>
    public void AddModule(BehaviorModule module)
    {
        switch (module.Channel)
        {
            case ModuleChannel.Movement:
                if (!movementModules.Contains(module))
                    movementModules.Add(module);
                break;
            case ModuleChannel.Action:
                if (!actionModules.Contains(module))
                    actionModules.Add(module);
                break;
        }
    }
    
    /// <summary>
    /// 모듈 제거.
    /// </summary>
    public void RemoveModule(BehaviorModule module)
    {
        switch (module.Channel)
        {
            case ModuleChannel.Movement:
                movementModules.Remove(module);
                break;
            case ModuleChannel.Action:
                actionModules.Remove(module);
                break;
        }
    }
    
    // ========================================================
    // 외부 API
    // ========================================================
    
    /// <summary>
    /// 인터럽트 이벤트 큐에 추가.
    /// EnemyEntity.HandleHit() → 여기 → InterruptLayer.Enqueue()
    /// </summary>
    public void EnqueueInterrupt(InterruptType type, DamageInfo? info = null)
    {
        interruptLayer.Enqueue(type, info);
    }
    
    // ========================================================
    // Tick (매 프레임 EnemyEntity.Update에서 호출)
    // ========================================================
    
    public void Tick(float deltaTime, EnemyEntity entity, EnemyBlackboard bb)
    {
        // ① InterruptLayer — 큐 소비 + 우선순위 판정
        interruptLayer.ProcessQueue(entity, bb);
        
        // ForceStop 요청 있으면 채널 중단
        if (interruptLayer.NeedsForceStop)
        {
            ForceStopAllChannels(entity, bb);
            interruptLayer.ClearForceStop();
        }
        
        // ② 인터럽트 실행 중이면 일반 Decision/Execution 건너뜀
        if (interruptLayer.IsInterrupted)
        {
            interruptLayer.ExecuteCurrent(deltaTime, entity, bb);
            return;
        }
        
        // ③ DecisionLayer — 모듈 선택/교체 판단
        ApplyDecision(
            decisionLayer.EvaluateMovement(movementModules, currentMovement, movementContext, entity, bb),
            ref currentMovement, movementContext, entity, bb);
        
        ApplyDecision(
            decisionLayer.EvaluateAction(actionModules, currentAction, actionContext, entity, bb),
            ref currentAction, actionContext, entity, bb);
        
        // ④ ExecutionLayer — 채널별 모듈 Tick
        executionLayer.Execute(deltaTime, currentMovement, movementContext, currentAction, actionContext, entity, bb);
    }
    
    // ========================================================
    // 내부: DecisionResult 적용
    // ========================================================
    
    private void ApplyDecision(DecisionResult result, ref BehaviorModule current, ModuleRuntimeContext context, EnemyEntity entity, EnemyBlackboard bb)
    {
        switch (result.action)
        {
            case DecisionResult.Action.Switch:
                current = result.newModule;
                context.Reset();
                current.Enter(entity, bb, context);
                break;
                
            case DecisionResult.Action.Clear:
                current = null;
                break;
                
            case DecisionResult.Action.NoChange:
            default:
                break;
        }
    }
    
    // ========================================================
    // 내부: 모든 채널 강제 중단
    // ========================================================
    
    private void ForceStopAllChannels(EnemyEntity entity, EnemyBlackboard bb)
    {
        if (currentMovement != null && !currentMovement.IsComplete())
        {
            currentMovement.Exit(entity, bb, movementContext);
            currentMovement = null;
        }
        
        if (currentAction != null && !currentAction.IsComplete())
        {
            currentAction.Exit(entity, bb, actionContext);
            currentAction = null;
        }
    }
}
