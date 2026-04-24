using UnityEngine;

/// <summary>
/// 조건부 행동 평가기. 매 N프레임 폴링.
/// HP 기반 페이즈 전환 시 Brain에 모듈 추가/제거.
/// EnemyHealth 이벤트에 결합하지 않음 — 폴링 기반 독립 컴포넌트.
/// </summary>
public class ConditionalBehaviorEvaluator
{
    private EnemyEntity entity;
    private EnemyHealth health;
    private EnemyBlackboard blackboard;
    private EnemyBrain brain;
    
    /// <summary>
    /// 평가 주기 (프레임 단위). 기본 10프레임.
    /// </summary>
    private int evaluateInterval = 10;
    private int frameCounter = 0;
    
    /// <summary>
    /// 마지막 평가된 페이즈.
    /// </summary>
    private int lastEvaluatedPhase = 0;
    
    public ConditionalBehaviorEvaluator(EnemyEntity entity, EnemyHealth health, EnemyBlackboard blackboard, EnemyBrain brain, int evaluateInterval = 10)
    {
        this.entity = entity;
        this.health = health;
        this.blackboard = blackboard;
        this.brain = brain;
        this.evaluateInterval = evaluateInterval;
    }
    
    /// <summary>
    /// 매 프레임 호출되지만, 실제 평가는 N프레임마다.
    /// </summary>
    public void Tick()
    {
        frameCounter++;
        if (frameCounter < evaluateInterval) return;
        frameCounter = 0;
        
        EvaluatePhaseTransition();
    }
    
    /// <summary>
    /// HP 기반 페이즈 전환 평가.
    /// PhaseContext.phaseHealthThresholds[0] = 0.7f → HP 70% 이하 시 Phase 1
    /// </summary>
    private void EvaluatePhaseTransition()
    {
        if (health == null) return;
        
        float[] thresholds = blackboard.Phase.phaseHealthThresholds;
        if (thresholds == null || thresholds.Length == 0) return;
        
        float healthPercent = health.CurrentHealth / health.MaxHealth;
        
        // 최고 달성 페이즈 결정 (한번 올라간 페이즈는 내려오지 않음)
        int newPhase = 0;
        for (int i = 0; i < thresholds.Length; i++)
        {
            if (healthPercent <= thresholds[i])
            {
                newPhase = i + 1;
            }
        }
        
        // 페이즈 변경 감지
        if (newPhase > lastEvaluatedPhase)
        {
            int previousPhase = lastEvaluatedPhase;
            lastEvaluatedPhase = newPhase;
            blackboard.Phase.currentPhase = newPhase;
            
            // 페이즈 전환 알림 (Brain이 모듈 교체에 사용)
            OnPhaseChanged(previousPhase, newPhase);
        }
    }
    
    /// <summary>
    /// 페이즈 전환 시 호출. 하위 클래스나 외부에서 오버라이드/확장 가능.
    /// 기본 구현: 로그만 출력.
    /// </summary>
    protected virtual void OnPhaseChanged(int oldPhase, int newPhase)
    {
        Debug.Log($"[ConditionalBehaviorEvaluator] Phase {oldPhase} → {newPhase}");
        
        // 향후 확장: EnemyDefinition에 ConditionalModuleSet[] 정의 시
        // Brain.AddModule() / Brain.RemoveModule() 로직 추가
    }
    
    /// <summary>
    /// 현재 페이즈 반환.
    /// </summary>
    public int CurrentPhase => lastEvaluatedPhase;
}
