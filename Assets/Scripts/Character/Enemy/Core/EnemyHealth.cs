using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyDefinition definition;

    private CombatProfile combat => definition.CombatSettings;

    private float currentHealth;
    private float currentPoise;
    private float lastHitTime;
    
    // Optional dependency, can be null
    private EnemyInvincibility invincibility;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => combat.maxHealth;
    public float CurrentPoise => currentPoise;
    public float MaxPoise => combat.maxPoise;
    public bool CanBeStunned => currentPoise <= 0f;
    public bool IsRecoveringPoise => Time.time - lastHitTime > combat.poiseRecoveryDelay;
    
    // Explicit Invincibility Interface
    public bool IsInvincible
    {
        get
        {
            if (invincibility != null) return invincibility.IsInvincible;
            return false;
        }
    }

    // Events
    public event Action<DamageInfo> OnHit;
    public event Action<DamageInfo> OnHitEffectOnly; // Triggered even if invincible
    public event Action OnPoiseBreak;
    public event Action<DamageInfo> OnDeath;

    private void Awake()
    {
        invincibility = GetComponent<EnemyInvincibility>();
        if (definition != null)
        {
            ResetHealth();
        }
    }

    private void Start()
    {
        // Ensure data is initialized
        if (currentHealth <= 0 && definition != null) ResetHealth();
    }

    public void ResetHealth()
    {
        if (definition == null) return;
        currentHealth = combat.maxHealth;
        currentPoise = combat.maxPoise;
        lastHitTime = -999f;
    }

    /// <summary>
    /// Immediately restore full poise (e.g., after Stun ends)
    /// </summary>
    public void ResetPoise()
    {
        if (definition == null) return;
        currentPoise = combat.maxPoise;
        lastHitTime = Time.time; // Prevent immediate regen logic from acting weirdly
    }

    private void Update()
    {
        if (definition == null) return;

        // Auto-regenerate Poise
        if (currentPoise < combat.maxPoise && IsRecoveringPoise)
        {
            currentPoise = Mathf.Min(
                currentPoise + combat.poiseRecoveryRate * Time.deltaTime,
                combat.maxPoise
            );
        }
    }

    public void TakeDamage(DamageInfo info)
    {
        if (definition == null)
        {
            Debug.LogError("[EnemyHealth] No EnemyDefinition assigned!");
            return;
        }

        // Check Invincibility
        bool isInvincible = !info.ignoreInvincibility && IsInvincible;

        if (isInvincible)
        {
            OnHitEffectOnly?.Invoke(info);
            return;
        }

        // Apply Damage
        currentHealth -= info.damage;

        // Apply Poise Damage
        float actualPoiseDamage = info.poiseDamage > 0
            ? info.poiseDamage
            : combat.poiseDamageTable.GetPoiseDamage(info.hitType);

        currentPoise -= actualPoiseDamage;
        
        // Clamp immediately to prevent negative values in logs/UI
        if (currentPoise < 0f) currentPoise = 0f;

        lastHitTime = Time.time;

        // Check Poise Break (PRIORITY: Check this first so StunState can be set)
        if (currentPoise <= 0f && actualPoiseDamage > 0)
        {
            OnPoiseBreak?.Invoke();
        }

        // Trigger Hit Event
        OnHit?.Invoke(info);

        // Check Death
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            OnDeath?.Invoke(info);
        }
        else
        {
             // Start invincibility if not dead
             invincibility?.StartInvincibility(combat.invincibilityDuration);
        }
    }

    // Debug / Test methods
    [ContextMenu("Test Damage (Light)")]
    public void TestLightDamage()
    {
        var info = DamageInfo.CreateLight(10f, transform.position, Vector2.right, gameObject);
        TakeDamage(info);
    }
}
