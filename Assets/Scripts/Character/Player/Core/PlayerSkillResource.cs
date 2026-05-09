using UnityEngine;

/// <summary>
/// 플레이어의 스킬 게이지(소울, MP 등)를 전담하여 관리하는 스크립트.
/// 적 타격이나 패링 성공 시 차오르며, 스킬/특수기 사용 시 소모됩니다.
/// 차후 아이템 획득을 통한 최대치(Max Gauge) 영구 증가 기능을 지원합니다.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerSkillResource : MonoBehaviour
{
    private PlayerController playerController;

    [Header("Runtime State")]
    [SerializeField, Tooltip("현재 스킬 게이지")]
    private int currentGauge;
    
    [SerializeField, Tooltip("현재(업그레이드 반영) 최대 스킬 게이지")]
    private int currentMaxGauge;

    public int CurrentGauge => currentGauge;
    public int MaxGauge => currentMaxGauge;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        InitializeGauge();
    }

    /// <summary>
    /// 초기 게이지 설정 (캐릭터 폼 데이터 및 게임 진행 상황 반영)
    /// </summary>
    private void InitializeGauge()
    {
        if (playerController != null && playerController.ActiveFormData != null)
        {
            // 기본 시작 최대치
            currentMaxGauge = playerController.ActiveFormData.skillResource.baseMaxGauge;
            currentGauge = 0; 
            
            // 씬 로드 시 중앙 데이터로부터 현재 상태 반영
            if (GameProgressManager.Instance != null && GameProgressManager.Instance.CurrentData != null)
            {
                var data = GameProgressManager.Instance.CurrentData;
                currentMaxGauge = data.maxSP > 0 ? data.maxSP : currentMaxGauge;
                currentGauge = data.currentSP;
            }
            
            NotifyGaugeChanged();
        }
    }

    /// <summary>
    /// 게이지 획득 (공격, 패링 성공 등)
    /// </summary>
    public void AddGauge(int amount)
    {
        if (amount <= 0) return;

        currentGauge = Mathf.Min(currentGauge + amount, currentMaxGauge);
        NotifyGaugeChanged();
    }

    /// <summary>
    /// 게이지 소모 (스킬, 그래플링 사용 등)
    /// </summary>
    /// <returns>소모 성공 여부 (게이지가 충분했는지)</returns>
    public bool TryConsumeGauge(int amount)
    {
        if (amount <= 0) return true; 

        if (currentGauge >= amount)
        {
            currentGauge -= amount;
            NotifyGaugeChanged();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 아이템 파밍 등을 통해 최대 게이지 량을 영구적으로 증가시킬 때 호출
    /// </summary>
    public void IncreaseMaxGauge(int amount)
    {
        if (amount <= 0) return;

        currentMaxGauge += amount;
        NotifyGaugeChanged();
    }
    
    /// <summary>
    /// 부활 시 등 게이지 강제 초기화가 필요할 때
    /// </summary>
    public void ResetGaugeToZero()
    {
        currentGauge = 0;
        NotifyGaugeChanged();
    }

    private void NotifyGaugeChanged()
    {
        PlayerEvents.RaiseSkillGaugeChanged(currentGauge, currentMaxGauge);
    }
}
