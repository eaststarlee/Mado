using UnityEngine;

/// <summary>
/// 대시/돌진 모듈 데이터 SO.
/// </summary>
[CreateAssetMenu(fileName = "NewDashData", menuName = "Enemy/Module Data/Dash")]
public class DashModuleData : ScriptableObject, IModuleData
{
    public string ModuleName => "Dash";
    
    [Header("속도")]
    [Tooltip("돌진 속도")]
    public float dashSpeed = 15f;
    
    [Header("타이밍")]
    [Tooltip("돌진 전조 시간 (준비 동작)")]
    public float prepareDuration = 0.5f;
    [Tooltip("돌진 지속 시간")]
    public float dashDuration = 0.3f;
    [Tooltip("감속 시간 (돌진 후)")]
    public float recoveryDuration = 0.4f;
    
    [Header("쿨다운")]
    public float cooldown = 3f;
    
    [Header("사거리 조건")]
    [Tooltip("최소 사거리 (이 이상이어야 대시 실행)")]
    public float minRange = 3f;
    [Tooltip("최대 사거리 (이 이내여야 대시 실행)")]
    public float maxRange = 10f;
    
    [Header("우선순위")]
    [Tooltip("WalkModule보다 높아야 대시 우선")]
    public int priority = 5;
    
    /// <summary>
    /// 전체 대시 지속 시간.
    /// </summary>
    public float TotalDuration => prepareDuration + dashDuration + recoveryDuration;
}
