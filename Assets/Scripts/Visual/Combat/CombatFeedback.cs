using System.Collections;
using UnityEngine;

/// <summary>
/// 히트스탑, 카메라 흔들림 등 연출 전담
/// 전투 로직(HitResolver)와 완전 분리
/// </summary>
public class CombatFeedback : MonoBehaviour
{
    public static CombatFeedback Instance { get; private set; }
    
    [Header("히트스탑")]
    [SerializeField] private bool enableHitStop = true;
    
    [Header("카메라 흔들림")]
    [SerializeField] private bool enableScreenShake = true;
    
    // 히트스탑 중첩 방지
    private bool isHitStopping;
    private Coroutine hitStopCoroutine;
    
    private void Awake()
    {
        // 싱글톤 (DontDestroy 아님 - 씬별로 존재 가능)
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 적중 시 피드백 트리거
    /// </summary>
    public void TriggerHitFeedback(AttackData attack)
    {
        if (attack == null) return;
        
        // 히트스탑
        if (enableHitStop && attack.hitStopDuration > 0)
        {
            RequestHitStop(attack.hitStopDuration);
        }
        
        // 카메라 흔들림
        if (enableScreenShake && attack.screenShakeMagnitude > 0)
        {
            RequestScreenShake(attack.screenShakeMagnitude);
        }
        
        // 이벤트 발생 (VFX/SFX)
        CombatEvents.RaiseHit(attack);
    }
    
    /// <summary>
    /// 히트스탑 요청 (중첩 시 갱신)
    /// </summary>
    public void RequestHitStop(float duration)
    {
        // 이미 히트스탑 중이면 갱신
        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
        }
        
        hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
    }
    
    private IEnumerator HitStopRoutine(float duration)
    {
        isHitStopping = true;
        Time.timeScale = 0f;
        
        yield return new WaitForSecondsRealtime(duration);
        
        Time.timeScale = 1f;
        isHitStopping = false;
        hitStopCoroutine = null;
    }
    
    /// <summary>
    /// 카메라 흔들림 요청
    /// </summary>
    public void RequestScreenShake(float magnitude)
    {
        // TODO: CameraShake 컴포넌트 연동
        // 현재는 플레이스홀더
        // cameraShake?.Shake(magnitude);
    }
    
    /// <summary>
    /// 즉시 히트스탑 취소 (씬 전환 등)
    /// </summary>
    public void CancelHitStop()
    {
        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
            hitStopCoroutine = null;
        }
        
        if (isHitStopping)
        {
            Time.timeScale = 1f;
            isHitStopping = false;
        }
    }
    
    // ==================== Event Subscriptions ====================
    
    private void OnEnable()
    {
        PlayerEvents.OnParrySuccess += HandleParrySuccess;
    }

    private void OnDisable()
    {
        PlayerEvents.OnParrySuccess -= HandleParrySuccess;
    }

    private void HandleParrySuccess(DamageInfo info)
    {
        // 최적화를 위해 PlayerController는 싱글톤/매니저에서 캐싱해두거나,
        // ActiveFormData.parry.successHitStopDuration 등을 사용할 수 있습니다.
        // 현재는 임시로 FindFirstObjectByType 사용 (추후 Manager를 통해 획득 권장)
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.ActiveFormData != null)
        {
            float hitStop = player.ActiveFormData.parry.successHitStopDuration;
            float screenShake = player.ActiveFormData.parry.successScreenShakePower;
            
            if (enableHitStop && hitStop > 0f) RequestHitStop(hitStop);
            if (enableScreenShake && screenShake > 0f) RequestScreenShake(screenShake);
            
            // TODO: 패링 성공 특유의 '챙!' 사운드 및 스파크 이펙트 재생
            // AudioManager.PlaySound("ParrySuccess");
            // FXManager.PlayEffect("ParrySpark", info.hitPoint);
            
            Debug.Log("[CombatFeedback] Parry Success Effects Triggered!");
        }
    }
    
    private void OnDestroy()
    {
        // 파괴 시 TimeScale 복구 보장
        if (isHitStopping)
        {
            Time.timeScale = 1f;
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
