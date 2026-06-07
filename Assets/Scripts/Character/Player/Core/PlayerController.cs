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
    
    // 李⑥썝 ?꾪솚 ?€?대㉧
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
    public PlayerHealth Health { get; private set; } // Awake?먯꽌 罹먯떛
    public LedgeDetector LedgeDetector { get; private set; }
    public PlayerCombat Combat { get; private set; }
    public GrappleDetector GrappleDetector { get; private set; }
    [SerializeField] private GrappleData grappleData;
    public GrappleData GrappleData => grappleData;

    private SpriteRenderer spriteRenderer;



    // ?€?€ ?좉퇋 ?꾨떞 而댄룷?뚰듃 李몄“ ?€?€
    public PlayerInputReader inputReader { get; private set; }
    public PlayerFormManager formManager { get; private set; }
    public PlayerActionController actionController { get; private set; }
    public Mado.AnimationSystem.CharacterSpriteAnimator SpriteAnimator { get; private set; }

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
#endif
    #endregion

    #region Unity Callback Functions
    private void Awake()
    {
        // 1. ?꾩닔 而댄룷?뚰듃 罹먯떛
        Health = GetComponent<PlayerHealth>();
        inputReader = GetComponent<PlayerInputReader>();
        formManager = GetComponent<PlayerFormManager>();
        actionController = GetComponent<PlayerActionController>();
        RB = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        SpriteAnimator = GetComponentInChildren<Mado.AnimationSystem.CharacterSpriteAnimator>();


        var boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            boxCol.edgeRadius = 0.015f;
        }
        
        LedgeDetector = GetComponentInChildren<LedgeDetector>(); 
        Combat = GetComponent<PlayerCombat>(); 
        GrappleDetector = GetComponent<GrappleDetector>(); 

        // 2. [Build Optimization] 臾쇰━ 蹂닿컙 諛??꾨젅???덉젙??
        if (RB != null) RB.interpolation = RigidbodyInterpolation2D.Interpolate;
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = -1;

        // 3. ?쒖뒪??珥덇린??
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
        
        // Grapple: 1?쒖쐞: V??洹몃옒?뚮쭅 諛쒕룞 濡쒖쭅 (?좎엯??吏??
        if (inputReader.GrappleInput)
        {
            grappleBufferTimer = GrappleData != null ? GrappleData.inputBufferTime : 0.15f;
        }

        // ?ъ씤??媛먯?媛 ?섏뿀怨? 踰꾪띁媛 ?⑥븘?덉쑝硫?利됯컖 諛쒕룞 (?ㅻⅨ 紐⑤뱺 ?곹깭 ?명꽣?쏀듃)
        if (grappleBufferTimer > 0f && CanGrapple())
        {
            grappleBufferTimer = 0f; // 踰꾪띁 ?뚮え
            
            // 吏꾪뻾 以묒씠??怨듦꺽 ?뱀닔 ?됰룞 媛뺤젣 醫낅즺 (?댄띁而? ?щ옩 ??
            Combat?.CancelSpecialAction();
            
            int capturedKey = GrappleDetector.NearestKey; // 肄쒕씪?대뜑 ?덉젙 ??(int)
            GrappleAimState.SetKey(capturedKey);
            StateMachine.ChangeState(GrappleAimState);
            return; // ?ㅻⅨ ?곹깭 濡쒖쭅 ?ㅽ궢 (?뺤떎???꾪솚 蹂댁옣)
        }
        
        // 2?쒖쐞: ?⑤쭅 ?낅젰 媛먯? 諛??곹깭 ?꾩씠
        if (ParryInput && CanParry())
        {
            StateMachine.ChangeState(ParryState);
        }
        else
        {
            // 怨듦꺽 ?좎엯???뚮퉬
            if (Combat != null && !Combat.IsAttacking && !Combat.IsSpecialActionLocked && LastPressedAttackTime > 0)
            {
                LastPressedAttackTime = 0f;
                ProcessAttackInput();
            }

            // 3?쒖쐞: 湲곕낯 ?곹깭 濡쒖쭅 ?낅뜲?댄듃 (Move, InAir, Glide, Attack ??
            StateMachine.CurrentState?.LogicUpdate();
        }
        
