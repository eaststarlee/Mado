using System;

/// <summary>
/// 플레이어 관련 이벤트를 관리하는 정적 클래스
/// </summary>
public static class PlayerEvents
{
    /// <summary>
    /// 활공 시작 시 발생하는 이벤트
    /// </summary>
    public static event Action OnGlideStart;
    
    /// <summary>
    /// 활공 종료 시 발생하는 이벤트
    /// </summary>
    public static event Action OnGlideEnd;
    
    /// <summary>
    /// 활공 시작 이벤트 발생
    /// </summary>
    public static void RaiseGlideStart()
    {
        OnGlideStart?.Invoke();
    }
    
    /// <summary>
    /// 활공 종료 이벤트 발생
    /// </summary>
    public static void RaiseGlideEnd()
    {
        OnGlideEnd?.Invoke();
    }
    
    /// <summary>
    /// 폼 변경 시 발생하는 이벤트
    /// </summary>
    public static event Action<FormType> OnFormChanged;
    
    /// <summary>
    /// 폼 변경 이벤트 발생
    /// </summary>
    public static void RaiseFormChanged(FormType form)
    {
        OnFormChanged?.Invoke(form);
    }
    
    // ==================== Health Events ====================
    
    /// <summary>
    /// 체력 변경 시 발생하는 이벤트 (현재 체력, 최대 체력)
    /// </summary>
    public static event Action<int, int> OnHealthChanged;
    
    /// <summary>
    /// 피격 시 발생하는 이벤트
    /// </summary>
    public static event Action OnPlayerHit;
    
    /// <summary>
    /// 사망 시 발생하는 이벤트
    /// </summary>
    public static event Action OnPlayerDeath;
    
    /// <summary>
    /// 무적 상태 변경 시 발생하는 이벤트 (무적 여부)
    /// </summary>
    public static event Action<bool> OnInvincibilityChanged;
    
    /// <summary>
    /// 체력 변경 이벤트 발생
    /// </summary>
    public static void RaiseHealthChanged(int currentHealth, int maxHealth)
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// 피격 이벤트 발생
    /// </summary>
    public static void RaisePlayerHit()
    {
        OnPlayerHit?.Invoke();
    }
    
    /// <summary>
    /// 사망 이벤트 발생
    /// </summary>
    public static void RaisePlayerDeath()
    {
        OnPlayerDeath?.Invoke();
    }
    
    /// <summary>
    /// 무적 상태 변경 이벤트 발생
    /// </summary>
    public static void RaiseInvincibilityChanged(bool isInvincible)
    {
        OnInvincibilityChanged?.Invoke(isInvincible);
    }
    
    // ==================== Parry Events ====================
    
    /// <summary>
    /// 패링 성공 시 발생하는 이벤트 (데미지 정보 전달)
    /// </summary>
    public static event Action<DamageInfo> OnParrySuccess;
    
    /// <summary>
    /// 패링 성공 이벤트 발생
    /// </summary>
    public static void RaiseParrySuccess(DamageInfo info)
    {
        OnParrySuccess?.Invoke(info);
    }
    // ==================== Skill Gauge Events ====================
    
    /// <summary>
    /// 스킬 게이지 변경 시 발생하는 이벤트 (현재 게이지, 최대 게이지)
    /// </summary>
    public static event Action<int, int> OnSkillGaugeChanged;
    
    /// <summary>
    /// 스킬 게이지 변경 이벤트 발생
    /// </summary>
    public static void RaiseSkillGaugeChanged(int currentGauge, int maxGauge)
    {
        OnSkillGaugeChanged?.Invoke(currentGauge, maxGauge);
    }

    // ==================== Grapple Events ====================

    /// <summary>
    /// 그래플링 대쉬 시작 시 발생
    /// </summary>
    public static event Action OnGrappleStart;

    /// <summary>
    /// 그래플링 대쉬 종료 시 발생 (Exit 기준)
    /// </summary>
    public static event Action OnGrappleEnd;

    public static void RaiseGrappleStart() => OnGrappleStart?.Invoke();
    public static void RaiseGrappleEnd()   => OnGrappleEnd?.Invoke();
}
