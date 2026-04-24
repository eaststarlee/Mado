using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 최상위 컨테이너. 유일한 MonoBehaviour.
/// 모든 시스템 참조를 보유하고, Unity 생명주기를 라우팅한다.
/// 
/// Update() 순서:
/// ① Sensors 갱신 → ② ConditionalBehaviorEvaluator → ③ Brain.Tick() → ④ HitboxManager
/// FixedUpdate(): Motor가 큐에 쌓인 이동 명령을 물리 적용
/// </summary>
public class EnemyEntity : MonoBehaviour
{
    // --- 데이터 ---
    [Header("Definition")]
    [SerializeField] private EnemyDefinition definition;
    
    // --- 컴포넌트 참조 ---
    public Rigidbody2D Rigidbody { get; private set; }
    public Animator Animator { get; private set; }
    public SpriteRenderer SpriteRenderer { get; private set; }
    public Collider2D Collider { get; private set; }
    public EnemyHealth Health { get; private set; }
    
    // --- 시스템 ---
    public EnemyBrain Brain { get; private set; }
    public EnemyBlackboard Blackboard { get; private set; }
    public EnemyMotor Motor { get; private set; }
    public HitboxManager HitboxManager { get; private set; }
    public AnimationEventRouter AnimEventRouter { get; private set; }
    public ConditionalBehaviorEvaluator ConditionalEvaluator { get; private set; }
    public EffectSpawner EffectSpawner { get; private set; }
    public EnemyDefinition Definition => definition;
    
    // --- 센서 ---
    // --- 센서 ---
    private List<SensorBase> sensors = new List<SensorBase>();
    private DamageSensor damageSensor; // [New] 특수 센서

    
    // --- 모듈 ---
    private BehaviorModule walkModule;
    private BehaviorModule meleeSwingModule;
    private BehaviorModule dashModule;
    private Modules.Reaction.HitReactionModule hitReactionModule;
    private Modules.Reaction.StunReactionModule stunReactionModule;
    private Modules.Reaction.DeathModule deathModule;
    
    // --- 디버그 ---
    [Header("Debug")]
    [SerializeField] private string currentState = "None";
    
    // ========================================================
    // Unity 생명주기
    // ========================================================
    
    private void Awake()
    {
        // 컴포넌트 수집
        Rigidbody = GetComponent<Rigidbody2D>();
        Animator = GetComponentInChildren<Animator>();
        SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        Collider = GetComponentInChildren<Collider2D>();
        Health = GetComponentInChildren<EnemyHealth>();
        
        // AnimationEventRouter (MonoBehaviour, Animator와 같은 오브젝트에 필요)
        AnimEventRouter = GetComponentInChildren<AnimationEventRouter>();
        
        // 센서 수집
        // 센서 수집
        sensors.AddRange(GetComponentsInChildren<SensorBase>());
        damageSensor = GetComponentInChildren<DamageSensor>(); // [New]
        if (damageSensor == null) Debug.LogError($"[EnemyEntity] {name} does NOT have a DamageSensor component!");

        
        // 시스템 초기화
        Blackboard = new EnemyBlackboard();
        Motor = new EnemyMotor(Rigidbody, SpriteRenderer, transform);
        Brain = new EnemyBrain();
        HitboxManager = new HitboxManager(this);
        EffectSpawner = new EffectSpawner(this);
        
        // 모듈 초기화
        InitializeModules();
    }
    
    private void OnEnable()
    {
        // EnemyHealth 이벤트 → Brain InterruptLayer에 큐잉
        if (Health != null)
        {
            Health.OnHit += HandleHit;
            Health.OnPoiseBreak += HandlePoiseBreak;
            Health.OnDeath += HandleDeath;
        }
    }
    
    private void OnDisable()
    {
        if (Health != null)
        {
            Health.OnHit -= HandleHit;
            Health.OnPoiseBreak -= HandlePoiseBreak;
            Health.OnDeath -= HandleDeath;
        }
    }
    