#if UNITY_EDITOR
        currentStateName = StateMachine.CurrentState?.GetType().Name;
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
        LastOnWallTime -= dt; // Wall Coyote Time 媛먯냼
        
        // ?쏆? ?대씪??荑⑦???媛먯냼
        if (ledgeFailCooldownTimer > 0f)
            ledgeFailCooldownTimer -= dt;
            
        // 洹몃옒?뚮쭅 ?좎엯??踰꾪띁 媛먯냼
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
            // ???댄깮??RisingAttack(怨듭쨷 ?꾩슜 ?댄띁而???寃쎌슦, 
            // ?먯뼱 ?ㅽ뀒?댄듃媛 ?꾨땲硫??? 吏?? Up ?낅젰??臾댁떆?섍퀬 ?쇰컲 ?뺣㈃ 怨듦꺽(Normal)?쇰줈 蹂??
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
    /// 李⑥썝 ?꾪솚(D????? 濡쒖쭅 ?쒗??
    /// </summary>
    private void HandleWorldSwitch()
    {
        // 1. ??대㉧ 利앷? ?먯젙 (議곗옉 以묒씠嫄곕굹 Idle???꾨땲硫???대㉧ 珥덇린??
        bool isMoving = Mathf.Abs(InputX) > 0.01f;
        bool isAnyAction = isMoving || JumpInputDown || DashInput || ButtonAInput || ParryInput;
        // 援ъ뿭(IsInDimensionZone) ?댁뿉?쒕쭔 異⑹쟾 媛??
        bool canCharge = StateMachine.CurrentState == IdleState && !isAnyAction && IsInDimensionZone;

        if (IsSwitchHeld && DimensionManager.Instance != null)
        {
            if (canCharge && !isSwitchInterrupted)
            {
                switchHoldTimer += Time.deltaTime;

                if (switchHoldTimer >= targetSwitchHoldTime)
                {
                    // SceneLoader瑜??듯븳 ??援먯껜 諛⑹떇?쇰줈 ?꾩엫
                    DimensionManager.Instance.RequestDimensionSwitch();

                    // ?곗냽 諛쒕룞 諛⑹? (D???꾩쟾?????뚭퉴吏 ?щ컻??李⑤떒)
                    switchHoldTimer = -0.5f;
                    isSwitchInterrupted = true;
                }
            }
            else if (!canCharge && switchHoldTimer > 0f)
            {
                // 異⑹쟾 以?議곗옉 ?깆씠 ?ㅼ뼱?ㅻ㈃ ??대㉧ 由ъ뀑 諛??ъ엯???좊룄
                switchHoldTimer = 0f;
                isSwitchInterrupted = true;
            }
        }
        else
        {
            // ?낅젰 ?쇨린 媛먯??섏뿬 ??대㉧ 珥덇린??
            if (switchHoldTimer > 0f)
            {
                switchHoldTimer = 0f;
            }
            else if (switchHoldTimer < 0f && !IsSwitchHeld)
            {
                switchHoldTimer = 0f; // 荑⑦????댁젣
            }

            // ?ㅻ? ?쇰㈃ 以묐떒 ?곹깭 ?댁젣
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
            LastOnWallTime = ActiveFormData.wall.coyoteTime; // Wall Coyote Time 媛깆떊
            LastWallDirection = IsFacingRight ? 1 : -1; // 踰?諛⑺뼢 ???
            RefillAirAbilities(); // ?낆뿉 ?우븘??怨듭쨷 ?λ젰 珥덇린??
        }
    }

    /// <summary>
    /// ??? ?붾툝 ?먰봽, ?쇱씠吏?怨듦꺽 ??怨듭쨷 泥닿났 ?λ젰??珥덇린?뷀빀?덈떎.
    /// (吏??踰?李⑹? ?먮뒗 ?ш퀬/洹몃옒?뚮쭅 ???몄텧)
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
        // ?몃━嫄?isTrigger = true)瑜?臾댁떆?섍퀬 ?ㅼ껜?섎뒗 肄쒕씪?대뜑留?諛섑솚
        Collider2D[] hits = Physics2D.OverlapBoxAll(point, size, 0, mask);
        foreach (var hit in hits)
        {
            if (!hit.isTrigger) return hit;
        }
        return null;
    }

    public bool IsGrounded()
    {
        // DimensionManager.CurrentWorldMask留??ъ슜
        LayerMask groundMask = DimensionManager.Instance.CurrentWorldMask;
        return GetSolidColliderInBox(groundCheckPoint.position, groundCheckSize, groundMask) != null;
    }

    public bool IsTouchingWall()
    {
        LayerMask mask = DimensionManager.Instance.CurrentWorldMask;

        Collider2D hit = GetSolidColliderInBox(wallCheckPoint.position, wallCheckSize, mask);
        if (hit == null) return false;

        // SurfaceType.Wall 寃利?
        // 1) 肄쒕씪?대뜑 ?먯떊?먯꽌 吏곸젒 ?먯깋
        // 2) ??쇰㏊??寃쎌슦 猷⑦듃 ?ㅻ툕?앺듃??SurfaceInfo媛 ?덉쑝誘濡?遺紐??먯깋
        SurfaceInfo surface = null;
        if (!hit.TryGetComponent(out surface) && hit.transform.parent != null)
            hit.transform.parent.TryGetComponent(out surface);

        // SurfaceInfo媛 ?덉쑝硫??뺤쓽??type ?띿꽦 ?ъ슜 (Wall??寃쎌슦留?踰쎌쑝濡??몄젙)
        if (surface != null)
            return surface.type == SurfaceType.Wall;

        // SurfaceInfo媛 ?녿뒗 吏?뺤? 諛잛쓣 ?섎뒗 ?덈릺 踰쏀?湲곕뒗 遺덇?
        return false; 
    }

    /// <summary>
    /// 泥쒖옣 媛먯? (Pogo 以?癒몃━ 異⑸룎 泥댄겕??
    /// </summary>
    public bool IsCeilinged()
    {
        LayerMask mask = DimensionManager.Instance.CurrentWorldMask;
        Vector2 origin = (Vector2)transform.position + new Vector2(0, 1.0f);
        // ?뚮젅?댁뼱 ?덈퉬(??0.8)蹂대떎 ?쎄컙 ?묎쾶(0.5) ?ㅼ젙?섏뿬 踰쎌뿉 諛李⑺뻽????踰쎌쓣 泥쒖옣?쇰줈 ?ㅼ씤?섏? ?딅룄濡???
        Vector2 size = new Vector2(0.5f, 0.2f);
        return GetSolidColliderInBox(origin, size, mask) != null;
    }

    public bool IsTouchingGroundOnSide()
    {
        LayerMask mask = DimensionManager.Instance.CurrentWorldMask;
        return GetSolidColliderInBox(wallCheckPoint.position, wallCheckSize, mask) != null;
    }

    public bool CanDash() => DashCountLeft > 0 && Time.time >= lastDashTime + ActiveFormData.ability.dashCooldown;
    public void OnDash()
    {
        lastDashTime = Time.time;
        DashCountLeft--; // ????잛닔 李④컧
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
    /// ?꾩옱 踰?諛⑺뼢??諛섑솚 (1 = ?ㅻⅨ履? -1 = ?쇱そ, 0 = 踰??놁쓬)
    /// WallSlide/WallJump?먯꽌 Facing ???踰?湲곗? ?먯젙???ъ슜
    /// </summary>
    public int WallDirection
    {
        get
        {
            if (IsTouchingWall())
                return IsFacingRight ? 1 : -1;
            else if (LastOnWallTime > 0)
                return LastWallDirection; // Coyote Time 以묒뿉??留덉?留?踰?諛⑺뼢 ?ъ슜
            return 0;
        }
    }
    
    /// <summary>
    /// 踰쎌젏??媛?ν븳 Jump Buffer媛 ?쒖꽦?붾릺???덈뒗吏 泥댄겕
    /// (Jump Buffer + Wall Contact + 荑⑦????듯빀)
    /// </summary>
    public bool HasBufferedWallJump()
    {
        return LastPressedJumpTime > 0 && CanWallJump() && LastOnWallTime > 0;
    }

    /// <summary>
    /// 踰쎌젏???쒕룄 (Try ?⑦꽩) - ?깃났 ??true 諛섑솚 諛??곹깭 ?꾪솚
    /// WallSlide, InAir?먯꽌 ?몄텧?섏뿬 踰쎌젏???섎룄瑜??곗꽑 泥섎━
    /// </summary>
    public bool TryWallJump()
    {
        // Jump Buffer + WallContact + 荑⑦???泥댄겕
        if (!HasBufferedWallJump())
            return false;
        
        // 踰?諛⑺뼢?쇰줈 ?낅젰?섍퀬 ?덈뒗吏 ?뺤씤 (踰?湲곗?, Facing 臾댁떆)
        bool isHoldingTowardsWall = InputX != 0 && Mathf.Sign(InputX) == WallDirection;
        
        if (isHoldingTowardsWall)
        {
            // ?섏쭅 踰??湲?(WallClimbState)
            StateMachine.ChangeState(WallClimbState);
        }
        else
        {
            // 踰?諛섎? ?먰봽 (WallJumpState)
            StateMachine.ChangeState(WallJumpState);
        }
        
        // Jump Buffer 諛?Coyote Time ?뚯쭊
        LastPressedJumpTime = 0;
        LastOnWallTime = 0;
        
        return true; // 踰쎌젏???깃났
    }
    
    public void TransformTo(FormType targetForm) => formManager.TransformTo(targetForm);
    
    /// <summary>
    /// ?쒓났 ?쒖옉 (Single Source of Truth)
    /// </summary>
    public void BeginGlide()
    {
        if (IsGliding) return; // 以묐났 ?몄텧 ?덉쟾
        
        IsGliding = true;
        PlayerEvents.RaiseGlideStart();
        
        // formManager.SetGlidingSprite(true); // ?ㅽ봽?쇱씠??媛뺤젣 援먯껜 鍮꾪솢?깊솕
    }
    
    /// <summary>
    /// ?쒓났 媛뺤젣 醫낅즺 (Fail-safe)
    /// </summary>
    public void ForceEndGlide()
    {
        if (!IsGliding) return; // 以묐났 ?몄텧 ?덉쟾
        
        IsGliding = false;
        PlayerEvents.RaiseGlideEnd();
        
        // formManager.SetGlidingSprite(false); // ?ㅽ봽?쇱씠??媛뺤젣 援먯껜 鍮꾪솢?깊솕
    }
    
    private void OnDestroy()
    {
        ForceEndGlide();
    }
    
    // ==================== Health Callbacks ====================
    
    /// <summary>
    /// PlayerHealth?먯꽌 ?쇨꺽 ???몄텧?섎뒗 肄쒕갚
    /// </summary>
    public void OnDamaged(DamageInfo damageInfo)
    {
        // 怨듦꺽 以묒씠硫?媛뺤젣 以묐떒 (Animator.speed 蹂듦뎄 ?ы븿)
        Combat?.InterruptAttack();
        
        // ?뱀닔 ?됰룞 以묒씠硫?媛뺤젣 痍⑥냼 (Slam ??
        Combat?.CancelSpecialAction();
        
        // 蹂??以묒씠硫?蹂??痍⑥냼
        if (StateMachine.CurrentState == TransformState)
        {
            // 蹂???ㅽ뙣 - 吏??怨듭쨷 ?곹깭濡?蹂듦?
            if (IsGrounded())
            {
                StateMachine.ChangeState(IdleState);
            }
            else
            {
                StateMachine.ChangeState(InAirState);
            }
            
            // 以묐젰 蹂듦뎄 (TransformState.Exit()媛 ?몄텧?섎?濡??먮룞 泥섎━??
        }
        
        // HitState???곕?吏 ?뺣낫 ?꾨떖
        HitState.SetDamageInfo(damageInfo);
        
        // HitState濡??꾪솚 (?꾩옱 State? 愿怨꾩뾾??利됱떆 ?꾪솚)
        StateMachine.ChangeState(HitState);
    }
    
    /// <summary>
    /// PlayerHealth?먯꽌 ?щ쭩 ???몄텧?섎뒗 肄쒕갚
    /// </summary>
    public void OnDeath()
    {
        // DeathState濡??꾪솚
        StateMachine.ChangeState(DeathState);
    }
    
    // ==================== Grapple Logic ====================

    /// <summary>
    /// 洹몃옒?뚮쭅 媛???щ? 泥댄겕.
    /// GrappleDetector???좏슚 ?寃잛씠 ?덇퀬 遺덇? ?곹깭媛 ?꾨땺 ?뚮쭔 true.
    /// </summary>
    public bool CanGrapple()
    {
        if (GrappleDetector == null || !GrappleDetector.HasTarget)
            return false;

        // (?꾨━利?肄붾（?댁? Time-Slow 議곗? ?곹깭濡??泥대릺???쒓굅??

        // ?꾩뿭 荑⑦???泥댄겕
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
    /// ?꾩옱 ?⑤쭅 ?쒕룄媛 媛?ν븳 ?곹깭?몄? ?뺤씤 (荑⑤떎?? ?꾩옱 ?곹깭 ??泥댄겕)
    /// </summary>
    public bool CanParry()
    {
        // 荑⑤떎??泥댄겕
        if (Time.time < LastParryEndTime + ActiveFormData.parry.cooldown)
            return false;
            
        // ?⑤쭅 遺덇? ?곹깭 ?쒗븳
        var state = StateMachine.CurrentState;
        if (state == ParryState || state == HitState || state == DeathState || state == TransformState || 
            state == LedgeClimbState || state == WallClimbState)
        {
            return false;
        }
        
        // ?뱀닔 ?됰룞 (Slam ?? 以묒씠硫?遺덇?
        if (Combat != null && Combat.IsSpecialActionActive)
            return false;
            
        // ??諛⑺뼢?ㅼ? ?④퍡 ?뚮????뚮뒗 ??蹂?좎씠 ?곗꽑?섏뼱????
        if (InputY > 0.5f)
            return false;

        return true;
    }
    
    /// <summary>
    /// 怨듦꺽??諛쏆븯?????⑤쭅 媛?ν븳 怨듦꺽?몄? ?먯젙?섍퀬 ?깃났 ??泥섎━
    /// PlayerHealth.TakeDamage() ?먯꽌 ?몄텧??
    /// </summary>
    public bool TryParry(DamageInfo info)
    {
        if (StateMachine.CurrentState != ParryState) return false;
        if (!ParryState.IsActiveWindow) return false;
        if (!info.canBeParried) return false;
        
        // 諛⑺뼢 泥댄겕 (Directional Check)
        // ?뚮젅?댁뼱 ?쒖꽑 諛섎? 諛⑺뼢?먯꽌 ?ㅺ??ㅻ뒗 怨듦꺽留??⑤쭅 (Dot > 0 ?대㈃ 媛숈? 諛⑺뼢 = ????
        // hitDirection???먯젏?먯꽌 ?뚮젅?댁뼱 諛⑺뼢???ν븳?ㅺ퀬 媛??
        Vector2 facingVector = new Vector2(IsFacingRight ? 1 : -1, 0);
        if (Vector2.Dot(facingVector, info.hitDirection) > 0)
        {
            // 怨듦꺽?먭? ?????ㅼ뿉???뚮┝ -> ?⑤쭅 ?ㅽ뙣
            return false;
        }
        
        // ?⑤쭅 ?깃났! ?뚮옒洹??ㅼ젙
        ParryState.SetSuccess();

        // ?⑤쭅 ?깃났 ???ㅽ궗 寃뚯씠吏(?뚯슱) ?띾뱷
        var skillResource = GetComponent<PlayerSkillResource>();
        if (skillResource != null)
        {
            skillResource.AddGauge(ActiveFormData.skillResource.gainOnParry);
        }
        
        // ?됰갚 媛뺣룄 諛?諛⑺뼢 怨꾩궛
        float knockbackDirX = Mathf.Sign(transform.position.x - info.damageSource.x);
        if (knockbackDirX == 0) knockbackDirX = IsFacingRight ? -1f : 1f;

        Vector2 finalKnockback = new Vector2(
            knockbackDirX * ActiveFormData.parry.successKnockbackForce.x, 
            ActiveFormData.parry.successKnockbackForce.y
        );

        // ?됰갚 ?ㅽ뻾 (Snappy Recoil)
        if (ActiveFormData.parry.successKnockbackForce != Vector2.zero && ActiveFormData.parry.successKnockbackDuration > 0)
        {
            StartRecoil(finalKnockback, ActiveFormData.parry.successKnockbackDuration);
        }
        
        // ?쇰뱶諛?諛??대깽???몃━嫄?
        PlayerEvents.RaiseParrySuccess(info);
        
        return true; // TakeDamage 痍⑥냼
    }

    /// <summary>
    /// ?쏆? ?대씪???ㅽ뙣 荑⑦????쒖옉
    /// </summary>
    public void StartLedgeFailCooldown(float duration)
    {
        ledgeFailCooldownTimer = duration;
    }

    /// <summary>
    /// ?먰봽 踰꾪띁瑜?利됱떆 ?뚮え?섏뿬 臾댄슚?뷀빀?덈떎.
    /// Sprint Turn, ?쇨꺽 ?곹깭 ???먰봽媛 ?덈? 遺덇??ν빐???섎뒗 ?곹깭?먯꽌 ?몄텧?⑸땲??
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
        
        
        // Ledge Climb Gizmo??LedgeDetector?먯꽌 ?쒖떆
    }
    

    #endregion
}
