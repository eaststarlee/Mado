using UnityEngine;

/// <summary>
/// 전투 규칙 설정 (상황 의존 값)
/// AttackData(성격)와 분리하여 부적/난이도/버프 등에 유연하게 대응
/// </summary>
[CreateAssetMenu(fileName = "CombatConfig", menuName = "Combat/Config")]
public class CombatConfig : ScriptableObject
{
    [Header("공격 쿨타임")]
    [Tooltip("기본 공격 재사용 대기시간")]
    public float baseAttackCooldown = 0.25f;
    
    [Header("입력 버퍼")]
    [Tooltip("공격 입력 버퍼 유지 시간")]
    public float inputBufferTime = 0.15f;
    

}
