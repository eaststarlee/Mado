using UnityEngine;

/// <summary>
/// Type of hit for Poise calculation.
/// </summary>
public enum HitType
{
    Light,      // Low poise damage
    Heavy,      // High poise damage
    Special,    // Very high poise damage
    Grab        // Ignores poise (instantly breaks)
}

public enum DamageType
{
    Physical,
    Magic,
    Environment,
    TrueDamage
}

/// <summary>
/// Comprehensive damage information passed to IDamageable.
/// </summary>
[System.Serializable]
public struct DamageInfo
{
    [Header("Basic")]
    public float damage;            // Amount of HP damage
    public Vector2 hitPoint;        // Point of impact
    public Vector2 hitDirection;    // Direction of impact (normalized)
    public Vector2 damageSource;    // Origin of damage (for knockback calculation if direction is missing)

    [Header("Physics")]
    public Vector2 knockbackForce;  // Force vector (X: Push, Y: Lift)
    public float stunDuration;      // Duration of hit stun/stop

    [Header("Poise")]
    public HitType hitType;         // Type of impact
    public float poiseDamage;       // Specific poise damage (0 = use HitType default)
    public DamageType damageType;   // Type of damage (Physical, Magic, etc)

    [Header("Flags")]
    public bool ignoreInvincibility; // Hits through i-frames
    public bool ignoreArmor;         // Hits through SuperArmor
    public bool canBeParried;        // [New] Indicates if this attack can be parried
    
    [Header("Source")]
    public GameObject source;        // Who caused the damage

    /// <summary>
    /// Helper to create a Light hit (Fast, low recoil).
    /// </summary>
    public static DamageInfo CreateLight(float damage, Vector2 point, Vector2 direction, GameObject source = null)
    {
        return new DamageInfo
        {
            damage = damage,
            hitPoint = point,
            hitDirection = direction,
            knockbackForce = new Vector2(5f, 0f),
            stunDuration = 0.2f,
            hitType = HitType.Light,
            damageType = DamageType.Physical,
            source = source,
            ignoreInvincibility = false,
            ignoreArmor = false,
            canBeParried = true
        };
    }

    /// <summary>
    /// Helper to create a Heavy hit (Slow, high recoil).
    /// </summary>
    public static DamageInfo CreateHeavy(float damage, Vector2 point, Vector2 direction, GameObject source = null)
    {
        return new DamageInfo
        {
            damage = damage,
            hitPoint = point,
            hitDirection = direction,
            knockbackForce = new Vector2(12f, 0f),
            stunDuration = 0.5f,
            hitType = HitType.Heavy,
            damageType = DamageType.Physical,
            source = source,
            ignoreInvincibility = false,
            ignoreArmor = false,
            canBeParried = true
        };
    }
}
