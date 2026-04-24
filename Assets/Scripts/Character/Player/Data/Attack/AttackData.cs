using UnityEngine;

/// <summary>
/// 공격의 "성격"을 정의하는 데이터 (What)
/// 타이밍/쿨타임 등 상황 의존 값은 포함하지 않음
/// </summary>
[CreateAssetMenu(fileName = "NewAttack", menuName = "Player/Attack Data")]
public class AttackData : ScriptableObject
{
    [Header("공격 정체성")]
    public string attackName;
    public AttackDirection direction;
    
    [Header("데미지")]
    public int baseDamage = 1;
    public Vector2 baseKnockback = new Vector2(5f, 3f);
    
    [Header("히트박스 (OverlapBox용)")]
    [Tooltip("플레이어 중심 기준 오프셋 (Facing 방향에 따라 X 반전됨)")]
    public Vector2 hitboxOffset = new Vector2(1f, 0f);
    public Vector2 hitboxSize = new Vector2(1.5f, 1f);
    
    [Header("반동 규칙 (적중 시)")]
    public RecoilType recoilType = RecoilType.None;
    [Tooltip("적중 시 플레이어에게 가해지는 힘 (포고 등)")]
    public Vector2 recoilForce;
    
    [Header("넉백 규칙 (상대방)")]
    public KnockbackMode knockbackMode = KnockbackMode.FixedDirection;
    [Tooltip("상대방이 제어 불능 상태가 되는 시간 (초)")]
    public float stunDuration = 0.4f; // Default for light attacks
    
    [Header("타격감")]
    [Tooltip("적중 시 게임 정지 시간")]
    public float hitStopDuration = 0.05f;
    [Tooltip("카메라 흔들림 강도")]
    public float screenShakeMagnitude = 0.1f;
    
    [Header("애니메이션")]
    public string animationName;
    [Tooltip("애니메이션 클립 길이 기준 (Animator.speed 계산용)")]
    public float baseAnimDuration = 0.3f;
    [Range(0f, 1f)]
    [Tooltip("히트 판정이 활성화되는 정규화 시간 (0~1)")]
    public float hitActiveNormalized = 0.2f;
    
    [Header("제약 (Constraint)")]
    [Tooltip("지상 공격 시 이동 잠금 여부 (Normal 공격은 보통 false, 강공격은 true)")]
    public bool lockMovementOnGround = false;
    [Tooltip("이동 잠금 지속 시간")]
    public float lockDuration = 0.1f;
}

/// <summary>
/// 공격 방향
/// </summary>
public enum AttackDirection
{
    Normal,     // 전방
    Up,         // 위
    Down        // 아래
}

/// <summary>
/// 적중 시 플레이어 반동 규칙
/// </summary>
public enum RecoilType
{
    None,           // 반동 없음
    ReplaceY,       // Y속도 덮어쓰기 (레거시, 권장하지 않음)
    AddImpulse,     // 충격 추가
    Slam,           // 아래로 강하게 (Devil폼 Slam)
    PogoJump        // Hollow Knight 스타일 Pogo (중력 스케일 기반)
}

/// <summary>
/// 넉백 방향 결정 모드
/// </summary>
public enum KnockbackMode
{
    FixedDirection,     // 공격자 방향 기준 (기본)
    RadialFromOrigin    // 공격 원점으로부터 방사형 (Slam Impact 등)
}
