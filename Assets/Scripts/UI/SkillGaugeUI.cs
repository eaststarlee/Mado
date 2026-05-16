using UnityEngine;
using TMPro; // TextMeshPro 사용

/// <summary>
/// 스킬 게이지(MP/소울)를 화면에 텍스트 형태로 표시해주는 UI 스크립트.
/// 추후 아트 리소스가 나오면 슬라이더나 애니메이션 연동으로 확장할 수 있습니다.
/// </summary>
public class SkillGaugeUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("게이지를 표시할 TextMeshPro 컴포넌트")]
    [SerializeField] private TextMeshProUGUI gaugeText;

    [Header("Settings")]
    [Tooltip("{0}은 현재 수치, {1}은 최대 수치로 치환됩니다. (예: {0} / {1})")]
    [SerializeField] private string textFormat = "{0} / {1}";

    private void OnEnable()
    {
        // 이벤트 구독
        PlayerEvents.OnSkillGaugeChanged += UpdateGaugeUI;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        
        // 초기 상태 설정
        if (GameStateManager.Instance != null)
        {
            RefreshVisibility(GameStateManager.Instance.CurrentState);
        }
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위한 구독 해제
        PlayerEvents.OnSkillGaugeChanged -= UpdateGaugeUI;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState prev, GameState current)
    {
        RefreshVisibility(current);
    }

    private void RefreshVisibility(GameState state)
    {
        // Gameplay 상태에서만 게이지 노출
        if (gaugeText != null)
        {
            gaugeText.gameObject.SetActive(state == GameState.Gameplay);
        }
    }

    private void Start()
    {
        if (gaugeText == null)
        {
            gaugeText = GetComponent<TextMeshProUGUI>();
            
            if (gaugeText == null)
            {
                Debug.LogWarning("[SkillGaugeUI] TextMeshProUGUI 컴포넌트를 찾을 수 없습니다.");
            }
        }
    }

    /// <summary>
    /// 이벤트 발생 시 텍스트 업데이트
    /// </summary>
    private void UpdateGaugeUI(int current, int max)
    {
        if (gaugeText != null)
        {
            gaugeText.text = string.Format(textFormat, current, max);
        }
    }
}
