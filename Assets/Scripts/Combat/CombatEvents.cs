using System;

/// <summary>
/// 전투 관련 이벤트 (VFX/SFX/UI 전용)
/// 핵심 로직은 직접 호출, 이벤트는 연출용으로 제한
/// </summary>
public static class CombatEvents
{
    /// <summary>
    /// 적중 시 발생 (VFX/SFX 트리거용)
    /// </summary>
    public static event Action<AttackData> OnHit;
    
    /// <summary>
    /// 공격 시작 시 발생
    /// </summary>
    public static event Action<AttackData> OnAttackStart;
    
    /// <summary>
    /// 공격 중단 시 발생 (피격 등으로 인한 강제 취소)
    /// </summary>
    public static event Action OnAttackInterrupt;
    
    public static void RaiseHit(AttackData attack)
    {
        OnHit?.Invoke(attack);
    }
    
    public static void RaiseAttackStart(AttackData attack)
    {
        OnAttackStart?.Invoke(attack);
    }
    
    public static void RaiseAttackInterrupt()
    {
        OnAttackInterrupt?.Invoke();
    }
}
