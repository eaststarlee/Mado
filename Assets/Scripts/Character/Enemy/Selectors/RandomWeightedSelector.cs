using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가중치 기반 랜덤 행동 선택기.
/// Action 채널에서 다양한 공격 패턴을 무작위로 선택.
/// 반복 방지 옵션: 직전 선택을 제외.
/// </summary>
public class RandomWeightedSelector : IDecisionMaker
{
    private bool preventRepeat;
    private BehaviorModule lastSelected;
    
    #if UNITY_EDITOR
    public DecisionTrace LastDecision { get; private set; }
    #endif
    
    public RandomWeightedSelector(bool preventRepeat = true)
    {
        this.preventRepeat = preventRepeat;
    }
    
    public BehaviorModule SelectNext(List<BehaviorModule> candidates, EnemyBlackboard bb)
    {
        #if UNITY_EDITOR
        var trace = new DecisionTrace
        {
            selectorName = "RandomWeighted",
            evaluatedModules = new List<ModuleEvaluation>(),
            timestamp = Time.time
        };
        #endif
        
        // 실행 가능한 모듈 필터링
        List<BehaviorModule> available = new List<BehaviorModule>();
        
        foreach (var module in candidates)
        {
            bool canExec = module.CanExecute(bb);
            bool isRepeat = preventRepeat && module == lastSelected && candidates.Count > 1;
            
            #if UNITY_EDITOR
            trace.evaluatedModules.Add(new ModuleEvaluation
            {
                moduleName = module.ModuleName,
                canExecute = canExec && !isRepeat,
                priority = module.Priority,
                rejectReason = !canExec ? "CanExecute=false" : (isRepeat ? "Repeat" : "")
            });
            #endif
            
            if (canExec && !isRepeat)
            {
                available.Add(module);
            }
        }
        
        if (available.Count == 0)
        {
            #if UNITY_EDITOR
            trace.chosenModule = "None";
            trace.reason = "사용 가능한 모듈 없음";
            LastDecision = trace;
            #endif
            return null;
        }
        
        // 우선순위를 가중치로 사용하여 랜덤 선택
        float totalWeight = 0f;
        foreach (var m in available)
        {
            totalWeight += m.Priority;
        }
        
        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        BehaviorModule selected = available[0];
        
        foreach (var m in available)
        {
            cumulative += m.Priority;
            if (random <= cumulative)
            {
                selected = m;
                break;
            }
        }
        
        lastSelected = selected;
        
        #if UNITY_EDITOR
        trace.chosenModule = selected.ModuleName;
        trace.reason = $"가중치 랜덤 (총 {totalWeight:F0}, 선택 {random:F1})";
        LastDecision = trace;
        #endif
        
        return selected;
    }
}
