using UnityEngine;

/// <summary>
/// 그래플링 시스템 전체 수치 데이터.
/// GrappleDetector와 PlayerGrapplingState가 공유합니다.
/// </summary>
[CreateAssetMenu(fileName = "GrappleData", menuName = "Player/Grapple Data")]
public class GrappleData : ScriptableObject
{
    [Header("감지")]
    [Tooltip("플레이어 주변 감지 원 반지름")]
    public float detectionRadius = 5f;
    [Tooltip("그래플링 대상 레이어 (GrappleTarget)")]
    public LayerMask grappleTargetLayer;
    [Tooltip("LOS 차단 레이어 (Ground + Wall 등 지형)")]
    public LayerMask losBlockerLayer;
    [Tooltip("감지 범위를 벗어나도 타겟팅을 유지해주는 코요테 타임 (초)")]
    public float coyoteTime = 0.15f;
    [Tooltip("명중하기 전에 미리 키를 눌러도 인식해주는 선입력 시간 (초)")]
    public float inputBufferTime = 0.15f;

    [Header("대쉬")]
    [Tooltip("그래플링 대쉬 속도 (최대 혹은 기본 스케일)")]
    public float dashSpeed = 18f;
    [Tooltip("시간에 따른 대쉬 속도 배율 (X축: 0~1 진행도, Y축: 속도 배율)")]
    public AnimationCurve dashSpeedCurve = new AnimationCurve(
        new Keyframe(0f, 1.0f),
        new Keyframe(1f, 0.2f)
    );
    [Tooltip("안전장치: 이 시간 내에 도착 못하면 강제 종료")]
    public float maxDashDuration = 0.4f;
    [Tooltip("대쉬 종료 후 입력 제어를 재개하기 전까지 관성을 유지하는 시간 (초)\n0 = 즉시 입력 제어, 클수록 더 오래 날아감")]
    public float postGrappleControlDelay = 0.15f;
    [Tooltip("대쉬 종료 후 관성이 깎이는 저항값 (1.0 = 유지, 0.0에 가까울수록 급감속)")]
    [Range(0f, 1f)]
    public float postGrappleDamping = 0.85f;

    [Header("쿨타임")]
    [Tooltip("연속 그래플 방지 전역 쿨타임 (초)")]
    public float globalCooldown = 0.5f;
    [Tooltip("같은 포인트 재사용 불가 시간 (초, 0 = 제한 없음)")]
    public float pointCooldown = 3f;

    [Header("조준 (새로운 그래플링)")]
    [Tooltip("S 키를 누른 상태에서 유지할 수 있는 최대 조준 시간 (초)")]
    public float aimDurationLimit = 3f;
}
