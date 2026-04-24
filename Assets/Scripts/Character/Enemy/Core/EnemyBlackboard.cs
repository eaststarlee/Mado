using UnityEngine;

/// <summary>
/// 강타입 공유 메모리. Sensor는 쓰기만, Module은 읽기만 한다.
/// Dictionary<string, object> 대신 Context 구조체를 사용하여 GC Free 보장.
/// </summary>
public class EnemyBlackboard
{
    // --- Contexts ---
    public TargetContext Target;
    public MovementContext Movement;
    public CombatContext Combat;
    public PhaseContext Phase;
    
    // --- Status Flags (BitMask, GC Free) ---
    private int statusFlags;
    
    public void Reset()
    {
        Target = default;
        Movement = default;
        Combat = default;
        Phase = default;
        statusFlags = 0;
    }
    
    // --- StatusFlag 비트 연산 ---
    public bool HasFlag(StatusFlag flag) => (statusFlags & (int)flag) != 0;
    public void SetFlag(StatusFlag flag) => statusFlags |= (int)flag;
    public void ClearFlag(StatusFlag flag) => statusFlags &= ~(int)flag;
    public void ClearAllFlags() => statusFlags = 0;
}

/// <summary>
/// 플레이어(타겟) 관련 데이터.
/// </summary>
/// <summary>
/// 플레이어(타겟) 관련 데이터.
/// </summary>
[System.Serializable]
public struct TargetContext
{
    /// <summary>현재 타겟 (null이면 추적 안함).</summary>
    public Transform target;
    
    /// <summary>타겟까지의 거리 (센서가 갱신).</summary>
    public float distance;
    /// <summary>타겟 방향 (정규화, 센서가 갱신).</summary>
    public Vector2 direction;
    
    /// <summary>감지된 경로 (Flags).</summary>
    public DetectionSource source;

    // --- 상태 판정 (Queries - Computed Properties) ---
    /// <summary>감지 중인가? (Source가 하나라도 있으면 true).</summary>
    public bool IsDetected => source != DetectionSource.None;
    
    /// <summary>직접 눈으로 보고 있는가?</summary>
    public bool IsVisible => source.HasFlag(DetectionSource.Vision);

    // --- 독립 필드 ---
    /// <summary>공격 사거리 내 여부 (AttackableRangeSensor 전용).</summary>
    public bool isInMeleeRange;
    /// <summary>중거리 사거리 내 여부 (옵션).</summary>
    public bool isInMidRange;

    // --- 메모리 (Memory) ---
    /// <summary>마지막으로 감지된 위치.</summary>
    public Vector2 lastKnownPosition;
    /// <summary>마지막으로 감지된 시간 (감지 중일 때 계속 갱신).</summary>
    public float lastDetectedTime;
    /// <summary>감지가 끊긴 시점의 시간.</summary>
    public float lostTime;

    /// <summary>놓친 지 얼마나 지났나?</summary>
    public float TimeSinceLost => Time.time - lostTime;

    /// <summary>
    /// Decay 로직: 모든 감지가 끊겼을 때 일정 시간 후 타겟 소실 처리.
    /// </summary>
    public void UpdateDecay(float memoryDuration)
    {
        if (target == null) return;

        if (source != DetectionSource.None)
        {
            // 감지 중: 계속 갱신
            lastDetectedTime = Time.time;
            lostTime = 0f; 
        }
        else
        {
            // 막 감지가 끊긴 순간
            if (lostTime == 0f)
            {
                lostTime = Time.time;
            }

            // 기억 시간 초과 시 완전 망각
            if (Time.time - lostTime > memoryDuration)
            {
                target = null;
                isInMeleeRange = false;
                // isVisible 등은 source에서 파생되므로 자동 처리됨
            }
        }
    }
}

/// <summary>
/// 이동 관련 데이터. GroundSensor가 갱신 (변경 없음).
/// </summary>
[System.Serializable]
public struct MovementContext
{
    /// <summary>지면 접촉 여부.</summary>
    public bool isGrounded;
    /// <summary>전방 벽 감지.</summary>
    public bool wallAhead;
    /// <summary>전방 낭떠러지 감지.</summary>
    public bool ledgeAhead;
    /// <summary>현재 바라보는 방향 (1 = 오른쪽, -1 = 왼쪽).</summary>
    public int facingDirection;
}

/// <summary>
/// 전투 관련 데이터.
/// </summary>
[System.Serializable]
public struct CombatContext
{
    /// <summary>마지막 공격 시간.</summary>
    public float lastAttackTime;
    /// <summary>콤보 카운트.</summary>
    public int comboCount;
    /// <summary>패링 카운트.</summary>
    public int parryCount;
}

/// <summary>
/// 보스 페이즈 관련 데이터.
/// </summary>
[System.Serializable]
public struct PhaseContext
{
    /// <summary>현재 페이즈.</summary>
    public int currentPhase;
    /// <summary>페이즈 전환 체력 임계값.</summary>
    public float[] phaseHealthThresholds;
}

/// <summary>
/// 불리언 상태 비트마스크. GC Free.
/// </summary>
[System.Flags]
public enum StatusFlag
{
    None        = 0,
    IsEnraged   = 1 << 0,
    IsShielded  = 1 << 1,
    IsCharging  = 1 << 2,
    IsStunned   = 1 << 3,
    IsDead      = 1 << 4,
    IsHit       = 1 << 5,
}
