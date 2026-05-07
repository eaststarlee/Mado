using UnityEngine;

/// <summary>
/// 라이징 공격 (위 화살표 + 공격) 데이터
/// 불꽃 대시 형태로 상승하며 공격하는 특수 행동 데이터입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewRisingAttack", menuName = "Player/Rising Attack Data")]
public class RisingAttackData : AttackData
{
    [Header("Rising Attack Dynamics")]
    [Tooltip("돌진 시작 전 기를 모으는 시간입니다. (초 단위)")]
    public float risingAnticipationDelay = 0.1f;
    
    [Header("Fire Dash Dynamics")]
    [Tooltip("불꽃 대시 돌진 속도입니다. 수치가 높을수록 더 빠르게 이동합니다.")]
    public float dashSpeed = 30f;
    
    [Tooltip("불꽃 대시가 유지되는 시간입니다. 이동 거리와 직결됩니다. (초 단위)")]
    public float dashDuration = 0.2f;
    
    [Tooltip("좌상/우상 대각선 이동 시 속도에 곱해지는 보정값입니다. (1보다 작으면 약간 느려짐)")]
    public float diagonalMultiplier = 0.8f;
    
    [Tooltip("체크 시 대시가 시작되어 끝날 때까지 적의 공격을 무시하는 무적 상태가 됩니다.")]
    public bool isInvincibleDuringDash = true;

    [Header("Post-Dash Physics")]
    [Tooltip("돌진 종료 직후 남겨둘 속도 비율 (0 = 즉시 정지, 1 = 속도 유지). 너무 높으면 위로 솟구칩니다.")]
    [Range(0f, 1f)] public float momentumRetention = 0.2f;
}
