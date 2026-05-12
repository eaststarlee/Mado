using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공격 행동 관리자 (상태가 아닌 행동 레이어)
/// 
/// 책임:
/// - 공격 요청 수신 및 조율
/// - 세션 생성 및 수명 관리
/// - 제약(Constraint) 플래그 관리
/// - 반동(Recoil) 요청 전달
/// 
/// 책임 분리:
/// - 히트 판정: HitResolver
/// - 피드백: CombatFeedback
/// - 타이밍은 AttackData.baseAnimDuration 기준
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private CombatConfig config;
    [SerializeField] private LayerMask enemyLayer;
    
    [Header("반동(Recoil) 설정")]
    [Tooltip("공격 시 플레이어를 뒤로 밀어내게 하는 타겟 레이어 (예: Enemy, Ground 등)")]
    public LayerMask recoilTriggerLayers;
    
    [Tooltip("공격 시 플레이어를 뒤로 밀어내게 하는 표면(Surface) 타입들")]
    public List<SurfaceType> recoilTriggerSurfaces;
    
    [Tooltip("반동 힘의 배율 (1.0 = AttackData의 기본값)")]
    [Range(0.1f, 5.0f)]
    public float recoilForceMultiplier = 1.0f;
    
    private PlayerController player;
    private HitResolver hitResolver;
    
    #region Properties
    
    /// <summary>
    /// 현재 폼의 공격 프로필
    /// </summary>
    private FormAttackProfile CurrentProfile => player.ActiveFormData?.attackProfile;
    
    /// <summary>
    /// 공격 중 여부
    /// </summary>
    public bool IsAttacking { get; private set; }
    
    /// <summary>
    /// 이동 잠금 플래그 (Constraint)
    /// MoveState 및 ActionSystem에서 참조
    /// </summary>
    public bool LockMovement { get; set; }
    
    /// <summary>
    /// Enemy Layer (SlamAction 등에서 참조)
    /// </summary>
    public LayerMask EnemyLayer => enemyLayer;
    
    #endregion
    
    #region Private State
    
    // 현재 공격 세션
    private AttackSession currentSession;
    private AttackData currentAttack;
    private float attackTimer;
    private float cooldownTimer;
    private bool hasHit;
    
    // 방향 스냅샷 (공격 시작 시간 고정)
    private int snapshotFacing;
    private AttackDirection snapshotDirection;
    
    
    // 입력 버퍼
    private float inputBufferTimer;
    private bool hasBufferedAttack;
    private AttackDirection bufferedDirection;
    
    // Pogo 관리
    private Coroutine pogoCoroutine;
    private bool isPogoActive;
    
    // Special Action Runner (Slam 등)
    private ISpecialAction currentSpecialAction;
    private ActionHandle currentActionHandle;
    
    /// <summary>
    /// Pogo 활성 상태 (외부 참조용)
    /// </summary>
    public bool IsPogoActive => isPogoActive;
    
    /// <summary>
    /// 특수 행동 활성 여부
    /// </summary>
    public bool IsSpecialActionActive => currentSpecialAction != null;

    /// <summary>
    /// 현재 진행 중인 특수 행동 객체
    /// </summary>
    public ISpecialAction CurrentSpecialAction => currentSpecialAction;

    /// <summary>
    /// 특수 행동 중 입력 잠금 여부
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
        
        // HitResolver가 씬에 없으면 경고
        if (hitResolver == null)
        {
            Debug.LogWarning("[PlayerCombat] HitResolver를 찾을 수 없습니다. 공격 판정이 작동하지 않습니다.");
        }

        // 가시(Spike) 표면은 자동 지지대로 등록
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
    
    #endregion

    #region Attack Requests

    /// <summary>
    /// 공격 요청 (PlayerController.GatherInput에서 호출)
    /// </summary>
    /// <param name="direction">공격 방향</param>
    public void RequestAttack(AttackDirection direction)
    {
        // 데이터가 없으면 즉시 초기화 시도
        if (CurrentProfile == null)
        {
            var formManager = player.GetComponent<PlayerFormManager>();
            if (formManager != null) formManager.InitializeFormData();
            
            if (CurrentProfile == null)
            {
                Debug.LogError("[PlayerCombat] 공격 프로필 NULL - 인스펙터에서 FormAttackProfile 할당 확인 필요");
                return;
            }
        }
        
        // 1. 즉시 공격 가능하면 실행
        if (CanAttack())
        {
            StartAttack(direction);
        }
        // 2. 공격 중이거나 쿨다운 중이면 버퍼링
        else if (IsAttacking || cooldownTimer > 0f)
        {
            hasBufferedAttack = true;
            bufferedDirection = direction;
            inputBufferTimer = config != null ? config.inputBufferTime : 0.15f;
        }
    }

    private void ProcessBufferedAttack()
    {
        if (hasBufferedAttack && CanAttack())
        {
            hasBufferedAttack = false;
            StartAttack(bufferedDirection);
        }
    }
    
    /// <summary>
    /// 공격 가능 여부
    /// </summary>
    public bool CanAttack()
    {
        if (IsAttacking) return false;
        if (cooldownTimer > 0) return false;
        
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
    /// 공격 강제 중단 (피격, 컷신 등)
    /// </summary>
    public void InterruptAttack()
    {
        if (IsAttacking)
        {
            IsAttacking = false;
            LockMovement = false;
            hasBufferedAttack = false;
            currentSession = null;
            
            CombatEvents.RaiseAttackInterrupt();
        }
        
        CancelSpecialAction();
        ForceStopPogo();
    }
    
    /// <summary>
    /// 특수 행동 시작 (Slam 등)
    /// </summary>
    public ActionHandle StartSpecialAction(ISpecialAction action)
    {
        CancelSpecialAction();
        InterruptAttack();
        
        currentSpecialAction = action;
        currentActionHandle = new ActionHandle();
        currentActionHandle.SetOnEndedCallback(OnSpecialActionEnded);
        
        action.Begin(currentActionHandle);
        return currentActionHandle;
    }
    
    /// <summary>
    /// 특수 행동 강제 취소
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
    /// Action 종료 콜백
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
        // 1. 방향 스냅샷
        snapshotFacing = player.IsFacingRight ? 1 : -1;
        snapshotDirection = dir;
        
        // 2. 공격 데이터 로드
        currentAttack = CurrentProfile.GetAttack(dir);
        
        if (currentAttack == null)
        {
            currentAttack = CurrentProfile.normalAttack;
            Debug.LogWarning($"[PlayerCombat] {dir} 공격 데이터가 없어 기본 공격으로 대체합니다.");
        }

        if (currentAttack == null)
        {
            Debug.LogError("[PlayerCombat] 빌드 오류: 모든 공격 데이터가 누락되었습니다.");
            return;
        }
        
        // 3. 공격 세션 생성
        currentSession = new AttackSession
        {
            attack = currentAttack,
            origin = transform.position,
            facing = snapshotFacing,
            targetLayer = enemyLayer,
            attacker = gameObject,
            damageMultiplier = CurrentProfile.damageMultiplier,
            rangeMultiplier = CurrentProfile.rangeMultiplier,
            // [BugFix] DownAttack(Pogo) 시 모든 지형에 반응하는 문제 해결
            // 1) 레이어 필터링: 하향 공격 시에는 일반 지형 레이어를 제외하고 적(Enemy) 레이어만 체크
            recoilTargetLayer = (dir == AttackDirection.Down) ? enemyLayer : recoilTriggerLayers,
            // 2) 표면 필터링: 하향 공격 시에는 Spike(가시) 표면 정보가 있는 지형에서만 반동 허용
            recoilTargetSurfaces = (dir == AttackDirection.Down) 
                ? new List<SurfaceType> { SurfaceType.Spike } 
                : recoilTriggerSurfaces
        };
        
        // 4. 상태 초기화
        IsAttacking = true;
        hasHit = false;
        attackTimer = 0f;
        
        // 5. 이동 제약 (지상 공격 시)
        if (currentAttack.lockMovementOnGround && player.IsGrounded())
        {
            LockMovement = true;
        }
        
        // 6. 애니메이션 재생
        Mado.Character.Animation.PlayerAnimType animType = GetAnimType(dir);
        player.PlayAnimation(animType, force: true);
        
        // 7. 이벤트 발생
        CombatEvents.RaiseAttackStart(currentAttack);
    }
    
    private void ProcessAttack()
    {
        if (!IsAttacking || currentAttack == null) return;
        
        if (attackTimer > 2.0f)
        {
            Debug.LogWarning("[PlayerCombat] 공격이 너무 오래 지속되어 강제 종료합니다.");
            EndAttack();
            return;
        }

        float speedMult = (CurrentProfile != null) ? CurrentProfile.attackSpeedMultiplier : 1f;
        attackTimer += Time.deltaTime * speedMult;
        
        if (LockMovement && attackTimer >= currentAttack.lockDuration)
        {
            LockMovement = false;
        }
        
        float hitTime = currentAttack.baseAnimDuration * currentAttack.hitActiveNormalized;
        if (!hasHit && attackTimer >= hitTime)
        {
            ExecuteHit();
            hasHit = true;
        }
        
        if (attackTimer >= currentAttack.baseAnimDuration)
        {
            EndAttack();
        }
    }
    
    private void ExecuteHit()
    {
        if (currentSession == null) return;
        
        if (hitResolver == null)
            hitResolver = HitResolver.Instance ?? FindFirstObjectByType<HitResolver>();
        
        if (hitResolver == null) return;
        
        currentSession.origin = transform.position;
        
        HitResult result = hitResolver.ProcessAttack(currentSession);
        
        if (result.TriggerRecoil)
        {
            ApplyRecoil();
        }
        
        if (result.HasHit)
        {
            if (currentAttack.hitStopDuration > 0f)
            {
                player.StartHitStop(currentAttack.hitStopDuration);
            }
        }
    }
    
    private void ApplyRecoil()
    {
        if (currentAttack == null) return;
        
        Vector2 recoil = currentAttack.recoilForce * recoilForceMultiplier;
        
        switch (currentAttack.recoilType)
        {
            case RecoilType.ReplaceY:
                if (recoil.y > 0)
                {
                    player.RB.linearVelocity = new Vector2(
                        player.RB.linearVelocity.x,
                        recoil.y
                    );
                }
                break;
                
            case RecoilType.AddImpulse:
                float recoilX = Mathf.Abs(recoil.x) * -snapshotFacing;
                Vector2 finalRecoil = new Vector2(recoilX, recoil.y);
                player.StartRecoil(finalRecoil, currentAttack.lockDuration);
                break;
                
            case RecoilType.Slam:
                SpawnSlamEffect();
                break;
                
            case RecoilType.PogoJump:
                float bounceVel = 14f;
                if (currentAttack is PogoAttackData pogoData)
                {
                    bounceVel = pogoData.pogoBounceVelocity;
                }
                
                player.PogoBounce(bounceVel * recoilForceMultiplier);
                CombatFeedback.Instance?.RequestHitStop(0.08f);
                CombatFeedback.Instance?.RequestScreenShake(currentAttack.screenShakeMagnitude);
                break;
        }
    }
    
    private void SpawnSlamEffect()
    {
        // TODO: Devil 폼 전용 충격파/착지 이펙트 생성
    }
    
    private void ForceStopPogo()
    {
        player.ClearFallSpeedClamp();
    }
    
    private void EndAttack()
    {
        IsAttacking = false;
        LockMovement = false;
        currentSession = null;
        
        if (hasBufferedAttack && inputBufferTimer > 0)
        {
            hasBufferedAttack = false;
            cooldownTimer = 0f;
            StartAttack(bufferedDirection);
        }
        else
        {
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
        
        Vector2 center = (Vector2)transform.position + new Vector2(
            currentAttack.hitboxOffset.x * snapshotFacing,
            currentAttack.hitboxOffset.y
        );
        
        Gizmos.color = hasHit ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(center, currentAttack.hitboxSize);
    }
#endif
}
