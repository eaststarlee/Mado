using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    #region State Machine Variables
    public PlayerStateMachine StateMachine { get; private set; }

    public PlayerIdleState IdleState { get; private set; }
    public PlayerMoveState MoveState { get; private set; }
    public PlayerInAirState InAirState { get; private set; }
    public PlayerDashState DashState { get; private set; }
    // [SPRINT_DISABLED] Sprint 관련 상태 비활성화
    // public PlayerSprintState SprintState { get; private set; }
    // public PlayerSprintStopState SprintStopState { get; private set; }
    // public PlayerSprintTurnState SprintTurnState { get; private set; }
    // public PlayerSprintJumpPrepareState SprintJumpPrepareState { get; private set; }
    public PlayerWallSlideState WallSlideState { get; private set; }
    public PlayerWallJumpState WallJumpState { get; private set; }
    public PlayerWallClimbState WallClimbState { get; private set; }
    public PlayerLedgeClimbState LedgeClimbState { get; private set; }
    public PlayerGlideState GlideState { get; private set; }
    public PlayerTransformState TransformState { get; private set; }
    public PlayerHitState HitState { get; private set; }
    public PlayerParryState ParryState { get; private set; }
    public PlayerDeathState DeathState { get; private set; }
    public PlayerGrappleAimState GrappleAimState { get; private set; }
    public PlayerGrapplingState GrapplingState { get; private set; }
    public PlayerRestState RestState { get; private set; }
    #endregion

    #region State Variables
    public bool CanDoubleJump { get; set; }
    // [SPRINT_DISABLED]
    // public bool IsSprintJumping { get; set; }
    // public float SprintJumpVelocityX { get; set; }
    public int DashCountLeft { get; set; }
    public float GlideHoldTime { get; set; }
    public bool IsGliding { get; private set; }
    public bool CanRisingAttack { get; set; } = true;
    private float ledgeFailCooldownTimer = 0f;
    private float grappleBufferTimer = 0f;
    public bool IsInDimensionZone { get; set; }
    public int VerticalLookIntention { get; set; } = 0;
    #endregion

    #region Input Variables
    public float InputX => inputReader.InputX;
    public float InputY => inputReader.InputY;
    public bool DashInput => inputReader.DashInput;
    // [SPRINT_DISABLED] public bool SprintInputHeld => inputReader.SprintInputHeld;
    public float LastPressedJumpTime { get; set; }
    public float LastPressedDashTime { get; set; }
    public float LastPressedAttackTime { get; set; }
    public bool JumpInputUp => inputReader.JumpInputUp;
    public bool JumpInput => inputReader.JumpInput;
    public bool JumpInputDown => inputReader.JumpInputDown;
    public bool IsAttackHeld => inputReader.IsAttackHeld;
    public bool ButtonAInput => inputReader.ButtonAInput;
    public bool ParryInput    => inputReader.ParryInput;
    public bool IsGrappleHeld => inputReader.IsGrappleHeld;
    public bool IsSwitchHeld => inputReader.IsSwitchHeld;
    
    // 차원 전환 타이머
    private float switchHoldTimer = 0f;
    [SerializeField] private float targetSwitchHoldTime = 0.8f;
    private bool isSwitchInterrupted = false;

    public bool IsUpPressed => inputReader.IsUpPressed;
    public bool IsDownPressed => inputReader.IsDownPressed;
    
    public float LastOnGroundTime { get; private set; }
    public float LastOnWallTime { get; private set; }
    public int LastWallDirection { get; private set; }
    public bool IsFacingRight { get; private set; }
    public bool WasLongWallJump { get; set; }
    public float timeSinceLanded = Mathf.Infinity;

    // [SPRINT_DISABLED] public float lastSprintTurnTime = -10f;
    private float lastDashTime = -10f;
    private float lastWallJumpTime = -10f;
    private float lastGrappleTime = -10f;
    
    public int AutoWalkDirection => inputReader.AutoWalkDirection;
    public void SetAutoWalk(int dirX) => inputReader.SetAutoWalk(dirX);
    #endregion

    #region Component Variables
    public Rigidbody2D RB { get; private set; }
    public PetController Pet { get; set; }
    public PlayerHealth Health { get; private set; } // Awake에서 캐싱
    public LedgeDetector LedgeDetector { get; private set; }
    public PlayerCombat Combat { get; private set; }
    public GrappleDetector GrappleDetector { get; private set; }
    [SerializeField] private GrappleData grappleData;
    public GrappleData GrappleData => grappleData;

    private SpriteRenderer spriteRenderer;



    // 자식 신규 전담 컴포넌트 참조 저장
    public PlayerInputReader inputReader { get; private set; }
    public PlayerFormManager formManager { get; private set; }
    public PlayerActionController actionController { get; private set; }
    public PlayerAnimationController animationController { get; private set; }
    public Mado.AnimationSystem.ICharacterAnimator Animator { get; private set; }

    public FormType CurrentForm => formManager.CurrentForm;
    public CharacterFormData ActiveFormData => formManager.ActiveFormData;



    public event System.Action OnGroundedConfirmed;
    public void RaiseGroundedConfirmed() => OnGroundedConfirmed?.Invoke();
    
    public bool IsInPogo => Combat != null && Combat.IsPogoActive;
    public bool IsRecoiling => actionController.IsRecoiling;

    public void StartRecoil(Vector2 force, float duration) => actionController.StartRecoil(force, duration);
    public void PogoBounce(float bounceVelocity) => actionController.PogoBounce(bounceVelocity);
    public void StartHitStop(float duration)
    {
        actionController.StartHitStop(duration);
    }
    #endregion

    #region Check Variables
    [Header("Checks")]
    [SerializeField] public LayerMask GroundLayer;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector2 groundCheckSize;
    [Space(5)]
    [SerializeField] private Transform wallCheckPoint;
    [SerializeField] private Vector2 wallCheckSize;
    #endregion

    #region Data Variables