    private void Update()
    {
        // 사망 상태면 Brain 업데이트 중지
        if (Blackboard.HasFlag(StatusFlag.IsDead)) return;
        
        // ① Sensors 갱신 → Blackboard에 결과 직접 기록
        // ① Sensors 갱신
        for (int i = 0; i < sensors.Count; i++)
        {
            sensors[i].Tick(Blackboard); // UpdateSensor -> Tick
        }
        
        // Decay (기억 소실) 처리
        // 5초(Memory Duration) 등은 EnemyDefinition이나 Blackboard 초기값으로 설정 필요
        // 일단 하드코딩 3.0f 또는 Definition 추가 필요. 여기선 3.0f 사용.
        Blackboard.Target.UpdateDecay(3.0f);

        
        // ② ConditionalBehaviorEvaluator (매 N프레임 폴링)
        ConditionalEvaluator?.Tick();
        
        // ③ Brain.Tick()
        Brain.Tick(Time.deltaTime, this, Blackboard);
        
        // ④ HitboxManager (활성 히트박스 판정)
        HitboxManager.Tick();
        
        // 디버그
        UpdateDebugInfo();
    }
    
    // ========================================================
    // 초기화
    // ========================================================
    
    private void InitializeModules()
    {
        if (definition == null)
        {
            Debug.LogError($"[EnemyEntity] {gameObject.name}: EnemyDefinition이 할당되지 않았습니다!");
            return;
        }
        
        // 초기 방향 설정
        Blackboard.Movement.facingDirection = 1;
        
        // 페이즈 임계값 설정
        if (definition.BrainSettings.phaseHealthThresholds != null)
        {
            Blackboard.Phase.phaseHealthThresholds = definition.BrainSettings.phaseHealthThresholds;
        }
        
        // Reaction 모듈 생성 (구조적으로 필수)
        hitReactionModule = new Modules.Reaction.HitReactionModule();
        stunReactionModule = new Modules.Reaction.StunReactionModule();
        deathModule = new Modules.Reaction.DeathModule();
        
        // Selector 생성
        var movementSelector = new DistanceBasedSelector();
        IDecisionMaker actionSelector = null;
        
        // Action 모듈이 있으면 RandomWeightedSelector 사용
        if (definition.BrainSettings.meleeSwingModuleData != null)
        {
            actionSelector = new RandomWeightedSelector();
        }
        
        // Brain 초기화
        Brain.Initialize(
            movementSelector: movementSelector,
            actionSelector: actionSelector,
            hitReaction: hitReactionModule,
            stunReaction: stunReactionModule,
            death: deathModule,
            abortThreshold: definition.BrainSettings.abortThreshold
        );
        
        // Movement 모듈 생성 및 등록
        if (definition.BrainSettings.walkModuleData != null)
        {
            walkModule = new Modules.Movement.WalkModule(definition.BrainSettings.walkModuleData);
            Brain.AddModule(walkModule);
        }
        else if (definition.BrainSettings.flyModuleData != null) // [New] FlyModule
        {
            var flyModule = new Modules.Movement.FlyModule(definition.BrainSettings.flyModuleData);
            Brain.AddModule(flyModule);
        }
        else
        {
             // Debug.LogError("[EnemyEntity] WalkModuleData is NULL in BrainSettings!");
        }
        
        // Combat 모듈 생성 및 등록
        if (definition.BrainSettings.meleeSwingModuleData != null)
        {
            meleeSwingModule = new Modules.Combat.MeleeSwingModule(definition.BrainSettings.meleeSwingModuleData);
            Brain.AddModule(meleeSwingModule);
        }
        
        // Dash 모듈 생성 및 등록
        if (definition.BrainSettings.dashModuleData != null)
        {
            dashModule = new Modules.Movement.DashModule(definition.BrainSettings.dashModuleData);
            Brain.AddModule(dashModule);
        }
        
        // ConditionalBehaviorEvaluator 생성
        if (Health != null)
        {
            ConditionalEvaluator = new ConditionalBehaviorEvaluator(this, Health, Blackboard, Brain);
        }
    }
    
    // ========================================================
    // 이벤트 핸들러 → Brain InterruptLayer에 큐잉
    // ========================================================
    
