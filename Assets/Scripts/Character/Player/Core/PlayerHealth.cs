using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 플레이어 체력 관리 컴포넌트
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerHealth : MonoBehaviour, IDamageable, ISaveable
{
    // 무적 소스 정의 (확장 가능)
    public enum InvincibilitySource { None, Hit, SlamImpact, SlamDescent, Dash, Cutscene }

    [Header("References")]
    [SerializeField] private HealthData healthData;
    
    private PlayerController playerController;
    private SpriteRenderer spriteRenderer;
    
    // 체력 상태
    private int currentHealth;
    private int maxHealth;
    private bool isDead;
    
    // 무적 관리 시스템 (Source 기반)
    // Key: 무적 소스, Value: 남은 시간 (-1f는 무제한)
    private Dictionary<InvincibilitySource, float> invincibilityTimers = new Dictionary<InvincibilitySource, float>();
    
    // 시각 효과 코루틴 (단일 참조 유지)
    private Coroutine invincibilityCoroutine;
    
    // 레이어 충돌 관리
    private int playerLayer;
    
    #region Properties
    // 하나라도 무적 소스가 있으면 무적 상태
    public bool IsInvincible => invincibilityTimers.Count > 0;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerLayer = gameObject.layer;
        
        // 체력 초기화
        if (healthData != null)
        {
            maxHealth = healthData.maxHealth;
            currentHealth = healthData.startingHealth;
        }
        else
        {
            Debug.LogError("HealthData가 할당되지 않았습니다!");
            maxHealth = 5;
            currentHealth = 5;
        }

        // ISaveable 등록 (Awake에서 등록 — 타이밍 계약)
        SaveManager.Instance?.Register(this);
    }

    private void OnDestroy()
    {
        SaveManager.Instance?.Unregister(this);
    }
    
    private void Update()
    {
        UpdateInvincibilityTimers();
    }
    #endregion

    // ── ISaveable ────────────────────────────────────────

    public void OnSave(SaveData data)
    {
        data.currentHP = currentHealth;
        data.maxHP     = maxHealth;
    }

    public void OnLoad(SaveData data)
    {
        isDead     = false;
        maxHealth  = data.maxHP > 0 ? data.maxHP : maxHealth;
        currentHealth = data.currentHP > 0 ? data.currentHP : maxHealth;

        // 시각적 처리 복구
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color   = UnityEngine.Color.white;
        }

        // 무적 초기화
        invincibilityTimers.Clear();
        if (invincibilityCoroutine != null) { StopCoroutine(invincibilityCoroutine); invincibilityCoroutine = null; }
        SetEnemyCollision(true);

        PlayerEvents.RaiseHealthChanged(currentHealth, maxHealth);
    }
    
    #region IDamageable Implementation
    /// <summary>
    /// 데미지를 받는 메서드
    /// </summary>
    public void TakeDamage(DamageInfo damageInfo)
    {
        // 사망 상태면 무시
        if (isDead) return;
        
        // [New] 패링 판정 인터셉트
        if (playerController != null && playerController.TryParry(damageInfo))
        {
            // 패링 성공 시 다단 히트 방어용 무적 부여
            if (playerController.ActiveFormData != null)
            {
                SetInvincible(InvincibilitySource.Hit, playerController.ActiveFormData.parry.successInvincibilityDuration);
            }
            return; // 일반 데미지 및 피격 무시!
        }
        
        // 무적 중이면 무시 (ignoreInvincibility 예외)
        if (IsInvincible && !damageInfo.ignoreInvincibility)
        {
            return;
        }
        
        // 체력 감소
        currentHealth -= (int)damageInfo.damage;
        
        // 사망 체크
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
            return;
        }
        
        // 피격 처리
        OnHit(damageInfo);
    }
    #endregion
    
    #region Damage Handling
    /// <summary>
    /// 피격 처리
    /// </summary>
    private void OnHit(DamageInfo damageInfo)
    {
        // 피격 무적 설정 (Hit Source)
        if (healthData != null)
        {
            SetInvincible(InvincibilitySource.Hit, healthData.invincibilityDuration);
        }
        
        // PlayerController에 피격 알림 (State 전환 트리거)
        playerController.OnDamaged(damageInfo);
        
        // 이벤트 발생
        PlayerEvents.RaisePlayerHit();
        PlayerEvents.RaiseHealthChanged(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// 사망 처리
    /// </summary>
    private void Die()
    {
        isDead = true;
        
        // PlayerController에 사망 알림 (DeathState 전환)
        playerController.OnDeath();
        
        // 이벤트 발생
        PlayerEvents.RaisePlayerDeath();
        PlayerEvents.RaiseHealthChanged(currentHealth, maxHealth);
    }
    #endregion
    
    #region Invincibility Management (New Enum System)
    
    /// <summary>
    /// 무적 설정 (Source 기반)
    /// </summary>
    public void SetInvincible(InvincibilitySource source, float duration = -1f)
    {
        if (source == InvincibilitySource.None) return;

        bool wasInvincible = IsInvincible;

        // 1. 딕셔너리 갱신
        if (invincibilityTimers.ContainsKey(source))
        {
            float currentDuration = invincibilityTimers[source];
            if (currentDuration < 0f) { } // Current Infinite -> Keep
            else if (duration < 0f) invincibilityTimers[source] = duration; // New Infinite -> Override
            else invincibilityTimers[source] = Mathf.Max(currentDuration, duration); // Max
        }
        else
        {
            invincibilityTimers.Add(source, duration);
        }

        // 2. 시각 효과 (Flash) - 최초 진입 시 시작
        if (!wasInvincible && IsInvincible)
        {
            if (invincibilityCoroutine != null) StopCoroutine(invincibilityCoroutine);
            invincibilityCoroutine = StartCoroutine(InvincibilityFlashRoutine());
        }
        
        // 3. 물리 충돌 제어
        SetEnemyCollision(false); 
    }

    /// <summary>
    /// 무적 해제 (Source 기반)
    /// </summary>
    public void RemoveInvincible(InvincibilitySource source)
    {
        if (invincibilityTimers.ContainsKey(source))
        {
            invincibilityTimers.Remove(source);
        }

        // 더 이상 무적 소스가 없으면 효과 종료
        if (invincibilityTimers.Count == 0)
        {
            if (invincibilityCoroutine != null)
            {
                StopCoroutine(invincibilityCoroutine);
                invincibilityCoroutine = null;
            }
            
            // 스프라이트 복구 (안전장치)
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                spriteRenderer.enabled = true;
            }
            
            PlayerEvents.RaiseInvincibilityChanged(false);
            SetEnemyCollision(true);
        }
    }
    
    private IEnumerator InvincibilityFlashRoutine()
    {
        // 데이터에서 주기 및 색상 가져오기
        float interval = 0.1f;
        Color flashColor = new Color(1f, 1f, 1f, 0.5f); // 기본값
                         
        if (playerController != null && playerController.ActiveFormData != null && playerController.ActiveFormData.reaction != null)
        {
            var reaction = playerController.ActiveFormData.reaction;
            interval = reaction.flashDuration;
            flashColor = reaction.flashColor;
        }

        WaitForSeconds wait = new WaitForSeconds(interval);
        
        // 현재 색상 저장 (보통 white)
        Color originalColor = (spriteRenderer != null) ? Color.white : Color.white;
        
        // 깜빡임 루프
        bool isFlash = false;
        while (IsInvincible)
        {
            if (spriteRenderer != null)
            {
                // 번갈아가며 색상 변경
                spriteRenderer.color = isFlash ? originalColor : flashColor;
                isFlash = !isFlash;
            }
            yield return wait;
        }
    }
    


    private void UpdateInvincibilityTimers()
    {
        if (invincibilityTimers.Count == 0) return;

        // 제거할 키 수집 (순회 중 수정 방지)
        List<InvincibilitySource> keysToRemove = null;

        // 딕셔너리 키 복사본으로 순회
        foreach (var key in invincibilityTimers.Keys.ToList())
        {
            float time = invincibilityTimers[key];
            
            // 무제한(-1)은 패스
            if (time < 0f) continue;

            // 시간 감소
            time -= Time.deltaTime;
            invincibilityTimers[key] = time;

            // 시간 종료 체크
            if (time <= 0f)
            {
                if (keysToRemove == null) keysToRemove = new List<InvincibilitySource>();
                keysToRemove.Add(key);
            }
        }

        // 일괄 제거
        if (keysToRemove != null)
        {
            foreach (var key in keysToRemove)
            {
                RemoveInvincible(key);
            }
        }
    }

    /// <summary>
    /// 적 레이어와의 충돌 설정
    /// </summary>
    private void SetEnemyCollision(bool enable)
    {
        if (healthData == null) return;
        
        int targetLayerMask = healthData.enemyLayer;
        
        // ⭐ 안전장치: Ground와 Wall 레이어는 절대 무시하지 않도록 강제 제외
        int groundLayer = LayerMask.NameToLayer("Ground");
        int wallLayer = LayerMask.NameToLayer("Wall");
        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        
        if (groundLayer != -1) targetLayerMask &= ~(1 << groundLayer);
        if (wallLayer != -1) targetLayerMask &= ~(1 << wallLayer);

        if (groundLayer != -1) targetLayerMask &= ~(1 << groundLayer);
        if (wallLayer != -1) targetLayerMask &= ~(1 << wallLayer);

        for (int i = 0; i < 32; i++)
        {
            if ((targetLayerMask & (1 << i)) != 0)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, i, !enable);
            }
        }
    }

    private string LayerMaskToString(int mask)
    {
        string result = "";
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                if (result.Length > 0) result += ", ";
                result += LayerMask.LayerToName(i);
            }
        }
        return result.Length > 0 ? result : "None";
    }
    #endregion
    
    #region Public Methods
    /// <summary>
    /// 체력 회복
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        PlayerEvents.RaiseHealthChanged(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// 최대 체력 증가
    /// </summary>
    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth += amount; // 최대 체력 증가 시 현재 체력도 증가
        PlayerEvents.RaiseHealthChanged(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// 리스폰 시 체력 초기화
    /// </summary>
    public void ResetHealth()
    {
        isDead = false;
        
        // 모든 무적 소스 초기화
        invincibilityTimers.Clear();
        if (invincibilityCoroutine != null)
        {
            StopCoroutine(invincibilityCoroutine);
            invincibilityCoroutine = null;
        }

        if (healthData != null)
        {
            currentHealth = healthData.startingHealth;
        }
        
        // 스프라이트 표시 복구
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
        }
        
        // 물리 충돌 복구
        SetEnemyCollision(true);
        
        PlayerEvents.RaiseHealthChanged(currentHealth, maxHealth);
        PlayerEvents.RaiseInvincibilityChanged(false);
    }
    #endregion
    
    private void OnDisable()
    {
        if (invincibilityCoroutine != null)
        {
            StopCoroutine(invincibilityCoroutine);
            invincibilityCoroutine = null;
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.enabled = true;
        }
    }
}