#if UNITY_EDITOR
    [Header("Debug Info")]
    [SerializeField] private string currentStateName;
    [SerializeField] private string currentAnimationName;
#endif
    #endregion

    #region Unity Callback Functions
    private void Awake()
    {
        // 1. 필수 컴포넌트 캐싱
        Health = GetComponent<PlayerHealth>();
        inputReader = GetComponent<PlayerInputReader>();
        formManager = GetComponent<PlayerFormManager>();
        actionController = GetComponent<PlayerActionController>();
        RB = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        Animator = GetComponentInChildren<Mado.AnimationSystem.ICharacterAnimator>();
        
        animationController = GetComponent<PlayerAnimationController>();
        if (animationController == null) animationController = gameObject.AddComponent<PlayerAnimationController>();
        animationController.Initialize(Animator);


        var boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            boxCol.edgeRadius = 0.015f;
        }
        
        LedgeDetector = GetComponentInChildren<LedgeDetector>(); 
        Combat = GetComponent<PlayerCombat>(); 
        GrappleDetector = GetComponent<GrappleDetector>(); 

        // 2. [Build Optimization] 물리 보간 및 프레임 설정
        if (RB != null) RB.interpolation = RigidbodyInterpolation2D.Interpolate;
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = -1;

        // 3. 시스템 초기화
        StateMachine = new PlayerStateMachine();
        IsFacingRight = true;
        InitializeStates();
    }



    private void InitializeStates()
    {
        IdleState = new PlayerIdleState(this, StateMachine);
        MoveState = new PlayerMoveState(this, StateMachine);
        InAirState = new PlayerInAirState(this, StateMachine);
        DashState = new PlayerDashState(this, StateMachine);
        // [SPRINT_DISABLED]
        // SprintState = new PlayerSprintState(this, StateMachine);
        // SprintStopState = new PlayerSprintStopState(this, StateMachine);
        // SprintTurnState = new PlayerSprintTurnState(this, StateMachine);
        // SprintJumpPrepareState = new PlayerSprintJumpPrepareState(this, StateMachine);
        WallSlideState = new PlayerWallSlideState(this, StateMachine);
        WallJumpState = new PlayerWallJumpState(this, StateMachine);
        WallClimbState = new PlayerWallClimbState(this, StateMachine);
        LedgeClimbState = new PlayerLedgeClimbState(this, StateMachine);
        GlideState = new PlayerGlideState(this, StateMachine);
        TransformState = new PlayerTransformState(this, StateMachine);
        HitState = new PlayerHitState(this, StateMachine);
        ParryState = new PlayerParryState(this, StateMachine);
        DeathState = new PlayerDeathState(this, StateMachine);
        GrappleAimState = new PlayerGrappleAimState(this, StateMachine, grappleData);
        GrapplingState = new PlayerGrapplingState(this, StateMachine, grappleData);
        RestState = new PlayerRestState(this, StateMachine);
    }
    
    private void OnDisable()
    {
        Combat?.CancelSpecialAction();
    }

    private void OnEnable()
    {

    }

    private void Start()
    {
        if (Pet == null)
        {
            Pet = FindFirstObjectByType<PetController>();
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this);
        }

        if (GameProgressManager.Instance != null && GameProgressManager.Instance.CurrentData != null)
        {
            var data = GameProgressManager.Instance.CurrentData;
            FormType targetForm = data.currentWorld == "Devil" ? FormType.Devil : FormType.Normal;
            if (formManager != null)
            {
                formManager.InitializeFormData(); 
                formManager.TransformTo(targetForm);
            }
        }
        
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        UpdateTimers();
        inputReader.GatherInput();
        CheckCollisions();
        HandleWorldSwitch();

        if(IsGrounded())
        {
            timeSinceLanded += Time.deltaTime;
        }
        else
        {
            timeSinceLanded = 0;
        }
        
        // Grapple: 1순위: V키 그래플링 발동 로직 (선입력 지원)
        if (inputReader.GrappleInput)
        {
            grappleBufferTimer = GrappleData != null ? GrappleData.inputBufferTime : 0.15f;
        }

        // 타겟이 감지되었고, 버퍼가 남아있으면 즉시 발동 (다른 모든 상태 인터럽트)
        if (grappleBufferTimer > 0f && CanGrapple())
        {
            grappleBufferTimer = 0f; // 버퍼 소모
            
            // 진행 중인 공격/특수 행동 강제 종료 (어퍼컷 등 캔슬)
            Combat?.CancelSpecialAction();
            
            int capturedKey = GrappleDetector.NearestKey; // 콜라이더 식별 키(int)
            GrappleAimState.SetKey(capturedKey);
            StateMachine.ChangeState(GrappleAimState);
            return; // 다른 상태 로직 스킵 (확실한 전환 보장)
        }
        
        // 2순위: 패링 입력 감지 및 상태 전이
        if (ParryInput && CanParry())
        {
            StateMachine.ChangeState(ParryState);
        }
        // 변신 입력 감지 (단일 키 A)
        else if (ButtonAInput && CanTransform())
        {
            FormType nextForm = CurrentForm == FormType.Normal ? FormType.Devil : FormType.Normal;
            TransformState.SetTransform(nextForm, StateMachine.CurrentState);
            StateMachine.ChangeState(TransformState);
        }
        else
        {
            // 공격 선입력 소비
            if (Combat != null && !Combat.IsAttacking && !Combat.IsSpecialActionLocked && LastPressedAttackTime > 0)
            {
                LastPressedAttackTime = 0f;
                ProcessAttackInput();
            }

            // 3순위: 기본 상태 로직 업데이트 (Move, InAir, Glide, Attack 등)
            StateMachine.CurrentState?.LogicUpdate();
        }
        
