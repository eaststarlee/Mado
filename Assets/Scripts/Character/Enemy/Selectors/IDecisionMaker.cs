using System.Collections.Generic;

/// <summary>
/// 행동 선택 인터페이스.
/// Selector는 "전략", Module.CanExecute()는 "가능 여부"를 담당.
/// </summary>
public interface IDecisionMaker
{
    /// <summary>
    /// 후보 모듈 목록에서 최적 모듈 1개를 선택.
    /// </summary>
    /// <param name="candidates">실행 가능한 후보 모듈 목록.</param>
    /// <param name="bb">블랙보드 (현재 상태 참조).</param>
    /// <returns>선택된 모듈. 없으면 null.</returns>
    BehaviorModule SelectNext(List<BehaviorModule> candidates, EnemyBlackboard bb);
    
#if UNITY_EDITOR
    /// <summary>
    /// 마지막 결정 추적 정보 (Editor 전용).
    /// </summary>
    DecisionTrace LastDecision { get; }
#endif
}

#if UNITY_EDITOR
/// <summary>
/// 디버그용 결정 추적 정보.
/// 모든 선택에 "왜?"가 추적 가능해야 한다.
/// </summary>
public struct DecisionTrace
{
    /// <summary>Selector 이름.</summary>
    public string selectorName;
    /// <summary>평가된 모듈 목록 (이름 + 결과).</summary>
    public List<ModuleEvaluation> evaluatedModules;
    /// <summary>선택된 모듈 이름.</summary>
    public string chosenModule;
    /// <summary>선택 이유.</summary>
    public string reason;
    /// <summary>결정 시점 (Time.time).</summary>
    public float timestamp;
}

/// <summary>
/// 개별 모듈 평가 결과.
/// </summary>
public struct ModuleEvaluation
{
    public string moduleName;
    public bool canExecute;
    public int priority;
    public string rejectReason;
}
#endif
