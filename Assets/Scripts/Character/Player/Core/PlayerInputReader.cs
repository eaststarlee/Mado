using UnityEngine;

/// <summary>
/// 플레이어 입력 수집 전담 컴포넌트.
/// Controls(InputAction) 액션을 읽어 정규화된 값으로 노출하고,
/// AutoWalk 상태를 관리합니다.
///
/// PlayerController는 이 컴포넌트를 참조하여 입력 값을 소비합니다.
/// GatherInput()은 PlayerController.Update()에서 매 프레임 호출됩니다.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerInputReader : MonoBehaviour
{
    // ── 입력 상태 (읽기 전용 노출) ─────────────────────────────
    public float  InputX          { get; private set; }
    public float  InputY          { get; private set; }
    public bool   JumpInput       { get; private set; }
    public bool   JumpInputUp     { get; private set; }
    public bool   JumpInputDown   { get; private set; }
    public bool   DashInput       { get; private set; }
    public bool   SprintInputHeld { get; private set; }
    public bool   IsAttackHeld    { get; private set; }
    public bool   ButtonAInput    { get; private set; }
    public bool   GrappleInput    { get; private set; }
    public bool   IsGrappleHeld   { get; private set; }
    public bool   IsSwitchHeld    { get; private set; }
    public bool   IsUpPressed     { get; private set; }
    public bool   IsDownPressed   { get; private set; }

    // ── AutoWalk (룸 전환 자동 워킹) ────────────────────────────
    public int AutoWalkDirection { get; private set; } = 0;

    public void SetAutoWalk(int dirX)
    {
        AutoWalkDirection = dirX;
    }

    // ── 내부 ────────────────────────────────────────────────────
    private Controls controls;
    private PlayerController player;

    // Axis GetKeyDown 모방용
    private bool wasUp;
    private bool wasDown;

    private void Awake()
    {
        controls = new Controls();
        player   = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        if (controls == null) controls = new Controls();
        controls.Enable();
    }
    private void OnDisable()
    {
        if (controls != null) controls.Disable();
    }

    // ── 공개 API ────────────────────────────────────────────────

    /// <summary>
    /// 매 프레임 입력 상태를 갱신합니다.
    /// PlayerController.Update() 첫 부분에서 호출하세요.
    /// </summary>
    public void GatherInput()
    {
        // ① 특수 행동(Slam) 또는 그래플링 대쉬 중 — 입력 완전 잠금
        bool isActionLocked = (player.Combat != null && player.Combat.IsSpecialActionLocked)
                              || player.StateMachine.CurrentState == player.GrapplingState;

        if (isActionLocked)
        {
            // V키(Grapple) 만은 항상 추적하여 강제 캔슬 가능
            IsGrappleHeld = controls.Player.Grapple.IsPressed();
            ClearAllInputs();
            IsGrappleHeld = false; // 위임 후 다시 초기화 (잠금 상태에서는 실제로도 막음)
            return;
        }

        // ② 룸 전환 오토 워킹 — 물리 이동만 강제하고 나머지 입력은 차단
        if (AutoWalkDirection != 0)
        {
            InputX = AutoWalkDirection;
            ClearAllInputs(keepInputX: true);
            player.CheckDirectionToFace(InputX > 0);
            return;
        }

        // ③ LedgeClimb 중 — 이동 입력만 차단
        if (player.StateMachine.CurrentState == player.LedgeClimbState)
        {
            InputX = 0f;
            InputY = 0f;
            return;
        }

        // ─── 일반 입력 수집 ────────────────────────────────────

        Vector2 inputVector = controls.Player.Move.ReadValue<Vector2>();
        InputX = inputVector.x;
        InputY = inputVector.y;

        // 방향 전환
        if (InputX != 0 && CanTurnDirection())
        {
            player.CheckDirectionToFace(InputX > 0);
        }

        // 점프
        if (controls.Player.Jump.triggered)
        {
            JumpInputDown = true;
            if (player.StateMachine.CurrentState == player.SprintTurnState)
                player.LastPressedJumpTime = 0;
            else if (player.StateMachine.CurrentState != player.DashState)
                player.LastPressedJumpTime = player.ActiveFormData.assist.jumpInputBufferTime;
        }
        else
        {
            JumpInputDown = false;
        }

        JumpInputUp     = controls.Player.Jump.WasReleasedThisFrame();
        JumpInput       = controls.Player.Jump.IsPressed();
        DashInput       = controls.Player.Dash.triggered;
        if (DashInput)
        {
            player.LastPressedDashTime = 0.15f; // 대쉬 선입력 버퍼
        }
        SprintInputHeld = controls.Player.Sprint.IsPressed();

        // 공격
        if (controls.Player.Attack.triggered && player.Combat != null)
        {
            player.LastPressedAttackTime = 0.15f; // 공격 선입력 버퍼
            player.ProcessAttackInput();
        }
        IsAttackHeld = controls.Player.Attack.IsPressed();

        // 기타 버튼
        ButtonAInput  = controls.Player.ButtonA.triggered;
        GrappleInput  = controls.Player.Grapple.triggered;
        IsGrappleHeld = controls.Player.Grapple.IsPressed();

        // ─── D키(차원전환), S키(펫 공격) — 레거시 구형 API 잠정 유지 ──
        // TODO(P2): Controls.inputactions에 WorldSwitch / PetAttack 액션 추가 후 교체
        IsSwitchHeld = Input.GetKey(KeyCode.D);
        if (Input.GetKeyDown(KeyCode.S))
        {
            player.Pet?.TriggerRushAttack();
        }

        // Up / Down pressed (GetKeyDown 모방)
        bool isUpNow    = InputY >  0.5f;
        bool isDownNow  = InputY < -0.5f;
        IsUpPressed     = isUpNow   && !wasUp;
        IsDownPressed   = isDownNow && !wasDown;
        wasUp   = isUpNow;
        wasDown = isDownNow;
    }

    // ── 헬퍼 ────────────────────────────────────────────────────

    /// <summary>
    /// 스프린트 계열 / 벽 관련 상태에서는 방향 전환 불가
    /// </summary>
    private bool CanTurnDirection()
    {
        var s = player.StateMachine.CurrentState;
        return s != player.SprintState
            && s != player.SprintTurnState
            && !player.IsSprintJumping
            && s != player.WallClimbState
            && s != player.WallJumpState
            && s != player.WallSlideState
            && s != player.LedgeClimbState;
    }

    /// <summary>
    /// 모든 입력을 false / 0으로 초기화합니다.
    /// </summary>
    private void ClearAllInputs(bool keepInputX = false)
    {
        if (!keepInputX) InputX = 0f;
        InputY          = 0f;
        JumpInput       = false;
        JumpInputUp     = false;
        JumpInputDown   = false;
        DashInput       = false;
        SprintInputHeld = false;
        IsAttackHeld    = false;
        ButtonAInput    = false;
        GrappleInput    = false;
        IsGrappleHeld   = false;
        IsSwitchHeld    = false;
        IsUpPressed     = false;
        IsDownPressed   = false;
        player.LastPressedJumpTime = 0f;
    }
}