#if UNITY_EDITOR
        currentStateName = StateMachine.CurrentState?.GetType().Name;
        if (animationController != null) currentAnimationName = animationController.CurrentPlayingAnimation;
#endif
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState?.PhysicsUpdate();
    }

    private void UpdateTimers()
    {
        float dt = Time.deltaTime;
        LastPressedJumpTime -= dt;
        LastPressedDashTime -= dt;
        LastPressedAttackTime -= dt;
        LastOnGroundTime -= dt;
        LastOnWallTime -= dt; // Wall Coyote Time 감소
        
        // 엣지 클라임 쿨타임 감소
        if (ledgeFailCooldownTimer > 0f)
            ledgeFailCooldownTimer -= dt;
            
        // 그래플링 선입력 버퍼 감소
        if (grappleBufferTimer > 0f)
            grappleBufferTimer -= dt;
    }



    public void ProcessAttackInput()
    {
        AttackDirection attackDir = GetAttackDirection();
        
        var downAttack = ActiveFormData?.attackProfile?.downAttack;
        
        if (CurrentForm == FormType.Devil && 
            !IsGrounded() && 
            attackDir == AttackDirection.Down &&
            downAttack is SlamAttackData slamData)
        {
            var slamAction = new SlamAction(this, slamData);
            Combat.StartSpecialAction(slamAction);
        }
        else if (StateMachine.CurrentState == InAirState && 
                 attackDir == AttackDirection.Up &&
                 ActiveFormData?.attackProfile?.upAttack is RisingAttackData risingData)
        {
            if (CanRisingAttack)
            {
                CanRisingAttack = false;
                var risingAction = new RisingAction(this, risingData);
                Combat.StartSpecialAction(risingAction);
            }
        }
        else
        {
            Combat.RequestAttack(attackDir);
        }
    }

    private AttackDirection GetAttackDirection()
    {

        float vertical = InputY;
        if (vertical > 0.5f) 
        {
            // 선택된 RisingAttack(공중 전용 어퍼컷)일 경우, 
            // 에어 스테이트가 아니면(즉 지상) Up 입력을 무시하고 일반 정면 공격(Normal)으로 변환
            if (ActiveFormData?.attackProfile?.upAttack is RisingAttackData)
            {
                if (StateMachine.CurrentState == InAirState)
                    return AttackDirection.Up;
                else
                    return AttackDirection.Normal;
            }
            return AttackDirection.Up;
        }
        if (vertical < -0.5f && !IsGrounded()) return AttackDirection.Down;
        return AttackDirection.Normal;
    }

    /// <summary>
    /// 차원 전환(D버튼) 로직 처리부
    /// </summary>
    private void HandleWorldSwitch()
    {
        // 1. 타이머 증가 판정 (조작 중이거나 Idle이 아니면 타이머 초기화)
        bool isMoving = Mathf.Abs(InputX) > 0.01f;
        bool isAnyAction = isMoving || JumpInputDown || DashInput || ButtonAInput || ParryInput;
        // 구역(IsInDimensionZone) 안에서만 충전 가능
        bool canCharge = StateMachine.CurrentState == IdleState && !isAnyAction && IsInDimensionZone;

        if (IsSwitchHeld)
        {
            if (canCharge && !isSwitchInterrupted)
            {
                switchHoldTimer += Time.deltaTime;

                if (switchHoldTimer >= targetSwitchHoldTime)
                {
                    // SceneLoader를 통한 씬 교체 방식으로 진입
                    if (SceneLoader.Instance != null && !SceneLoader.Instance.IsTransitioning)
                    {
                        RoomData currentRoomData = FindFirstObjectByType<RoomData>();
                        if (currentRoomData != null && !string.IsNullOrEmpty(currentRoomData.otherWorldSceneName))
                        {
                            WorldType targetWorld = currentRoomData.world == WorldType.Devil ? WorldType.Spirit : WorldType.Devil;
                            SceneLoader.Instance.SwitchDimensionRoom(currentRoomData.otherWorldSceneName, targetWorld);
                        }
                    }

                    // 연속 발동 방지 (D버튼 완전히 뗄 때까지 재발동 차단)
                    switchHoldTimer = -0.5f;
                    isSwitchInterrupted = true;
                }
            }
            else if (!canCharge && switchHoldTimer > 0f)
            {
                // 충전 중 조작 등이 들어오면 타이머 리셋 및 재입력 유도
                switchHoldTimer = 0f;
                isSwitchInterrupted = true;
            }
        }
        else
        {
            // 입력 떼기 감지하여 타이머 초기화
            if (switchHoldTimer > 0f)
            {
                switchHoldTimer = 0f;
            }
            else if (switchHoldTimer < 0f && !IsSwitchHeld)
            {
                switchHoldTimer = 0f; // 쿨타임 해제
            }

            // 떼면 재입력 차단 상태 해제
            if (!IsSwitchHeld)
            {
                isSwitchInterrupted = false;
            }
        }
    }

    private void CheckCollisions()
    {
        if (IsGrounded())
        {
            LastOnGroundTime = ActiveFormData.assist.coyoteTime;
            RefillAirAbilities();
        }
        else if (IsTouchingWall())
        {
            LastOnWallTime = ActiveFormData.wall.coyoteTime; // Wall Coyote Time 갱신
            LastWallDirection = IsFacingRight ? 1 : -1; // 벽 방향 저장
            RefillAirAbilities(); // 벽에 닿아도 공중 능력 초기화
        }
    }

    /// <summary>
    /// 대쉬, 더블 점프, 슬램/상승 공격 등 공중 체공 능력을 초기화합니다.
    /// (지상/벽 착지 또는 포고/그래플링 등에서 호출)
    /// </summary>
    public void RefillAirAbilities()
    {
        DashCountLeft = ActiveFormData.ability.amountOfDashes;
        CanRisingAttack = true;
        
        if (ActiveFormData.jump.hasDoubleJumpAbility)
        {
            CanDoubleJump = true;
        }
    }

    public void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != IsFacingRight)
        {
            IsFacingRight = isMovingRight;
            Vector3 scale = transform.localScale;
            scale.x = isMovingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    public void SwitchForm(int formIndex) => formManager.SwitchForm(formIndex);

    private Collider2D GetSolidColliderInBox(Vector2 point, Vector2 size, LayerMask mask)
    {
        // 트리거(isTrigger = true)를 무시하고 실체가있는 콜라이더만 반환
        Collider2D[] hits = Physics2D.OverlapBoxAll(point, size, 0, mask);
        foreach (var hit in hits)
        {
            if (!hit.isTrigger) return hit;
        }
        return null;
    }

    public bool IsGrounded()
    {
        return GetSolidColliderInBox(groundCheckPoint.position, groundCheckSize, GroundLayer) != null;
    }

    public bool IsTouchingWall()
    {
        Collider2D hit = GetSolidColliderInBox(wallCheckPoint.position, wallCheckSize, GroundLayer);
        if (hit == null) return false;

        // SurfaceType.Wall 검증
        // 1) 콜라이더 자신에서 직접 검색
        // 2) 타일맵일 경우 루트 오브젝트에 SurfaceInfo가 있으므로 부모 검색
        SurfaceInfo surface = null;
        if (!hit.TryGetComponent(out surface) && hit.transform.parent != null)
            hit.transform.parent.TryGetComponent(out surface);

        // SurfaceInfo가 있으면 정의된 type 속성 사용 (Wall인 경우만 벽으로 인정)
        if (surface != null)
            return surface.type == SurfaceType.Wall;

        // SurfaceInfo가 없는 지형은 밟을 수는 있되 벽타기는 불가
        return false; 
    }

    /// <summary>
    /// 천장 감지 (Pogo 중 머리 충돌 체크)
    /// </summary>
    public bool IsCeilinged()
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(0, 1.0f);
        // 플레이어 너비(약 0.8)보다 약간 작게(0.5) 설정하여 벽에 밀착했을 때 벽을 천장으로 오인하지 않도록 함
        Vector2 size = new Vector2(0.5f, 0.2f);
        return GetSolidColliderInBox(origin, size, GroundLayer) != null;
    }

    public bool IsTouchingGroundOnSide()
    {
        return GetSolidColliderInBox(wallCheckPoint.position, wallCheckSize, GroundLayer) != null;
    }

    public bool CanDash() => DashCountLeft > 0 && Time.time >= lastDashTime + ActiveFormData.ability.dashCooldown;
    public void OnDash()
    {
        lastDashTime = Time.time;
        DashCountLeft--; // 대쉬 횟수 차감
    }
    public bool CanWallJump() => Time.time >= lastWallJumpTime + ActiveFormData.wall.jumpCooldown;
    public void OnWallJump() => lastWallJumpTime = Time.time;
    public void SetGravityScale(float scale) => RB.gravityScale = scale;
    
    #region ActionSystem - Gravity/FallSpeed Override
    public void RequestGravityOverride(float scale) => actionController.RequestGravityOverride(scale);
    public void ClearGravityOverride() => actionController.ClearGravityOverride();
    public void RequestFallSpeedClamp(float maxSpeed) => actionController.RequestFallSpeedClamp(maxSpeed);
    public void ClearFallSpeedClamp() => actionController.ClearFallSpeedClamp();
    public float EffectiveGravityScale => actionController.EffectiveGravityScale;
    public void ApplyFallSpeedClamp() => actionController.ApplyFallSpeedClamp();
    #endregion
    
    /// <summary>
    /// 현재 벽 방향을 반환 (1 = 오른쪽, -1 = 왼쪽, 0 = 벽 없음)
    /// WallSlide/WallJump에서 Facing 대비 벽 기준 판정시 사용
    /// </summary>
    public int WallDirection
    {
        get
        {
            if (IsTouchingWall())
                return IsFacingRight ? 1 : -1;
            else if (LastOnWallTime > 0)
                return LastWallDirection; // Coyote Time 중에는 마지막 벽 방향 사용
            return 0;
        }
    }
    
    /// <summary>
    /// 벽점프가 가능한 Jump Buffer가 활성화되어 있는지 체크
    /// (Jump Buffer + Wall Contact + 코요테 통합)
    /// </summary>
    public bool HasBufferedWallJump()
    {
        return LastPressedJumpTime > 0 && CanWallJump() && LastOnWallTime > 0;
    }

    /// <summary>
    /// 벽점프 시도 (Try 패턴) - 성공 시 true 반환 및 상태 전환
    /// WallSlide, InAir에서 호출하여 벽점프 시도를 우선 처리
    /// </summary>
    public bool TryWallJump()
    {
        // Jump Buffer + WallContact + 코요테 체크
        if (!HasBufferedWallJump())
            return false;
        
        // 벽 방향으로 입력하고 있는지 확인 (벽 기준, Facing 무시)
        bool isHoldingTowardsWall = InputX != 0 && Mathf.Sign(InputX) == WallDirection;
        
        if (isHoldingTowardsWall)
        {
            // 수직 벽 타기(WallClimbState)
            StateMachine.ChangeState(WallClimbState);
        }
        else
        {
            // 벽 반대 점프 (WallJumpState)
            StateMachine.ChangeState(WallJumpState);
        }
        
        // Jump Buffer 및 Coyote Time 소진
        LastPressedJumpTime = 0;
        LastOnWallTime = 0;
        
        return true; // 벽점프 성공
    }
    
    public void RestAt(float centerX)
    {
        RestState.SetTargetX(centerX);
        StateMachine.ChangeState(RestState);
    }
    
    public void TransformTo(FormType targetForm) => formManager.TransformTo(targetForm);
    
    /// <summary>
    /// 체공 시작 (Single Source of Truth)
    /// </summary>
    public void BeginGlide()
    {
        if (IsGliding) return; // 중복 호출 방지
        
        IsGliding = true;
        PlayerEvents.RaiseGlideStart();
        
        // formManager.SetGlidingSprite(true); // 스프라이트 강제 교체 비활성화
    }
    
    /// <summary>
    /// 체공 강제 종료 (Fail-safe)
    /// </summary>
    public void ForceEndGlide()
    {
        if (!IsGliding) return; // 중복 호출 방지
        
        IsGliding = false;
        PlayerEvents.RaiseGlideEnd();
        
        // formManager.SetGlidingSprite(false); // 스프라이트 강제 교체 비활성화
    }
    
    private void OnDestroy()
    {
        ForceEndGlide();
    }
    
    // ==================== Health Callbacks ====================
    
    /// <summary>
    /// PlayerHealth에서 피격 시 호출되는 콜백
    /// </summary>
    public void OnDamaged(DamageInfo damageInfo)
    {
        // 공격 중이면 강제 중단 (Animator.speed 복구 포함)
        Combat?.InterruptAttack();
        
        // 특수 행동 중이면 강제 취소 (Slam 등)
        Combat?.CancelSpecialAction();
        
        // 변신 중이면 변신 취소
        if (StateMachine.CurrentState == TransformState)
        {
            // 변신 실패 - 지상/공중 상태로 복귀
            if (IsGrounded())
            {
                StateMachine.ChangeState(IdleState);
            }
            else
            {
                StateMachine.ChangeState(InAirState);
            }
            
            // 중력 복구 (TransformState.Exit()가 호출되므로 자동 처리)
        }
        
        // HitState에 데미지 정보 전달
        HitState.SetDamageInfo(damageInfo);
        
        // HitState로 전환 (현재 State와 관계없이 즉시 전환)
        StateMachine.ChangeState(HitState);
    }
    
    /// <summary>
    /// PlayerHealth에서 사망 시 호출되는 콜백
    /// </summary>
    public void OnDeath()
    {
        // DeathState로 전환
        StateMachine.ChangeState(DeathState);
    }
    
    // ==================== Grapple Logic ====================

    /// <summary>
    /// 그래플링 가능 여부 체크.
    /// GrappleDetector에 유효 타겟이 있고 불가 상태가 아닐 때만 true.
    /// </summary>
    public bool CanGrapple()
    {
        if (GrappleDetector == null || !GrappleDetector.HasTarget)
            return false;

        // (프리즈 코루틴은 Time-Slow 조작 상태로 대체되어 제거)

        // 전역 쿨타임 체크
        if (grappleData != null && Time.time < lastGrappleTime + grappleData.globalCooldown)
            return false;

        var state = StateMachine.CurrentState;
        if (state == GrapplingState ||
            state == HitState       ||
            state == DeathState     ||
            state == LedgeClimbState)
        {
            return false;
        }

        return true;
    }
    
    // ==================== Parry Logic ====================

    
    public float LastParryEndTime { get; set; } = -10f;
    
    /// <summary>
    /// 현재 패링 시도가 가능한 상태인지 확인 (쿨다운, 현재 상태 등 체크)
    /// </summary>
    public bool CanParry()
    {
        // 쿨다운 체크
        if (Time.time < LastParryEndTime + ActiveFormData.parry.cooldown)
            return false;
            
        // 패링 불가 상태 제한
        var state = StateMachine.CurrentState;
        if (state == ParryState || state == HitState || state == DeathState || state == TransformState || 
            state == LedgeClimbState || state == WallClimbState)
        {
            return false;
        }
        
        // 특수 행동 (Slam 등) 중이면 불가
        if (Combat != null && Combat.IsSpecialActionActive)
            return false;
            
        // 윗 방향키와 함께 눌렀을 때는 폼 변신이 우선되어야 함
        if (InputY > 0.5f)
            return false;

        return true;
    }

    /// <summary>
    /// 현재 조작 가능한 상태이며 변신이 가능한지 확인
    /// </summary>
    public bool CanTransform()
    {
        var state = StateMachine.CurrentState;
        
        // 조작 불가 및 특수 상태에서는 변신 불가
        if (state == HitState || 
            state == DeathState || 
            state == TransformState || 
            state == LedgeClimbState || 
            state == GrappleAimState || 
            state == GrapplingState ||
            state == ParryState ||
            state == DashState)
        {
            return false;
        }

        // 특수 공격 행동 중 변신 불가
        if (Combat != null && (Combat.IsAttacking || Combat.IsSpecialActionActive))
        {
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// 공격을 받았을 때 패링 가능한 공격인지 판정하고 성공 시 처리
    /// PlayerHealth.TakeDamage() 에서 호출됨
    /// </summary>
    public bool TryParry(DamageInfo info)
    {
        if (StateMachine.CurrentState != ParryState) return false;
        if (!ParryState.IsActiveWindow) return false;
        if (!info.canBeParried) return false;
        
        // 방향 체크 (Directional Check)
        // 플레이어 시선 반대 방향에서 다가오는 공격만 패링 (Dot > 0 이면 같은 방향 = 뒤통수)
        // hitDirection은 원점에서 플레이어 방향을 향한다고 가정
        Vector2 facingVector = new Vector2(IsFacingRight ? 1 : -1, 0);
        if (Vector2.Dot(facingVector, info.hitDirection) > 0)
        {
            // 공격자가 플레이어 뒤에서 때림 -> 패링 실패
            return false;
        }
        
        // 패링 성공! 플래그 설정
        ParryState.SetSuccess();

        // 패링 성공 시 스킬 게이지(소울) 획득
        var skillResource = GetComponent<PlayerSkillResource>();
        if (skillResource != null)
        {
            skillResource.AddGauge(ActiveFormData.skillResource.gainOnParry);
        }
        
        // 넉백 강도 및 방향 계산
        float knockbackDirX = Mathf.Sign(transform.position.x - info.damageSource.x);
        if (knockbackDirX == 0) knockbackDirX = IsFacingRight ? -1f : 1f;

        Vector2 finalKnockback = new Vector2(
            knockbackDirX * ActiveFormData.parry.successKnockbackForce.x, 
            ActiveFormData.parry.successKnockbackForce.y
        );

        // 넉백 실행 (Snappy Recoil)
        if (ActiveFormData.parry.successKnockbackForce != Vector2.zero && ActiveFormData.parry.successKnockbackDuration > 0)
        {
            StartRecoil(finalKnockback, ActiveFormData.parry.successKnockbackDuration);
        }
        
        // 피드백 및 이벤트 트리거
        PlayerEvents.RaiseParrySuccess(info);
        
        return true; // TakeDamage 취소
    }

    /// <summary>
    /// 엣지 클라임 실패 쿨타임 시작
    /// </summary>
    public void StartLedgeFailCooldown(float duration)
    {
        ledgeFailCooldownTimer = duration;
    }

    /// <summary>
    /// 점프 버퍼를 즉시 소모하여 무효화합니다.
    /// Sprint Turn, 피격 상태 등 점프가 절대 불가능해야 하는 상태에서 호출합니다.
    /// </summary>
    public void ConsumeJumpBuffer()
    {
        LastPressedJumpTime = 0;
    }

    private void OnDrawGizmos()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        }
        if (wallCheckPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(wallCheckPoint.position, wallCheckSize);
        }
        
        
        // Ledge Climb Gizmo는 LedgeDetector에서 표시
    }
    

    #endregion
}
