using UnityEngine;

/// <summary>
/// 모듈별 런타임 임시 데이터 컨테이너.
/// Enter 시 생성/초기화, Exit 시 파괴.
/// 모듈 내부 상태를 여기에 저장하여 Stateless 원칙 준수.
/// </summary>
public class ModuleRuntimeContext
{
    /// <summary>
    /// 모듈 실행 경과 시간.
    /// </summary>
    public float ElapsedTime { get; set; }
    
    /// <summary>
    /// 초기화 완료 여부.
    /// </summary>
    public bool IsInitialized { get; set; }
    
    /// <summary>
    /// 시작 시점 (Time.time).
    /// </summary>
    public float StartTime { get; set; }

    // --- Reaction 모듈용 ---
    
    /// <summary>
    /// 저장된 DamageInfo (HitReaction 등에서 사용).
    /// </summary>
    public DamageInfo? StoredDamageInfo { get; set; }
    
    /// <summary>
    /// 넉백 초기 벡터 (HitReaction에서 사용).
    /// </summary>
    public Vector2 InitialKnockback { get; set; }
    
    /// <summary>
    /// 넉백/스턴 지속 시간.
    /// </summary>
    public float Duration { get; set; }
    
    /// <summary>
    /// 넉백 타이머.
    /// </summary>
    public float KnockbackTimer { get; set; }
    
    // --- Walk 모듈용 ---
    
    /// <summary>
    /// 현재 순찰 대기 중인지.
    /// </summary>
    public bool IsWaiting { get; set; }
    
    /// <summary>
    /// 순찰 대기 타이머.
    /// </summary>
    public float WaitTimer { get; set; }

    /// <summary>
    /// 장애물 감지 지속 타이머 (버퍼링용).
    /// </summary>
    public float ObstacleDetectionTimer { get; set; }
    
    /// <summary>
    /// 현재 이동 방향 (1 또는 -1).
    /// </summary>
    public int PatrolDirection { get; set; } = 1;

    // --- Combat 모듈용 ---
    
    /// <summary>
    /// 현재 공격 페이즈 (전조/활성/후딜).
    /// </summary>
    public AttackPhase CurrentAttackPhase { get; set; }
    
    /// <summary>
    /// 히트박스가 활성화되었는지 (애니메이션 이벤트 기반).
    /// </summary>
    public bool HitboxActive { get; set; }
    
    /// <summary>
    /// 공격이 적중했는지 (중복 방지).
    /// </summary>
    public bool HasAttacked { get; set; }

    // --- Dash 모듈용 ---
    
    /// <summary>
    /// 현재 대시 페이즈.
    /// </summary>
    public DashPhase CurrentDashPhase { get; set; }

    /// <summary>
    /// 모든 런타임 데이터 초기화.
    /// </summary>
    public void Reset()
    {
        ElapsedTime = 0f;
        IsInitialized = false;
        StartTime = Time.time;
        StoredDamageInfo = null;
        InitialKnockback = Vector2.zero;
        Duration = 0f;
        KnockbackTimer = 0f;
        IsWaiting = false;
        WaitTimer = 0f;
        ObstacleDetectionTimer = 0f;
        PatrolDirection = 1;
        CurrentAttackPhase = AttackPhase.None;
        HitboxActive = false;
        HasAttacked = false;
        CurrentDashPhase = DashPhase.None;
    }
}

/// <summary>
/// 공격 모듈의 실행 단계.
/// </summary>
public enum AttackPhase
{
    None,
    PreDelay,      // 뜸들이기 (preAttackDelay 적용 시)
    Anticipation,  // 전조 (애니메이션 시작 ~ 히트박스 활성)
    Active,        // 활성 (히트박스 ON)
    Recovery       // 후딜 (히트박스 OFF ~ 모듈 완료)
}

/// <summary>
/// 대시 모듈의 실행 단계.
/// </summary>
public enum DashPhase
{
    None,
    Prepare,   // 전조 (준비 동작)
    Dashing,   // 돌진
    Recovery   // 감속
}
