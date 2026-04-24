using System.Collections;
using UnityEngine;

/// <summary>
/// 화면 Fade In/Out 관리 싱글톤
/// </summary>
public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }
    
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;
    
    /// <summary>
    /// 현재 Fade 중인지 여부 (외부에서 입력 차단 등에 활용)
    /// </summary>
    public bool IsFading { get; private set; }
    
    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 초기 상태 설정
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }
    
    /// <summary>
    /// 화면 암전 (Fade Out)
    /// </summary>
    public IEnumerator FadeOut(float duration = -1f)
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("FadeCanvasGroup이 할당되지 않았습니다!");
            yield break;
        }
        
        IsFading = true;
        float time = duration > 0 ? duration : fadeDuration;
        float elapsed = 0f;
        
        fadeCanvasGroup.blocksRaycasts = true; // 마우스 입력 차단
        
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / time);
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// 화면 밝아짐 (Fade In)
    /// </summary>
    public IEnumerator FadeIn(float duration = -1f)
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("FadeCanvasGroup이 할당되지 않았습니다!");
            yield break;
        }
        
        float time = duration > 0 ? duration : fadeDuration;
        float elapsed = 0f;
        
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / time);
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false; // 입력 차단 해제
        IsFading = false;
    }
}
