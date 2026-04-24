using UnityEngine;

/// <summary>
/// 폼별 공격 프로필
/// CharacterFormData에서 참조하여 폼 전환 시 자동으로 공격 데이터 교체
/// </summary>
[CreateAssetMenu(fileName = "NewFormAttackProfile", menuName = "Player/Form Attack Profile")]
public class FormAttackProfile : ScriptableObject
{
    [Header("방향별 공격 데이터")]
    [Tooltip("전방 공격")]
    public AttackData normalAttack;
    [Tooltip("위쪽 공격")]
    public AttackData upAttack;
    [Tooltip("아래쪽 공격 (Normal폼: Pogo / Devil폼: Slam)")]
    public AttackData downAttack;
    
    [Header("폼 전용 수치 보정")]
    [Tooltip("공격 속도 배율 (1.0 = 기본, 1.5 = 50% 빠름)")]
    [Range(0.5f, 2f)]
    public float attackSpeedMultiplier = 1f;
    
    [Tooltip("데미지 배율")]
    [Range(0.5f, 3f)]
    public float damageMultiplier = 1f;
    
    [Tooltip("히트박스 범위 배율")]
    [Range(0.5f, 2f)]
    public float rangeMultiplier = 1f;
    

    
    /// <summary>
    /// 방향에 해당하는 공격 데이터 반환
    /// </summary>
    public AttackData GetAttack(AttackDirection direction)
    {
        return direction switch
        {
            AttackDirection.Up => upAttack,
            AttackDirection.Down => downAttack,
            _ => normalAttack
        };
    }
}
