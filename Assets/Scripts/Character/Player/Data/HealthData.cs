using UnityEngine;

/// <summary>
/// 체력 관련 설정 데이터
/// </summary>
[CreateAssetMenu(fileName = "NewHealthData", menuName = "Player/Health Data")]
public class HealthData : ScriptableObject
{
    [Header("Health Settings")]
    public int maxHealth = 5;           // 최대 체력 (하트 개수)
    public int startingHealth = 5;      // 시작 체력
    
    [Header("Invincibility")]
    public float invincibilityDuration = 1.5f;  // 무적 시간
    public LayerMask enemyLayer;                // 무적 중 충돌 무시할 적 레이어
}