    private void HandleHit(DamageInfo info)
    {
        // 사망 상태면 무시
        if (Blackboard.HasFlag(StatusFlag.IsDead)) return;
        
        // 슈퍼아머 체크
        if (definition.CombatSettings.hasSuperArmor && !info.ignoreArmor)
        {
            return;
        }
        
        // 스턴 중이면 피격 무시
        if (Blackboard.HasFlag(StatusFlag.IsStunned))
        {
            return;
        }
        
        Debug.Log("[EnemyEntity] Enqueue Hit Interrupt");
        
        // [New] DamageSensor에게 알림 Aggro 유지
        if (damageSensor != null)
        {
            Debug.Log($"[EnemyEntity] HandleHit -> ReportDamage. Source: {info.source?.name}");
            damageSensor.ReportDamage(info, Blackboard);
        }
        else
        {
            Debug.LogError("[EnemyEntity] DamageSensor is NULL! Please add DamageSensor component to the enemy.");
        }

        Brain.EnqueueInterrupt(InterruptType.Hit, info);

    }
    
    private void HandlePoiseBreak()
    {
        if (Blackboard.HasFlag(StatusFlag.IsDead)) return;
        if (Blackboard.HasFlag(StatusFlag.IsStunned)) return;
        
        if (definition.CombatSettings.hasStunState)
        {
            Brain.EnqueueInterrupt(InterruptType.Stun);
        }
    }
    
    private void HandleDeath(DamageInfo info)
    {
        Brain.EnqueueInterrupt(InterruptType.Death, info);
    }
    
    // ========================================================
    // 디버그
    // ========================================================
    
    private void UpdateDebugInfo()
    {
        if (Brain.IsInterrupted)
        {
            currentState = $"[INTERRUPT] {Brain.CurrentInterrupt?.ModuleName}";
        }
        else
        {
            string mov = Brain.CurrentMovement?.ModuleName ?? "None";
            string movStatus = Brain.CurrentMovement?.GetStatus();
            if (!string.IsNullOrEmpty(movStatus)) mov += $" ({movStatus})";

            string act = Brain.CurrentAction?.ModuleName ?? "None";
            string actStatus = Brain.CurrentAction?.GetStatus();
            if (!string.IsNullOrEmpty(actStatus)) act += $" ({actStatus})";

            currentState = $"Mov:{mov} | Act:{act}";
        }
    }
    
    // ========================================================
    // 디버그 기즈모 (에디터 전용)
    // ========================================================
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (definition == null || definition.BrainSettings.meleeSwingModuleData == null) return;
        
        var data = definition.BrainSettings.meleeSwingModuleData;
        
        // 1. 타겟 추적/공격 사거리 (노란색 원)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);
        
        // 2. 예상 히트박스 영역 (빨간색 박스)
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        
        // 방향 결정 로직 (플레이 모드면 블랙보드, 아니면 트랜스폼/스프라이트)
        int facing = 1;
        bool isAttacking = false;
        
        if (Application.isPlaying && Blackboard != null)
        {
            facing = Blackboard.Movement.facingDirection;
            
            // MeleeSwing 모듈이 활성 상태인지 확인
            if (Brain != null && Brain.CurrentAction != null && Brain.CurrentAction.ModuleName == "MeleeSwing")
            {
                // Active 상태(PreDelay, Anticipation, Active 등)면 true
                isAttacking = Brain.CurrentAction.State == ModuleState.Active; 
            }
        }
        else if (SpriteRenderer != null)
        {
            facing = SpriteRenderer.flipX ? -1 : 1;
        }
        
        // 기즈모를 그릴 것인가?
        // 1. 설정이 꺼져있으면 항상 그림
        // 2. 설정이 켜져있으면 공격 중(isAttacking)이거나 에디터 모드(!Application.isPlaying)일 때만 그림
        bool shouldDrawHitbox = !data.showGizmoOnlyWhenAttacking || isAttacking || !Application.isPlaying;
        
        if (shouldDrawHitbox)
        {
            Vector2 center = (Vector2)transform.position + new Vector2(data.hitboxOffset.x * facing, data.hitboxOffset.y);
            Gizmos.DrawCube(center, data.hitboxSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, data.hitboxSize);
        }
    }
#endif
}
