using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 결정 계층. Selector를 통한 모듈 선택 + Abort 판정을 전담.
/// Brain에서 호출되며, Brain의 내부 상태를 직접 수정하지 않는다.
/// 대신 "선택 결과"를 반환하여 Brain이 적용.
/// </summary>
public class DecisionLayer
{
    private IDecisionMaker movementSelector;
    private IDecisionMaker actionSelector;
    private int abortThreshold;
    
    // ========================================================
    // 초기화
    // ========================================================
    
    public void Initialize(IDecisionMaker movementSelector, IDecisionMaker actionSelector, int abortThreshold)
    {
        this.movementSelector = movementSelector;
        this.actionSelector = actionSelector;
        this.abortThreshold = abortThreshold;
    }
    
    // ========================================================
    // Movement 채널 평가
    // ========================================================
    
    /// <summary>
    /// Movement 채널에서 다음 모듈 결정.
    /// 현재 모듈 완료 → 새 모듈 선택, 실행 중 → Abort 체크.
    /// </summary>
    /// <returns>실행할 모듈. null이면 변경 없음.</returns>
    public DecisionResult EvaluateMovement(
        List<BehaviorModule> candidates,
        BehaviorModule current,
        ModuleRuntimeContext context,
        EnemyEntity entity,
        EnemyBlackboard bb)
    {
        return EvaluateChannel(candidates, current, context, movementSelector, entity, bb);
    }
    
    // ========================================================
    // Action 채널 평가
    // ========================================================
    
    /// <summary>
    /// Action 채널에서 다음 모듈 결정.
    /// </summary>
    public DecisionResult EvaluateAction(
        List<BehaviorModule> candidates,
        BehaviorModule current,
        ModuleRuntimeContext context,
        EnemyEntity entity,
        EnemyBlackboard bb)
    {
        return EvaluateChannel(candidates, current, context, actionSelector, entity, bb);
    }
    
    // ========================================================
    // 공통: 채널 평가 로직
    // ========================================================
    
    private DecisionResult EvaluateChannel(
        List<BehaviorModule> candidates,
        BehaviorModule current,
        ModuleRuntimeContext context,
        IDecisionMaker selector,
        EnemyEntity entity,
        EnemyBlackboard bb)
    {
        if (selector == null)
            return DecisionResult.NoChange();
        
        // Case 1: 현재 모듈 없거나 완료 → 새 모듈 선택
        if (current == null || current.IsComplete())
        {
            // 완료된 모듈 Exit
            if (current != null && current.IsComplete())
            {
                current.Exit(entity, bb, context);
            }
            
            var selected = selector.SelectNext(candidates, bb);
            if (selected != null)
            {
                return DecisionResult.Switch(selected);
            }
            return DecisionResult.Clear();
        }
        
        // Case 2: 실행 중 → Abort 체크
        if (current.CanBeInterrupted())
        {
            var candidate = selector.SelectNext(candidates, bb);
            if (candidate != null && candidate != current &&
                candidate.Priority > current.Priority + abortThreshold)
            {
                // Abort: 현재 모듈 중단
                current.Exit(entity, bb, context);
                return DecisionResult.Switch(candidate);
            }
        }
        
        return DecisionResult.NoChange();
    }
}

/// <summary>
/// 결정 결과. Brain이 이 결과를 받아 적용.
/// </summary>
public struct DecisionResult
{
    public enum Action
    {
        NoChange,   // 변경 없음
        Switch,     // 새 모듈로 교체
        Clear       // 모듈 해제 (후보 없음)
    }
    
    public Action action;
    public BehaviorModule newModule;
    
    public static DecisionResult NoChange() => new DecisionResult { action = Action.NoChange };
    public static DecisionResult Switch(BehaviorModule module) => new DecisionResult { action = Action.Switch, newModule = module };
    public static DecisionResult Clear() => new DecisionResult { action = Action.Clear };
}
