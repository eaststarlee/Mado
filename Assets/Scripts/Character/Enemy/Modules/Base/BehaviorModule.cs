using UnityEngine;

/// <summary>
/// 모듈이 소속된 실행 채널.
/// Movement + Action은 병렬 실행 가능, Interrupt는 모든 채널 강제 중단.
/// </summary>
public enum ModuleChannel
{
    Movement,   // 하체 (Walk, Fly, Dash, Jump)
    Action,     // 상체 (MeleeSwing, Shoot, Block)
    Interrupt   // 전신 (Hit, Stun, Death — 모든 채널 중단)
}

/// <summary>
/// 모듈의 현재 실행 상태.
/// </summary>
public enum ModuleState
{
    Inactive,
    Active,
    Complete
}

/// <summary>
/// 모든 행동 모듈의 추상 베이스 클래스.
/// 
/// 설계 원칙:
/// - Module은 "의도(Intent)"만 발행하고, 직접 실행하지 않는다.
/// - Module은 Stateless를 기본으로 한다 (entity, blackboard 캐싱 금지).
/// - 모든 실행 상태는 Blackboard, ModuleData(SO), ModuleRuntimeContext에만 저장.
/// </summary>
public abstract class BehaviorModule
{
    // --- 식별 ---
    
    /// <summary>
    /// 이 모듈이 소속된 채널.
    /// </summary>
    public abstract ModuleChannel Channel { get; }
    
    /// <summary>
    /// 우선순위 (Abort 판단용). 높을수록 우선.
    /// </summary>
    public virtual int Priority => 0;
    
    /// <summary>
    /// 모듈 이름 (디버그용).
    /// </summary>
    public virtual string ModuleName => GetType().Name;
    
    // --- 상태 ---
    
    /// <summary>
    /// 현재 모듈 실행 상태.
    /// </summary>
    public ModuleState State { get; protected set; } = ModuleState.Inactive;
    
    // --- 핵심 라이프사이클 ---
    
    /// <summary>
    /// 물리적 실행 가능 여부 (쿨타임, 지면 등).
    /// Selector가 호출하기 전에 체크.
    /// </summary>
    public abstract bool CanExecute(EnemyBlackboard bb);
    
    /// <summary>
    /// 모듈 시작. 참조는 파라미터로 받고 캐싱하지 않음 (Stateless 원칙).
    /// </summary>
    public virtual void Enter(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
    {
        State = ModuleState.Active;
        // context.Reset(); // ⚠️ 제거: InterruptLayer에서 데이터 설정 후 진입하므로 여기서 초기화하면 안 됨.
    }
    
    /// <summary>
    /// 매 프레임 실행. System에 명령 발행.
    /// </summary>
    public abstract void Execute(float deltaTime, EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context);
    
    /// <summary>
    /// 모듈 종료. 정리 작업.
    /// </summary>
    public virtual void Exit(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
    {
        State = ModuleState.Inactive;
    }
    
    /// <summary>
    /// 작업 완료 여부.
    /// </summary>
    public virtual bool IsComplete() => State == ModuleState.Complete;
    
    /// <summary>
    /// 중단 허용 여부 (기본: true. 공격 전조 중엔 false 가능).
    /// </summary>
    public virtual bool CanBeInterrupted() => true;
    /// <summary>
    /// 디버그용 상태 문자열 반환.
    /// 구체적인 내부 상태(예: Patrol, Chase)를 표시하고 싶을 때 오버라이드.
    /// </summary>
    public virtual string GetStatus() => "";
}
