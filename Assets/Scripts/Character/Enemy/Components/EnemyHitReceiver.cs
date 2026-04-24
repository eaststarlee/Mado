using UnityEngine;
using System.Collections;

/// <summary>
/// Handles visual and audio feedback for enemy hits.
/// Completely decoupled from logic (Health/Controller).
/// </summary>
public class EnemyHitReceiver : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private GameObject hitParticlePrefab; // Or string tag for pool
    [SerializeField] private string hitParticleTag = "HitParticle";
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.8f, 0.8f, 1f); // Red/White tint

    [Header("Audio")]
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip blockSound; // Sound when hitting invincible/shielded

    private EnemyHealth health;
    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;
    
    // Separate flash coroutine to ensure it doesn't conflict logic
    private Coroutine flashCoroutine;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnHit += HandleHit;
            health.OnHitEffectOnly += HandleHitEffectOnly;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHit -= HandleHit;
            health.OnHitEffectOnly -= HandleHitEffectOnly;
        }
    }

    private void HandleHit(DamageInfo info)
    {
        PlayEffects(info, false);
    }

    private void HandleHitEffectOnly(DamageInfo info)
    {
        PlayEffects(info, true);
    }

    private void PlayEffects(DamageInfo info, bool isBlocked)
    {
        // 1. Spawn Particle
        // Use ObjectPooler if available
        if (ObjectPooler.Instance != null && !string.IsNullOrEmpty(hitParticleTag))
        {
            GameObject particle = ObjectPooler.Instance.Spawn(hitParticleTag, info.hitPoint, Quaternion.identity);
            if (particle != null)
            {
                 // Rotation based on hit direction
                 float angle = Mathf.Atan2(info.hitDirection.y, info.hitDirection.x) * Mathf.Rad2Deg;
                 particle.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
        else if (hitParticlePrefab != null)
        {
            // Fallback instantiation
            Instantiate(hitParticlePrefab, info.hitPoint, Quaternion.identity);
        }

        // 2. Play Sound
        AudioClip clip = isBlocked ? blockSound : GetRandomHitSound();
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }

        // 3. Flash Effect
        // Only flash if not blocked (or maybe flash different color if blocked? sticking to plan)
        if (!isBlocked)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }
    }

    private AudioClip GetRandomHitSound()
    {
        if (hitSounds == null || hitSounds.Length == 0) return null;
        return hitSounds[Random.Range(0, hitSounds.Length)];
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", hitFlashColor);
            spriteRenderer.SetPropertyBlock(propertyBlock);

            yield return new WaitForSeconds(flashDuration);

            spriteRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", Color.white);
            spriteRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
