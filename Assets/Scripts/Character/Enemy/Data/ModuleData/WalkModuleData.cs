using UnityEngine;

/// <summary>
/// WalkModule의 세팅 데이터 (ScriptableObject).
/// 순찰 속도, 추격 속도, 대기 시간, 방향 전환 딜레이 등.
/// </summary>
[CreateAssetMenu(fileName = "NewWalkModuleData", menuName = "Enemy/Module Data/Walk")]
public class WalkModuleData : ScriptableObject, IModuleData
{
    public string ModuleName => "WalkModule";
    
    [Header("순찰 (Patrol)")]
    [Tooltip("순찰 이동 속도")]
    public float patrolSpeed = 2f;
    
    [Tooltip("벽/낭떠러지에서 방향 전환 후 대기 시간")]
    public float patrolWaitTime = 1f;
    
    [Header("추격 (Chase)")]
    [Tooltip("추격 이동 속도")]
    public float chaseSpeed = 4f;
    
    [Tooltip("추격 포기 거리 (타겟이 이 거리를 넘으면 순찰 복귀)")]
    public float chaseGiveUpDistance = 15f;
    
    [Tooltip("막힘 판정 시간 (초) - 이 시간동안 제자리면 Search로 전환")]
    public float stuckThreshold = 1.5f;

    [Header("수색 (Search)")]
    [Tooltip("수색 이동 속도")]
    public float searchSpeed = 4f;
    
    [Tooltip("수색 대기 시간 (LastKnownPosition 도착 후 두리번거리는 시간)")]
    public float searchDuration = 2f;
    
    [Tooltip("목적지 도착 판정 거리")]
    public float arrivalDistance = 0.5f;
    
    [Tooltip("수색 이동 타임아웃 (도달 불가 시 대기 상태로 강제 전환)")]
    public float searchMoveTimeout = 3f;

    [Header("공통")]
    [Tooltip("방향 전환 시 잠깐 멈추는 시간")]
    public float turnDelay = 0.1f;
}
