using UnityEngine;

/// <summary>
/// 그래플링 대쉬 실행 상태.
/// 
/// [설계 의도]
/// - 그래플링 포인트는 "감지 구역" 역할만 함. 포인트 위치로 이동하지 않음.
/// - PlayerGrappleAimState에서 8방향으로 정규화된 방향 벡터를 SetDirection()으로 전달받아,
///   그 방향으로 dashSpeed만큼 직선 대시.
/// - 포인트와의 거리, 포인트 도달 여부는 완전히 무관.
/// - maxDashDuration 이후 InAirState로 전환.
/// </summary>
public class PlayerGrapplingState : PlayerState
{
    private readonly GrappleData data;

    private Vector2 direction;      // Enter()에서 계산, 이후 고정
    private float elapsedTime;
    private bool isFinished;
    private bool isStarted;         // Enter abort 감지용

    private float originalDrag;

    public PlayerGrapplingState(PlayerController player, PlayerStateMachine stateMachine, GrappleData data)
        : base(player, stateMachine)
    {
        this.data = data;
    }

    /// <summary>
    /// 상태 진입 전 반드시 호출 (방향 설정).
    /// PlayerGrappleAimState에서 ChangeState 직전에 호출.
    /// </summary>
    public void SetDirection(Vector2 newDirection)
    {
        // 8방향으로 이미 정규화되어 들어오는 벡터
        direction = newDirection.normalized;
    }

    // ==================== State Lifecycle ====================

    public override void Enter()
    {
        base.Enter();
        isStarted = false;

        if (data == null || direction.sqrMagnitude < 0.001f || data.maxDashDuration <= 0.001f)
        {
            stateMachine.ChangeState(player.InAirState);
            return;
        }

        elapsedTime = 0f;
        isFinished  = false;
        isStarted   = true;

        if (Mathf.Abs(direction.x) > 0.01f)
        {
            player.CheckDirectionToFace(direction.x > 0);
        }

        // 중력 및 마찰력(Drag) 제거하여 완벽한 직선 운동 보장
        player.RequestGravityOverride(0f);
        originalDrag = player.RB.linearDamping; // Unity 6에서는 drag 대신 linearDamping 사용
        player.RB.linearDamping = 0f;
        
        // 속도 고정 (시작 시점: 0초)
        float currentMultiplier = data.dashSpeedCurve != null ? data.dashSpeedCurve.Evaluate(0f) : 1f;
        player.RB.linearVelocity = direction * data.dashSpeed * currentMultiplier;

        PlayerEvents.RaiseGrappleStart();
    }

    public override void Exit()
    {
        if (!isStarted) return;

        // 마찰력 및 중력 복구
        player.RB.linearDamping = originalDrag;
        player.ClearGravityOverride();
        
        // InAirState로 넘어갈 때 관성을 유지하기 위해 마지막 속도 세팅 (이후 InAirState에서 Damping으로 자연 감속)
        float finalMultiplier = data.dashSpeedCurve != null ? data.dashSpeedCurve.Evaluate(1f) : 1f;
        player.RB.linearVelocity = direction * data.dashSpeed * finalMultiplier;
        
        PlayerEvents.RaiseGrappleEnd();
    }

    // ==================== Update ====================

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        if (!isStarted || data == null) return;

        // 진행도에 따른 속도 변화 곡선 적용
        float normalizedTime = Mathf.Clamp01(elapsedTime / data.maxDashDuration);
        float currentMultiplier = data.dashSpeedCurve != null ? data.dashSpeedCurve.Evaluate(normalizedTime) : 1f;
        
        // 매 물리 프레임마다 속도 강제 고정 (외부 간섭 원천 차단)
        player.RB.linearVelocity = direction * data.dashSpeed * currentMultiplier;
        elapsedTime += Time.fixedDeltaTime;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (!isStarted || isFinished || data == null) return;

        // 시간 기반으로만 종료 (완벽한 직선 대쉬 후 에어 상태로)
        bool timeout = elapsedTime >= data.maxDashDuration;

        if (timeout)
        {
            isFinished = true;
            stateMachine.ChangeState(player.InAirState);
        }
    }
}
