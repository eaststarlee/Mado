using UnityEngine;

/// <summary>
/// S(Grapple) 키를 누르고 있는 동안 조준하는 상태.
/// 시간이 느려지고(aimTimeScale), 최대 3초(aimDurationLimit) 간 대기 가능.
/// S키를 떼거나 3초 경과 시 입력된 화살표 8방향 중 하나로 돌진(PlayerGrapplingState)합니다.
/// 입력이 없으면 위쪽(Up)으로 돌진합니다.
/// </summary>
public class PlayerGrappleAimState : PlayerState
{
    private readonly GrappleData data;
    private int nearestKeyToRegister;
    private float aimTimer;
    private Vector2 lastValidAimDir;

    public PlayerGrappleAimState(PlayerController player, PlayerStateMachine stateMachine, GrappleData data) 
        : base(player, stateMachine)
    {
        this.data = data;
    }

    /// <summary>
    /// 콜라이더 쿨타임 키를 전달받아, 대쉬가 확정될 때 등록합니다.
    /// </summary>
    public void SetKey(int key)
    {
        nearestKeyToRegister = key;
    }

    public override void Enter()
    {
        base.Enter();
        
        aimTimer = 0f;

        // [New] 그래플링 시작(조준) 시 공중 능력을 즉시 리필하여 직후 연계가 가능하도록 함
        player.RefillAirAbilities();

        // 공중에서 정지 (주인공만 정지, 전체 시간은 그대로 흐름)
        player.RequestGravityOverride(0f);
        player.RB.linearVelocity = Vector2.zero;
        
        // 조준 방향 초기화 (기본 위쪽)
        lastValidAimDir = Vector2.up;
        
        // (필요 시 조준 애니메이션 등을 여기서 추가할 수 있습니다)
    }

    public override void Exit()
    {
        base.Exit();
        
        player.ClearGravityOverride();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        aimTimer += Time.unscaledDeltaTime;

        float limit = data != null ? data.aimDurationLimit : 3f;
        bool isTimeOut = aimTimer >= limit;
        bool isKeyReleased = !player.IsGrappleHeld;

        // V키 조준 중 실시간 화살표 입력 캐싱
        // 입력이 있을 때만 lastValidAimDir 갱신 (손 떼기 직전 마지막 방향 기억)
        Vector2 currentInput = new Vector2(player.InputX, player.InputY);
        if (currentInput.sqrMagnitude >= 0.1f)
        {
            lastValidAimDir = currentInput;
        }
        // 입력이 없으면 lastValidAimDir는 Enter()에서 설정한 Vector2.up 유지

        if (isTimeOut || isKeyReleased)
        {
            ExecuteDash();
        }
    }

    private void ExecuteDash()
    {
        // 마지막 유효 방향을 8방향으로 정규화 (입력 없으면 up이 기본)
        Vector2 dashDir = QuantizeTo8Directions(lastValidAimDir);

        // 쿨타임 등록
        player.GrappleDetector?.RegisterKey(nearestKeyToRegister);

        // 상태 전환 및 방향 설정
        player.GrapplingState.SetDirection(dashDir);
        stateMachine.ChangeState(player.GrapplingState);
    }

    /// <summary>
    /// 입력 벡터를 8방향 중 가장 가까운 방향으로 정규화하여 반환.
    /// zero 벡터가 들어올 경우 Vector2.up으로 안전하게 처리.
    /// </summary>
    private Vector2 QuantizeTo8Directions(Vector2 input)
    {
        // 안전 처리: zero 벡터 또는 너무 작은 입력이면 위쪽 반환
        if (input.sqrMagnitude < 0.001f)
            return Vector2.up;

        input.Normalize();
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;

        // 45도 단위로 반올림하여 8방향으로 스냅
        float step = 45f;
        float roundedAngle = Mathf.Round(angle / step) * step;

        return new Vector2(
            Mathf.Cos(roundedAngle * Mathf.Deg2Rad),
            Mathf.Sin(roundedAngle * Mathf.Deg2Rad)
        );
    }
}
