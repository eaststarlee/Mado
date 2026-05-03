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
    [Tooltip("공격 시 플레이어를 뒤로 밀려나게 할 대상 레이어 (예: Enemy, Ground 등)")]
    public LayerMask recoilTriggerLayers;
    
    [Tooltip("공격 시 플레이어를 뒤로 밀려나게 할 표면(Surface) 타입들")]
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
    
    // 방향 스냅샷 (공격 시작 순간 고정)
    private int snapshotFacing;
    private AttackDirection snapshotDirection;
    
    
    // 입력 버퍼
    private float inputBufferTimer;
    private bool hasBufferedAttack;
    private AttackDirection bufferedDirection;
    
    // Pogo 관리
    private Coroutine pogoCoroutine;
    private bool isPogoActive;
    
    // [New] Special Action Runner (Slam 등)
    private ISpecialAction currentSpecialAction;
    private ActionHandle currentActionHandle;
    
    /// <summary>
    /// Pogo 활성 상태 (외부 참조용)
    /// </summary>
    public bool IsPogoActive => isPogoActive;
    
    /// <summary>
    /// [New] 특수 행동 활성 여부
    /// </summary>
    public bool IsSpecialActionActive => currentSpecialAction != null;

    /// <summary>
    /// [New] 특수 행동 중 입력 잠금 여부
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

        // [New] 가시(Spike) 포고 자동 지원 등록
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
        
        // [New] Special Action Update
        currentSpecialAction?.Update(Time.deltaTime);
    }
    
    private void FixedUpdate()
    {
        // Pogo는 이제 코루틴 기반이므로 물리 업데이트 불필요
    }
    
    // ...

    /// <summary>
    /// 공격 요청 (PlayerController.GatherInput에서 호출)
    /// </summary>
    /// <param name="direction">공격 방향</param>
    public void RequestAttack(AttackDirection direction)
    {
        // [Build Fix] 데이터가 없으면 즉시 초기화 시도
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
        // 2. 공격 중이거나 쿨타임 중이면 버퍼링
        else if (IsAttacking || cooldownTimer > 0f)
        {
            // 입력 버퍼에 저장
            hasBufferedAttack = true;
            bufferedDirection = direction;
            inputBufferTimer = config != null ? config.inputBufferTime : 0.15f;
            // Debug.Log($"[PlayerCombat] Attack buffered! Direction: {direction}");
        }
    }

    private void ProcessBufferedAttack()
    {
        // 버퍼된 공격이 있고, 지금 당장 수행 가능하다면 실행
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
        // 공격 중이 아니고 쿨다운 끝남
        if (IsAttacking) return false;
        if (cooldownTimer > 0) return false;
        
        // 특정 상태에서는 공격 불가 (WallSlide, LedgeClimb 등)
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
    /// Animator.speed 안전 복구 포함
    /// </summary>
    public void InterruptAttack()
    {
        // 일반 공격 중단
        if (IsAttacking)
        {
            IsAttacking = false;
            LockMovement = false;
            hasBufferedAttack = false;
            currentSession = null;
            
            // 이벤트 발생
            CombatEvents.RaiseAttackInterrupt();
        }
        
        // [New] 특수 행동 중단
        CancelSpecialAction();
        
        // Pogo 안전 중단
        ForceStopPogo();
        
        // [TODO] 추후 공격 애니메이션 에셋 제작 후 주석 해제
        // 중요: Animator 속도 복구
        // if (player.Animator != null)
        // {
        //     player.Animator.speed = 1f;
        // }
    }
    
    /// <summary>
    /// [New] 특수 행동 시작 (Slam 등)
    /// </summary>
    public ActionHandle StartSpecialAction(ISpecialAction action)
    {
        // 기존 행동 취소
        CancelSpecialAction();
        
        // 일반 공격 중단
        InterruptAttack();
        
        currentSpecialAction = action;
        currentActionHandle = new ActionHandle();
        
        // 종료 콜백 연결
        currentActionHandle.SetOnEndedCallback(OnSpecialActionEnded);
        
        action.Begin(currentActionHandle);
        return currentActionHandle;
    }
    
    /// <summary>
    /// [New] 특수 행동 강제 취소
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
    /// [New] Action 종료 콜백 (Action 내에서 호출 필요 시)
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
        // 1. 방향 스냅샷 (이 순간 고정, 이후 입력 무시)
        snapshotFacing = player.IsFacingRight ? 1 : -1;
        snapshotDirection = dir;
        
        // 2. 현재 폼의 공격 데이터 로드
        currentAttack = CurrentProfile.GetAttack(dir);
        
        // [Build Fix] 만약 특정 방향 공격 데이터가 없으면 일반 공격 데이터로 대체 시도
        if (currentAttack == null)
        {
            currentAttack = CurrentProfile.normalAttack;
            Debug.LogWarning($"[PlayerCombat] {dir} 공격 데이터가 없어 기본 공격으로 대체합니다.");
        }

        if (currentAttack == null)
        {
            Debug.LogError("[PlayerCombat] 빌드본 오류: 모든 공격 데이터가 누락되었습니다.");
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
            recoilTargetLayer = recoilTriggerLayers,
            recoilTargetSurfaces = recoilTriggerSurfaces
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
        
        // 6. [New] 커스텀 애니메이션 재생
        Mado.Character.Animation.PlayerAnimType animType = GetAnimType(dir);
        player.PlayAnimation(animType, force: true);
        
        // 7. 이벤트 발생 (로직은 정상 작동)
        CombatEvents.RaiseAttackStart(currentAttack);
    }
    
    private void ProcessAttack()
    {
        if (!IsAttacking || currentAttack == null) return;
        
        // [Build Fix] 공격이 어떤 이유로든 끝나지 않는 현상 방지 (안전 타이머 2초)
        if (attackTimer > 2.0f)
        {
            Debug.LogWarning("[PlayerCombat] 공격이 너무 오래 지속되어 강제 종료합니다.");
            EndAttack();
            return;
        }

        // 속도 배율 적용된 타이머
        float speedMult = (CurrentProfile != null) ? CurrentProfile.attackSpeedMultiplier : 1f;
        attackTimer += Time.deltaTime * speedMult;
        
        // 이동 잠금 해제 체크
        if (LockMovement && attackTimer >= currentAttack.lockDuration)
        {
            LockMovement = false;
        }
        
        // 히트 프레임 체크
        float hitTime = currentAttack.baseAnimDuration * currentAttack.hitActiveNormalized;
        if (!hasHit && attackTimer >= hitTime)
        {
            ExecuteHit();
            hasHit = true;
        }
        
        // 공격 종료 체크
        if (attackTimer >= currentAttack.baseAnimDuration)
        {
            EndAttack();
        }
    }
    
    private void ExecuteHit()
    {
        if (currentSession == null) return;
        
        // [Fix] 캐싱된 참조가 null이면 매번 다시 찾기 (씬 로드 순서 문제 해결)
        if (hitResolver == null)
            hitResolver = HitResolver.Instance ?? FindFirstObjectByType<HitResolver>();
        
        if (hitResolver == null) return;
        
        // 현재 위치 갱신
        currentSession.origin = transform.position;
        
        // 히트 판정
        HitResult result = hitResolver.ProcessAttack(currentSession);
        
        // [반동] 설정된 레이어나 표면에 적중했는지 확인하여 반동 적용
        if (result.TriggerRecoil)
        {
            ApplyRecoil();
        }
        
        // 적중(데미지 판정) 시 피드백 처리
        if (result.HasHit)
        {
            // 타격감: 역경직(HitStop)
            if (currentAttack.hitStopDuration > 0f)
            {
                player.StartHitStop(currentAttack.hitStopDuration);
            }
        }
    }
    
    private void ApplyRecoil()
    {
        if (currentAttack == null) return;
        
        // 인스펙터에 설정된 배율(Multiplier)을 적용
        Vector2 recoil = currentAttack.recoilForce * recoilForceMultiplier;
        
        switch (currentAttack.recoilType)
        {
            case RecoilType.ReplaceY:
                // 포고 점프 (Normal 폼 하강 공격) - 레거시
                if (recoil.y > 0)
                {
                    player.RB.linearVelocity = new Vector2(
                        player.RB.linearVelocity.x,
                        recoil.y
                    );
                }
                break;
                
            case RecoilType.AddImpulse:
                // 충격 추가 (X축은 바라보는 방향 반대)
                float recoilX = Mathf.Abs(recoil.x) * -snapshotFacing;
                Vector2 finalRecoil = new Vector2(recoilX, recoil.y);
                
                // PlayerController의 RecoilRoutine 위임 (Snappy Recoil)
                // 지속 시간은 LockDuration을 사용
                player.StartRecoil(finalRecoil, currentAttack.lockDuration);
                break;
                
            case RecoilType.Slam:
                // Devil 폼 Slam (충격파 생성)
                SpawnSlamEffect();
                break;
                
            case RecoilType.PogoJump:
                // Hollow Knight 스타일 Pogo (물리 기반)
                // 1. 물리 속도 적용
                float bounceVel = 14f; // Default fallback
                if (currentAttack is PogoAttackData pogoData)
                {
                    bounceVel = pogoData.pogoBounceVelocity;
                }
                
                // Pogo 바운스에도 배율 적용
                player.PogoBounce(bounceVel * recoilForceMultiplier);
                
                // 2. 히트 스톱 (Game Feel) - Pogo는 약간 길게
                CombatFeedback.Instance?.RequestHitStop(0.08f);
                
                // 3. 스크린 쉐이크 (옵션)
                CombatFeedback.Instance?.RequestScreenShake(currentAttack.screenShakeMagnitude);
                break;
        }
    }
    
    private void SpawnSlamEffect()
    {
        // TODO: Devil 폼 전용 충격파/착지 이펙트 생성
        // 추후 구현
    }
    
    // PogoRoutine 제거됨 (Legacy)
    
    /// <summary>
    /// Pogo 강제 중단 및 중력 복구 (안전장치)
    /// </summary>
    private void ForceStopPogo()
    {
        // Pogo가 이제 물리 기반이므로 특별한 코루틴 중단은 필요 없음
        // 다만 낙하 속도 제한 해제 등 안전장치는 유지
        player.ClearFallSpeedClamp();
    }
    
    private void EndAttack()
    {
        IsAttacking = false;
        LockMovement = false;
        currentSession = null;
        
        // [TODO] 추후 공격 애니메이션 에셋 제작 후 주석 해제
        // Animator 속도 복구
        // if (player.Animator != null)
        // {
        //     player.Animator.speed = 1f;
        // }
        
        // 버퍼 확인 (쿨타임보다 우선!)
        if (hasBufferedAttack && inputBufferTimer > 0)
        {
            Debug.Log("[PlayerCombat] Executing buffered attack!");
            hasBufferedAttack = false;
            cooldownTimer = 0f;  // ⚠️ 버퍼 공격은 쿨타임 무시!
            StartAttack(bufferedDirection);
        }
        else
        {
            // 쿨다운 시작 (버퍼 없을 때만)
            // ⭐ 쿨타임 적용 (CombatConfig 기준)
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
        
        // 현재 공격 히트박스 표시
        Vector2 center = (Vector2)transform.position + new Vector2(
            currentAttack.hitboxOffset.x * snapshotFacing,
            currentAttack.hitboxOffset.y
        );
        
        Gizmos.color = hasHit ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(center, currentAttack.hitboxSize);
    }
#endif
}
