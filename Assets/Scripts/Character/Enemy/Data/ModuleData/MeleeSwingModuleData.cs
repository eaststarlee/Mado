using UnityEngine;

/// <summary>
/// 근접 공격 모듈 세팅 SO.
/// </summary>
[CreateAssetMenu(fileName = "NewMeleeSwingData", menuName = "Enemy/Module Data/Melee Swing")]
public class MeleeSwingModuleData : ScriptableObject, IModuleData
{
    public string ModuleName => "MeleeSwing";
    
    [Header("데미지")]
    public float damage = 10f;
    public HitType hitType = HitType.Light;
    
    [Header("넉백")]
    public float knockbackForce = 5f;
    
    [Header("타이밍")]
    [Tooltip("사거리 도달 후 실제로 공격 모션(전조)을 시작하기 전까지 대기하는 시간")]
    public float preAttackDelay = 0.0f; // [New]
    [Tooltip("공격 전조 시간 (애니메이션 시작 ~ 히트박스 활성)")]
    public float anticipationDuration = 0.2f;
    [Tooltip("히트박스 활성 시간")]
    public float activeDuration = 0.15f;
    [Tooltip("후딜 시간 (히트박스 OFF ~ 모듈 완료)")]
    public float recoveryDuration = 0.3f;
    
    [Header("쿨다운")]
    public float cooldown = 1.5f;
    
    [Header("사거리")]
    public float attackRange = 2f;
    
    [Header("이동 및 동작 옵션")]
    [Tooltip("공격 중일 때 이동(속도)을 강제로 멈출 것인지 여부. 끄면 걸으면서 공격 가능")]
    public bool stopMovementOnAttack = true;
    [Tooltip("공격 시작 후 속도를 강제로 0으로 유지할 시간. 0이면 공격 전체(PreDelay+TotalDuration) 동안 멈춤.")]
    public float stopMovementDuration = 0f; // [New]
    [Tooltip("공격이 발생(히트박스 활성)하는 순간 앞으로 전진할 거리 (런지)")]
    public float forwardDashDistance = 0f; // [New]
    
    [Header("디버그 옵션")]
    [Tooltip("평소에는 기즈모를 숨기고, 실제로 몬스터가 공격 중일 때만 빨간 박스를 표시")]
    public bool showGizmoOnlyWhenAttacking = true; // [New]
    
    [Header("히트박스")]
    public Vector2 hitboxOffset = new Vector2(1f, 0f);
    public Vector2 hitboxSize = new Vector2(1.5f, 1f);
    
    [Header("타겟 레이어")]
    public LayerMask targetLayer;
    
    /// <summary>
    /// 전체 공격 지속 시간.
    /// </summary>
    public float TotalDuration => anticipationDuration + activeDuration + recoveryDuration;
}
