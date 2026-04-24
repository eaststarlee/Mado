using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 개별 하트 UI 컴포넌트 (fillAmount 기반 겹침 구조)
/// </summary>
public class HeartUI : MonoBehaviour
{
    [Header("Heart Images")]
    [SerializeField] private Image emptyHeartImage;  // 배경 (회색 하트)
    [SerializeField] private Image fullHeartImage;   // 전경 (빨간 하트)
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private bool enablePulseEffect = true;
    
    private float currentFillAmount = 1f;
    private Coroutine fillAnimationCoroutine;
    private Coroutine pulseCoroutine;
    
    /// <summary>
    /// 하트 fillAmount 설정 (0.0 ~ 1.0)
    /// </summary>
    /// <param name="amount">채울 양 (0.0 = 빈 하트, 0.5 = 반 칸, 1.0 = 가득 참)</param>
    /// <param name="animate">애니메이션 재생 여부</param>
    public void SetFillAmount(float amount, bool animate = false)
    {
        amount = Mathf.Clamp01(amount);
        
        if (animate)
        {
            // 기존 애니메이션 중단
            if (fillAnimationCoroutine != null)
            {
                StopCoroutine(fillAnimationCoroutine);
            }
            
            fillAnimationCoroutine = StartCoroutine(AnimateFillAmount(amount));
            
            // 펄스 효과 (선택)
            if (enablePulseEffect && amount != currentFillAmount)
            {
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                }
                pulseCoroutine = StartCoroutine(PulseEffect());
            }
        }
        else
        {
            // 즉시 적용
            fullHeartImage.fillAmount = amount;
            currentFillAmount = amount;
        }
    }
    
    /// <summary>
    /// fillAmount를 부드럽게 전환하는 애니메이션
    /// </summary>
    private IEnumerator AnimateFillAmount(float targetAmount)
    {
        float startAmount = fullHeartImage.fillAmount;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            // Ease Out Cubic 곡선 (부드러운 감속)
            t = 1f - Mathf.Pow(1f - t, 3f);
            
            fullHeartImage.fillAmount = Mathf.Lerp(startAmount, targetAmount, t);
            yield return null;
        }
        
        fullHeartImage.fillAmount = targetAmount;
        currentFillAmount = targetAmount;
    }
    
    /// <summary>
    /// 하트가 변경될 때 펄스 효과
    /// </summary>
    private IEnumerator PulseEffect()
    {
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = Vector3.one * 1.2f;
        
        float elapsed = 0f;
        float duration = 0.15f;
        
        // 커지기
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        elapsed = 0f;
        
        // 원래 크기로
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 초기화 (에디터에서 설정 확인)
    /// </summary>
    private void OnValidate()
    {
        if (fullHeartImage != null)
        {
            fullHeartImage.fillAmount = currentFillAmount;
        }
    }
}
