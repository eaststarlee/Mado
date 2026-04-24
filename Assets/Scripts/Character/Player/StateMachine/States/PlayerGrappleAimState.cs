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

        // unscaledDeltaTime을 사용하여 시간 감속에 영향받지 않는 실제 시간 경과 측정
        aimTimer += Time.unscaledDeltaTime;

        float limit = data != null ? data.aimDurationLimit : 3f;
        bool isTimeOut = aimTimer >= limit;
        
        // PlayerController에서 매 프레임 업데이트되는 S키 유지 상태
        bool isKeyReleased = !player.IsGrappleHeld;

        // V키 조준 중 실시간 화살표 입력 캐싱 (손 떼기 직전까지 기억)
        Vector2 currentInput = new Vector2(player.InputX, player.InputY);
        if (currentInput.sqrMagnitude >= 0.1f)
        {
            lastValidAimDir = currentInput;
        }

        if (isTimeOut || isKeyReleased)
        {
            ExecuteDash();
        }
    }

    private void ExecuteDash()
    {
        // 최후 순간까지 유효했던 방향 사용 (손 떼는 찰나에 입력이 풀려도 보정됨)
        Vector2 dashDir = QuantizeTo8Directions(lastValidAimDir);

        // 2. 쿨타임 등록
        player.GrappleDetector?.RegisterKey(nearestKeyToRegister);

        // 3. 상태 전환 및 대쉬 방향 설정
        player.GrapplingState.SetDirection(dashDir);
        stateMachine.ChangeState(player.GrapplingState);
    }

    /// <summary>
    /// 입력 벡터를 제일 가까운 8방향 벡터로 변환 (정규화된 Vector2 반환)
    /// </summary>
    private Vector2 QuantizeTo8Directions(Vector2 input)
    {
        input.Normalize();
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;

        // 45도 단위로 반올림
        float step = 45f;
        float roundedAngle = Mathf.Round(angle / step) * step;

        return new Vector2(Mathf.Cos(roundedAngle * Mathf.Deg2Rad), Mathf.Sin(roundedAngle * Mathf.Deg2Rad));
    }
}
