using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 체력 UI 전체를 관리하는 컴포넌트
/// </summary>
public class HealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private Transform heartContainer;
    
    [Header("Settings")]
    [SerializeField] private bool animateChanges = true;
    
    private List<HeartUI> hearts = new List<HeartUI>();
    private int currentHealth;
    private int maxHealth;
    
    #region Unity Lifecycle
    /// <summary>
    /// 초기화 동기화 - PlayerHealth의 현재 상태를 가져와 초기화
    /// </summary>
    private void Start()
    {
        // PlayerHealth를 찾아 초기 상태 동기화
        var playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            UpdateHealthInternal(playerHealth.CurrentHealth, playerHealth.MaxHealth, false);
        }
        else
        {
            Debug.LogWarning("PlayerHealth를 찾을 수 없습니다. 기본값으로 초기화합니다.");
            UpdateHealthInternal(5, 5, false); // 기본값
        }
    }
    
    private void OnEnable()
    {
        // 이벤트 구독
        PlayerEvents.OnHealthChanged += UpdateHealth;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        
        // 초기 상태 설정
        if (GameStateManager.Instance != null)
        {
            RefreshVisibility(GameStateManager.Instance.CurrentState);
        }
    }
    
    private void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        PlayerEvents.OnHealthChanged -= UpdateHealth;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState prev, GameState current)
    {
        RefreshVisibility(current);
    }

    private void RefreshVisibility(GameState state)
    {
        // 메인 메뉴를 제외하고 노출 (로딩 중 페이드인 될 때 자연스럽게 보이도록)
        if (heartContainer != null)
        {
            heartContainer.gameObject.SetActive(state != GameState.MainMenu);
        }
    }
    #endregion
    
    #region Health Update
    /// <summary>
    /// 체력 업데이트 (PlayerEvents.OnHealthChanged 콜백)
    /// </summary>
    private void UpdateHealth(int current, int max)
    {
        UpdateHealthInternal(current, max, animateChanges);
    }

    private void UpdateHealthInternal(int current, int max, bool doAnimate)
    {
        // 최대 체력 변경 시 하트 개수 조정
        if (max != maxHealth)
        {
            int diff = max - maxHealth;
            if (diff > 0)
            {
                CreateHearts(diff);
            }
            else if (diff < 0)
            {
                RemoveHearts(-diff);
            }
            
            maxHealth = max;
        }
        
        // 현재 체력에 따라 하트 fillAmount 업데이트
        for (int i = 0; i < hearts.Count; i++)
        {
            // 각 하트는 1칸을 담당
            // 예: 체력 2.5 → 하트0=1.0, 하트1=1.0, 하트2=0.5
            float fillAmount = Mathf.Clamp01(current - i);
            hearts[i].SetFillAmount(fillAmount, doAnimate);
        }
        
        currentHealth = current;
    }
    #endregion
    
    #region Heart Management
    /// <summary>
    /// 하트 생성
    /// </summary>
    private void CreateHearts(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject heartObj = Instantiate(heartPrefab, heartContainer);
            HeartUI heartUI = heartObj.GetComponent<HeartUI>();
            
            if (heartUI != null)
            {
                hearts.Add(heartUI);
                // 초기 상태는 가득 참
                heartUI.SetFillAmount(1f, false);
            }
            else
            {
                Debug.LogError("Heart Prefab에 HeartUI 컴포넌트가 없습니다!");
                Destroy(heartObj);
            }
        }
    }
    
    /// <summary>
    /// 하트 제거
    /// </summary>
    private void RemoveHearts(int count)
    {
        for (int i = 0; i < count && hearts.Count > 0; i++)
        {
            int lastIndex = hearts.Count - 1;
            HeartUI heartToRemove = hearts[lastIndex];
            hearts.RemoveAt(lastIndex);
            
            if (heartToRemove != null)
            {
                Destroy(heartToRemove.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 모든 하트 제거 (리셋용)
    /// </summary>
    public void ClearAllHearts()
    {
        foreach (var heart in hearts)
        {
            if (heart != null)
            {
                Destroy(heart.gameObject);
            }
        }
        hearts.Clear();
        maxHealth = 0;
        currentHealth = 0;
    }
    #endregion
    
    #region Editor Helpers
#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 설정 검증
    /// </summary>
    private void OnValidate()
    {
        if (heartPrefab == null)
        {
            Debug.LogWarning("Heart Prefab이 할당되지 않았습니다!");
        }
        
        if (heartContainer == null)
        {
            Debug.LogWarning("Heart Container가 할당되지 않았습니다!");
        }
    }
#endif
    #endregion
}
