using UnityEngine;

[CreateAssetMenu(fileName = "NewSlamAttack", menuName = "Player/Slam Attack Data")]
public class SlamAttackData : AttackData
{
    [Header("Slam 설정 (Action)")]
    [Tooltip("기 모으기 시간 (초)")]
    public float slamAnticipationDuration = 0.5f;

    [Tooltip("하강 속도 (고정 velocity)")]
    public float slamDescentSpeed = 40f;
    
    [Tooltip("착지 후 무적 시간 (초)")]
    public float slamPostInvincibilityDuration = 1.0f;
    
    [Tooltip("착지 후 경직 시간")]
    public float slamRecoveryDuration = 0.25f;

    [Tooltip("착지 충격파 공격 데이터 (임팩트)")]
    public AttackData slamImpactAttack;
    
    [Tooltip("착지 충격파 VFX")]
    public GameObject slamImpactVFXPrefab;
}
