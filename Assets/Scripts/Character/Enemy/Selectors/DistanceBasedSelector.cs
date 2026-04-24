using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 거리 기반 행동 선택기.
/// 타겟까지의 거리에 따라 최적 모듈을 선택한다.
/// CanExecute() 통과한 모듈 중 Priority가 가장 높은 것을 반환.
/// </summary>
public class DistanceBasedSelector : IDecisionMaker
{
#if UNITY_EDITOR
    private DecisionTrace lastDecision;
    public DecisionTrace LastDecision => lastDecision;
#endif
    
    public BehaviorModule SelectNext(List<BehaviorModule> candidates, EnemyBlackboard bb)
    {
        BehaviorModule best = null;
        int bestPriority = int.MinValue;
        
#if UNITY_EDITOR
        lastDecision = new DecisionTrace
        {
            selectorName = nameof(DistanceBasedSelector),
            evaluatedModules = new List<ModuleEvaluation>(),
            timestamp = Time.time
        };
#endif
        
        for (int i = 0; i < candidates.Count; i++)
        {
            var module = candidates[i];
            bool canExec = module.CanExecute(bb);
            
#if UNITY_EDITOR
            lastDecision.evaluatedModules.Add(new ModuleEvaluation
            {
                moduleName = module.ModuleName,
                canExecute = canExec,
                priority = module.Priority,
                rejectReason = canExec ? "" : "CanExecute=false"
            });
#endif
            
            if (canExec && module.Priority > bestPriority)
            {
                best = module;
                bestPriority = module.Priority;
            }
        }
        
#if UNITY_EDITOR
        lastDecision.chosenModule = best?.ModuleName ?? "None";
        lastDecision.reason = best != null 
            ? $"Highest priority ({bestPriority}) among {candidates.Count} candidates, TargetDist={bb.Target.distance:F1}"
            : "No executable module found";
#endif
        
        return best;
    }
}
