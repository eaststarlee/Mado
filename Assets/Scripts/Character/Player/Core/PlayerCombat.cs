using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 怨듦꺽 ?됰룞 愿由ъ옄 (?곹깭媛 ?꾨땶 ?됰룞 ?덉씠??
/// 
/// 梨낆엫:
/// - 怨듦꺽 ?붿껌 ?섏떊 諛?議곗쑉
/// - ?몄뀡 ?앹꽦 諛??섎챸 愿由?
/// - ?쒖빟(Constraint) ?뚮옒洹?愿由?
/// - 諛섎룞(Recoil) ?붿껌 ?꾨떖
/// 
/// 梨낆엫 遺꾨━:
/// - ?덊듃 ?먯젙: HitResolver
/// - ?쇰뱶諛? CombatFeedback
/// - ??대컢? AttackData.baseAnimDuration 湲곗?
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    [Header("?ㅼ젙")]
    [SerializeField] private CombatConfig config;
    [SerializeField] private LayerMask enemyLayer;
    
    [Header("諛섎룞(Recoil) ?ㅼ젙")]
    [Tooltip("怨듦꺽 ???뚮젅?댁뼱瑜??ㅻ줈 諛?ㅻ굹寃???????덉씠??(?? Enemy, Ground ??")]
    public LayerMask recoilTriggerLayers;
    
    [Tooltip("怨듦꺽 ???뚮젅?댁뼱瑜??ㅻ줈 諛?ㅻ굹寃????쒕㈃(Surface) ??낅뱾")]
    public List<SurfaceType> recoilTriggerSurfaces;
    
    [Tooltip("諛섎룞 ?섏쓽 諛곗쑉 (1.0 = AttackData??湲곕낯媛?")]
    [Range(0.1f, 5.0f)]
    public float recoilForceMultiplier = 1.0f;
    
    private PlayerController player;
    private HitResolver hitResolver;
    
    #region Properties
    
    /// <summary>
    /// ?꾩옱 ?쇱쓽 怨듦꺽 ?꾨줈??
    /// </summary>
    private FormAttackProfile CurrentProfile => player.ActiveFormData?.attackProfile;
    
    /// <summary>
    /// 怨듦꺽 以??щ?
    /// </summary>
    public bool IsAttacking { get; private set; }
    
    /// <summary>
    /// ?대룞 ?좉툑 ?뚮옒洹?(Constraint)
    /// MoveState 諛?ActionSystem?먯꽌 李몄“
    /// </summary>
    public bool LockMovement { get; set; }
    
    /// <summary>
    /// Enemy Layer (SlamAction ?깆뿉??李몄“)
    /// </summary>
    public LayerMask EnemyLayer => enemyLayer;
    
    #endregion
    
    #region Private State
    
    // ?꾩옱 怨듦꺽 ?몄뀡
    private AttackSession currentSession;
    private AttackData currentAttack;
    private float attackTimer;
    private float cooldownTimer;
    private bool hasHit;
    
    // 諛⑺뼢 ?ㅻ깄??(怨듦꺽 ?쒖옉 ?쒓컙 怨좎젙)
    private int snapshotFacing;
    private AttackDirection snapshotDirection;
    
    
    // ?낅젰 踰꾪띁
    private float inputBufferTimer;
    private bool hasBufferedAttack;
    private AttackDirection bufferedDirection;
    
    // Pogo 愿由?
    private Coroutine pogoCoroutine;
    private bool isPogoActive;
    
    // Special Action Runner (Slam ??
    private ISpecialAction currentSpecialAction;
    private ActionHandle currentActionHandle;
    
    /// <summary>
    /// Pogo ?쒖꽦 ?곹깭 (?몃? 李몄“??
    /// </summary>
    public bool IsPogoActive => isPogoActive;
    
    /// <summary>
    /// ?뱀닔 ?됰룞 ?쒖꽦 ?щ?
    /// </summary>
    public bool IsSpecialActionActive => currentSpecialAction != null;

    /// <summary>
    /// ?뱀닔 ?됰룞 以??낅젰 ?좉툑 ?щ?
    /// </summary>
    public bool IsSpecialActionLocked => currentSpecialAction != null && currentSpecialAction.LocksInput;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }
    
    private void Start()
    {
        hitResolver = HitResolver.Instance ?? FindFirstObjectByType<HitResolver>();
        
        // HitResolver媛 ?ъ뿉 ?놁쑝硫?寃쎄퀬
        if (hitResolver == null)
        {
            Debug.LogWarning("[PlayerCombat] HitResolver瑜?李얠쓣 ???놁뒿?덈떎. 怨듦꺽 ?먯젙???묐룞?섏? ?딆뒿?덈떎.");
        }

        // 媛??Spike) ?ш퀬 ?먮룞 吏???깅줉
        if (recoilTriggerSurfaces != null && !recoilTriggerSurfaces.Contains(SurfaceType.Spike))
        {
            recoilTriggerSurfaces.Add(SurfaceType.Spike);
        }
    }
    
    private void Update()
    {
        UpdateTimers();
        ProcessAttack();
        ProcessBufferedAttack();
        
        // Special Action Update
        currentSpecialAction?.Update(Time.deltaTime);
    }
    
    // FixedUpdate ?쒓굅??(Pogo???댁젣 肄붾（??湲곕컲?대?濡?臾쇰━ ?낅뜲?댄듃 遺덊븘??
    
    // ...

    /// <summary>
    /// 怨듦꺽 ?붿껌 (PlayerController.GatherInput?먯꽌 ?몄텧)
    /// </summary>
    /// <param name="direction">怨듦꺽 諛⑺뼢</param>
    public void RequestAttack(AttackDirection direction)
    {
        // ?곗씠?곌? ?놁쑝硫?利됱떆 珥덇린???쒕룄
        if (CurrentProfile == null)
        {
            var formManager = player.GetComponent<PlayerFormManager>();
            if (formManager != null) formManager.InitializeFormData();
            
            if (CurrentProfile == null)
            {
                Debug.LogError("[PlayerCombat] 怨듦꺽 ?꾨줈??NULL - ?몄뒪?숉꽣?먯꽌 FormAttackProfile ?좊떦 ?뺤씤 ?꾩슂");
                return;
            }
        }
        
        // 1. 利됱떆 怨듦꺽 媛?ν븯硫??ㅽ뻾
        if (CanAttack())
        {
            StartAttack(direction);
        }
        // 2. 怨듦꺽 以묒씠嫄곕굹 荑⑦???以묒씠硫?踰꾪띁留?
        else if (IsAttacking || cooldownTimer > 0f)
        {
            // ?낅젰 踰꾪띁?????
            hasBufferedAttack = true;
            bufferedDirection = direction;
            inputBufferTimer = config != null ? config.inputBufferTime : 0.15f;
            // Debug.Log($"[PlayerCombat] Attack buffered! Direction: {direction}");
        }
    }

    private void ProcessBufferedAttack()
    {
        // 踰꾪띁??怨듦꺽???덇퀬, 吏湲??뱀옣 ?섑뻾 媛?ν븯?ㅻ㈃ ?ㅽ뻾
        if (hasBufferedAttack && CanAttack())
        {
            hasBufferedAttack = false;
            StartAttack(bufferedDirection);
        }
    }
    
    /// <summary>
    /// 怨듦꺽 媛???щ?
    /// </summary>
    public bool CanAttack()
    {
        // 怨듦꺽 以묒씠 ?꾨땲怨?荑⑤떎???앸궓
        if (IsAttacking) return false;
        if (cooldownTimer > 0) return false;
        
        // ?뱀젙 ?곹깭?먯꽌??怨듦꺽 遺덇? (WallSlide, LedgeClimb ??
        var currentState = player.StateMachine.CurrentState;
        if (currentState == player.WallSlideState ||
            currentState == player.WallClimbState ||
            currentState == player.LedgeClimbState ||
            currentState == player.HitState ||
            currentState == player.DeathState ||
            currentState == player.TransformState)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 怨듦꺽 媛뺤젣 以묐떒 (?쇨꺽, 而룹떊 ??
    /// Animator.speed ?덉쟾 蹂듦뎄 ?ы븿
    /// </summary>
    public void InterruptAttack()
    {
        // ?쇰컲 怨듦꺽 以묐떒
        if (IsAttacking)
        {
            IsAttacking = false;
            LockMovement = false;
            hasBufferedAttack = false;
            currentSession = null;
            
            // ?대깽??諛쒖깮
            CombatEvents.RaiseAttackInterrupt();
        }
        
        // ?뱀닔 ?됰룞 以묐떒
        CancelSpecialAction();
        
        // Pogo ?덉쟾 以묐떒
        ForceStopPogo();
        
        // [TODO] 異뷀썑 怨듦꺽 ?좊땲硫붿씠???먯뀑 ?쒖옉 ??二쇱꽍 ?댁젣
        // 以묒슂: Animator ?띾룄 蹂듦뎄
        // if (player.Animator != null)
        // {
        //     player.Animator.speed = 1f;
        // }
    }
    
    /// <summary>
    /// ?뱀닔 ?됰룞 ?쒖옉 (Slam ??
    /// </summary>
    public ActionHandle StartSpecialAction(ISpecialAction action)
    {
        // 湲곗〈 ?됰룞 痍⑥냼
        CancelSpecialAction();
        
        // ?쇰컲 怨듦꺽 以묐떒
        InterruptAttack();
        
        currentSpecialAction = action;
        currentActionHandle = new ActionHandle();
        
        // 醫낅즺 肄쒕갚 ?곌껐
        currentActionHandle.SetOnEndedCallback(OnSpecialActionEnded);
        
        action.Begin(currentActionHandle);
        return currentActionHandle;
    }
    
    /// <summary>
    /// ?뱀닔 ?됰룞 媛뺤젣 痍⑥냼
    /// </summary>
    public void CancelSpecialAction()
    {
        if (currentSpecialAction == null) return;
        
        currentActionHandle?.Dispose();
        currentSpecialAction.Cancel();
        
        currentSpecialAction = null;
        currentActionHandle = null;
    }
    
    /// <summary>
    /// Action 醫낅즺 肄쒕갚 (Action ?댁뿉???몄텧 ?꾩슂 ??
    /// </summary>
    internal void OnSpecialActionEnded()
    {
        currentActionHandle?.Dispose();
        currentSpecialAction = null;
        currentActionHandle = null;
    }
    
    #endregion
    
    #region Attack Execution
    
    private void StartAttack(AttackDirection dir)
    {
        // 1. 諛⑺뼢 ?ㅻ깄??(???쒓컙 怨좎젙, ?댄썑 ?낅젰 臾댁떆)
        snapshotFacing = player.IsFacingRight ? 1 : -1;
        snapshotDirection = dir;
        
        // 2. ?꾩옱 ?쇱쓽 怨듦꺽 ?곗씠??濡쒕뱶
        currentAttack = CurrentProfile.GetAttack(dir);
        
        if (currentAttack == null)
        {
            currentAttack = CurrentProfile.normalAttack;
            Debug.LogWarning($"[PlayerCombat] {dir} 怨듦꺽 ?곗씠?곌? ?놁뼱 湲곕낯 怨듦꺽?쇰줈 ?泥댄빀?덈떎.");
        }

        if (currentAttack == null)
        {
            Debug.LogError("[PlayerCombat] 鍮뚮뱶蹂??ㅻ쪟: 紐⑤뱺 怨듦꺽 ?곗씠?곌? ?꾨씫?섏뿀?듬땲??");
            return;
        }
        
        // 3. 怨듦꺽 ?몄뀡 ?앹꽦
        currentSession = new AttackSession
        {
            attack = currentAttack,
            origin = transform.position,
            facing = snapshotFacing,
            targetLayer = enemyLayer,
            attacker = gameObject,
            damageMultiplier = CurrentProfile.damageMultiplier,
            rangeMultiplier = CurrentProfile.rangeMultiplier,
            recoilTargetLayer = recoilTriggerLayers,
            recoilTargetSurfaces = recoilTriggerSurfaces
        };
        
        // 4. ?곹깭 珥덇린??
        IsAttacking = true;
        hasHit = false;
        attackTimer = 0f;
        
        // 5. ?대룞 ?쒖빟 (吏??怨듦꺽 ??
        if (currentAttack.lockMovementOnGround && player.IsGrounded())
        {
            LockMovement = true;
        }
        
        // 6. 而ㅼ뒪? ?좊땲硫붿씠???ъ깮
        Mado.Character.Animation.PlayerAnimType animType = GetAnimType(dir);
        player.PlayAnimation(animType, force: true);
        
        // 7. ?대깽??諛쒖깮 (濡쒖쭅? ?뺤긽 ?묐룞)
        CombatEvents.RaiseAttackStart(currentAttack);
    }
    
    private void ProcessAttack()
    {
        if (!IsAttacking || currentAttack == null) return;
        
        // 怨듦꺽???대뼡 ?댁쑀濡쒕뱺 ?앸굹吏 ?딅뒗 ?꾩긽 諛⑹? (?덉쟾 ??대㉧ 2珥?
        if (attackTimer > 2.0f)
        {
            Debug.LogWarning("[PlayerCombat] 怨듦꺽???덈Т ?ㅻ옒 吏?띾릺??媛뺤젣 醫낅즺?⑸땲??");
            EndAttack();
            return;
        }

        // ?띾룄 諛곗쑉 ?곸슜????대㉧
        float speedMult = (CurrentProfile != null) ? CurrentProfile.attackSpeedMultiplier : 1f;
        attackTimer += Time.deltaTime * speedMult;
        
        // ?대룞 ?좉툑 ?댁젣 泥댄겕
        if (LockMovement && attackTimer >= currentAttack.lockDuration)
        {
            LockMovement = false;
        }
        
        // ?덊듃 ?꾨젅??泥댄겕
        float hitTime = currentAttack.baseAnimDuration * currentAttack.hitActiveNormalized;
        if (!hasHit && attackTimer >= hitTime)
        {
            ExecuteHit();
            hasHit = true;
        }
        
        // 怨듦꺽 醫낅즺 泥댄겕
        if (attackTimer >= currentAttack.baseAnimDuration)
        {
            EndAttack();
        }
    }
    
    private void ExecuteHit()
    {
        if (currentSession == null) return;
        
        // 罹먯떛??李몄“媛 null?대㈃ 留ㅻ쾲 ?ㅼ떆 李얘린 (??濡쒕뱶 ?쒖꽌 臾몄젣 ?닿껐)
        if (hitResolver == null)
            hitResolver = HitResolver.Instance ?? FindFirstObjectByType<HitResolver>();
        
        if (hitResolver == null) return;
        
        // ?꾩옱 ?꾩튂 媛깆떊
        currentSession.origin = transform.position;
        
        // ?덊듃 ?먯젙
        HitResult result = hitResolver.ProcessAttack(currentSession);
        
        // [諛섎룞] ?ㅼ젙???덉씠?대굹 ?쒕㈃???곸쨷?덈뒗吏 ?뺤씤?섏뿬 諛섎룞 ?곸슜
        if (result.TriggerRecoil)
        {
            ApplyRecoil();
        }
        
        // ?곸쨷(?곕?吏 ?먯젙) ???쇰뱶諛?泥섎━
        if (result.HasHit)
        {
            // ?寃⑷컧: ??꼍吏?HitStop)
            if (currentAttack.hitStopDuration > 0f)
            {
                player.StartHitStop(currentAttack.hitStopDuration);
            }
        }
    }
    
    private void ApplyRecoil()
    {
        if (currentAttack == null) return;
        
        // ?몄뒪?숉꽣???ㅼ젙??諛곗쑉(Multiplier)???곸슜
        Vector2 recoil = currentAttack.recoilForce * recoilForceMultiplier;
        
        switch (currentAttack.recoilType)
        {
            case RecoilType.ReplaceY:
                // ?ш퀬 ?먰봽 (Normal ???섍컯 怨듦꺽) - ?덇굅??
                if (recoil.y > 0)
                {
                    player.RB.linearVelocity = new Vector2(
                        player.RB.linearVelocity.x,
                        recoil.y
                    );
                }
                break;
                
            case RecoilType.AddImpulse:
                // 異⑷꺽 異붽? (X異뺤? 諛붾씪蹂대뒗 諛⑺뼢 諛섎?)
                float recoilX = Mathf.Abs(recoil.x) * -snapshotFacing;
                Vector2 finalRecoil = new Vector2(recoilX, recoil.y);
                
                // PlayerController??RecoilRoutine ?꾩엫 (Snappy Recoil)
                // 吏???쒓컙? LockDuration???ъ슜
                player.StartRecoil(finalRecoil, currentAttack.lockDuration);
                break;
                
            case RecoilType.Slam:
                // Devil ??Slam (異⑷꺽???앹꽦)
                SpawnSlamEffect();
                break;
                
            case RecoilType.PogoJump:
                // Hollow Knight ?ㅽ???Pogo (臾쇰━ 湲곕컲)
                // 1. 臾쇰━ ?띾룄 ?곸슜
                float bounceVel = 14f; // Default fallback
                if (currentAttack is PogoAttackData pogoData)
                {
                    bounceVel = pogoData.pogoBounceVelocity;
                }
                
                // Pogo 諛붿슫?ㅼ뿉??諛곗쑉 ?곸슜
                player.PogoBounce(bounceVel * recoilForceMultiplier);
                
                // 2. ?덊듃 ?ㅽ넲 (Game Feel) - Pogo???쎄컙 湲멸쾶
                CombatFeedback.Instance?.RequestHitStop(0.08f);
                
                // 3. ?ㅽ겕由??먯씠??(?듭뀡)
                CombatFeedback.Instance?.RequestScreenShake(currentAttack.screenShakeMagnitude);
                break;
        }
    }
    
    private void SpawnSlamEffect()
    {
        // TODO: Devil 폼 전용 충격파/착지 이펙트 생성
    }
    
    // PogoRoutine 제거됨 (Legacy)
    
    /// <summary>
    /// Pogo 媛뺤젣 以묐떒 諛?以묐젰 蹂듦뎄 (?덉쟾?μ튂)
    /// </summary>
    private void ForceStopPogo()
    {
        // Pogo媛€ ?댁젣 臾쇰━ 湲곕컲?대?濡??밸퀎??肄붾（??以묐떒?€ ?꾩슂 ?놁쓬
        // ?ㅻ쭔 ?숉븯 ?띾룄 ?쒗븳 ?댁젣 ???덉쟾?μ튂???좎?
        player.ClearFallSpeedClamp();
    }
    
    private void EndAttack()
    {
        IsAttacking = false;
        LockMovement = false;
        currentSession = null;
        
        // [TODO] 異뷀썑 怨듦꺽 ?좊땲硫붿씠???먯뀑 ?쒖옉 ??二쇱꽍 ?댁쑙
        // Animator ?띾룄 蹂듦뎄
        // if (player.Animator != null)
        // {
        //     player.Animator.speed = 1f;
        // }
        
        // 踰꾪띁 ?뺤씤 (荑⑦??꾨낫???곗꽑!)
        if (hasBufferedAttack && inputBufferTimer > 0)
        {
            Debug.Log("[PlayerCombat] Executing buffered attack!");
            hasBufferedAttack = false;
            cooldownTimer = 0f;  // ?좑툘 踰꾪띁 怨듦꺽?€ 荑⑦???臾댁떆!
            StartAttack(bufferedDirection);
        }
        else
        {
            // 荑⑤떎???쒖옉 (踰꾪띁 ?놁쑣 ?뚮쭔)
            // 狩?荑⑦????곸슜 (CombatConfig 湲곗?)
            cooldownTimer = config != null ? config.baseAttackCooldown : 0.25f;
        }
    }
    
    #endregion
    
    #region Helpers
    
    private Mado.Character.Animation.PlayerAnimType GetAnimType(AttackDirection dir)
    {
        return dir switch
        {
            AttackDirection.Up => Mado.Character.Animation.PlayerAnimType.AttackUp,
            AttackDirection.Down => Mado.Character.Animation.PlayerAnimType.AttackDown,
            _ => Mado.Character.Animation.PlayerAnimType.AttackNormal
        };
    }
    
    private void UpdateTimers()
    {
        if (cooldownTimer > 0)
            cooldownTimer -= Time.deltaTime;
        
        if (inputBufferTimer > 0)
            inputBufferTimer -= Time.deltaTime;
    }
    
    #endregion
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (currentAttack == null || !IsAttacking) return;
        
        // ?꾩옱 怨듦꺽 ?덊듃諛뺤뒪 ?쒖떆
        Vector2 center = (Vector2)transform.position + new Vector2(
            currentAttack.hitboxOffset.x * snapshotFacing,
            currentAttack.hitboxOffset.y
        );
        
        Gizmos.color = hasHit ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(center, currentAttack.hitboxSize);
    }
#endif
}
