using System.Collections;
using UnityEngine;

/// <summary>
/// 파괴 가능한 오브젝트(DestructibleEntity)의 시각적/청각적 피드백을 전담하는 컴포넌트입니다.
/// 로직과 연출을 완벽하게 분리하기 위해 고안되었습니다.
/// </summary>
[RequireComponent(typeof(DestructibleEntity))]
public class DestructibleFeedback : MonoBehaviour
{
    [Header("피격 효과")]
    public Color hitFlashColor = new Color(1f, 0.3f, 0.3f, 1f);
    public float hitFlashDuration = 0.1f;
    
    [Header("진동 효과")]
    public float shakeIntensity = 0.05f;
    public float shakeDuration = 0.15f;

    [Header("파괴 효과")]
    [Tooltip("파괴 시 생성할 파티클 프리팹")]
    public GameObject destroyVfxPrefab;

    private SpriteRenderer spriteRenderer;
    private DestructibleEntity entity;
    private Vector3 originalPosition;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        entity = GetComponent<DestructibleEntity>();
        originalPosition = transform.localPosition;

        // 이벤트 동적 연결
        if (entity != null)
        {
            entity.OnHit.AddListener(PlayHitFeedback);
            entity.OnDestroyed.AddListener(PlayDestroyFeedback);
        }
    }

    private void OnDestroy()
    {
        if (entity != null)
        {
            entity.OnHit.RemoveListener(PlayHitFeedback);
            entity.OnDestroyed.RemoveListener(PlayDestroyFeedback);
        }
    }

    public void PlayHitFeedback()
    {
        // 이미 연출 중이면 멈추고 다시 시작 (리셋)
        StopAllCoroutines(); 
        transform.localPosition = originalPosition;
        
        StartCoroutine(HitFlashRoutine());
        StartCoroutine(ShakeRoutine());
    }

    public void PlayDestroyFeedback()
    {
        if (destroyVfxPrefab != null)
        {
            Instantiate(destroyVfxPrefab, transform.position, Quaternion.identity);
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        
        // 깜빡임 후 복구
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
            float offsetY = Random.Range(-shakeIntensity, shakeIntensity);
            transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
    }
}
