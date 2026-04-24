using UnityEngine;

public enum GhostReason { None, FarAway, Stuck }

public class PetController : MonoBehaviour
{
    #region State Machine Variables
    public PetStateMachine StateMachine { get; private set; }
    public PetFollowState FollowState { get; private set; }
    public PetGhostState GhostState { get; private set; }
    public PetRushState RushState { get; private set; }
    #endregion
    
    #region References
    public PlayerController Player { get; private set; }
    public Rigidbody2D RB { get; private set; }
    public Collider2D Collider { get; private set; }
    public SpriteRenderer SpriteRenderer { get; private set; }
    #endregion
    
    #region Data
    [SerializeField] private PetData petData;
    public PetData PetData => petData;
    #endregion
    
    #region Logic Variables
    public GhostReason CurrentGhostReason { get; set; } = GhostReason.None;
    public float LastGhostExitTime { get; set; } = -999f;
    #endregion

    #region Visual
    public bool IsFacingRight { get; private set; } = true;
    private bool isFloating = true;
    #endregion
    
    #region Teleport
    private float lastTeleportTime = -999f;
    #endregion
    
    #region Follow Control
    private bool isFollowEnabled = true; // Follow AI 활성화 여부
    #endregion
    

    
#if UNITY_EDITOR
    #region Debug Info (Inspector)
    [Header("Debug Info")]
    [SerializeField] private string currentStateName;
    [SerializeField] private float distanceToPlayer;
    [SerializeField] private bool isTeleporting;
    #endregion
#endif
    
    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        Collider = GetComponent<Collider2D>();
        SpriteRenderer = GetComponent<SpriteRenderer>();
        Player = FindFirstObjectByType<PlayerController>();
        
        if (Player == null)
        {
            Debug.LogError("PetController: PlayerController를 찾을 수 없습니다!");
        }
        
        StateMachine = new PetStateMachine();
        FollowState = new PetFollowState(this, StateMachine);
        GhostState = new PetGhostState(this, StateMachine);
        RushState = new PetRushState(this, StateMachine);
    }
    
    
    private void OnEnable()
    {
        SpriteRenderer.enabled = true; // 씬 재로딩/리스폰 시 펫은 무조건 보여야 함 (기본값 복구)
    }
    
    private void Start()
    {
        // Follow 상태로 시작 (단일 상태 시스템)
        StateMachine.Initialize(FollowState);
        
        // 이벤트 구독
        GameEvents.OnPlayerDeath += CancelRushAttack;
        GameEvents.OnSavePointActivated += OnSavePointRest;
    }

    private void OnDestroy()
    {
        GameEvents.OnPlayerDeath -= CancelRushAttack;
        GameEvents.OnSavePointActivated -= OnSavePointRest;
    }
    
    private void Update()
    {
        // Follow AI가 비활성화되면 Update 중단
        if (!isFollowEnabled) return;
        
        // 공통 텔레포트 체크 (제미나이 개선: 중복 제거)
        if (Player != null)
        {
            float distance = Vector2.Distance(transform.position, Player.transform.position);
            if (distance > PetData.followToGhostDistance * 3f) // 텔레포트는 아주 멀 때 (예: 30m)
            {
                Teleport();
            }
        }
        
        StateMachine.CurrentState?.LogicUpdate();
    }
    
#if UNITY_EDITOR
    private void LateUpdate()
    {
        // 디버그 정보는 LateUpdate에서 업데이트 (플레이어 업데이트 후)
        UpdateDebugInfo();
    }
#endif
    
    private void FixedUpdate()
    {
        // 물리 업데이트 (이동 로직)
        StateMachine.CurrentState?.PhysicsUpdate();
    }
    
#if UNITY_EDITOR
    private void UpdateDebugInfo()
    {
        // 현재 상태 이름 표시
        if (StateMachine?.CurrentState != null)
        {
            currentStateName = StateMachine.CurrentState.GetType().Name;
        }
        else
        {
            currentStateName = "None";
        }
        
        // 플레이어와의 거리 표시
        if (Player != null && this != null)
        {
            distanceToPlayer = Vector2.Distance(transform.position, Player.transform.position);
        }
    }
#endif
    
#if UNITY_EDITOR
    public void SetTeleporting(bool value)
    {
        isTeleporting = value;
    }
#endif
    
    #region Attack Controls
    public int RushCharges { get; set; } = 0;
    
    public void TriggerRushAttack()
    {
        // 이미 러쉬 모드(장전) 중이면 S키 재장전 무시
        if (RushCharges > 0) return;
        
        RushCharges = PetData.rushMaxCharge;
        
        if (StateMachine.CurrentState != RushState)
        {
            StateMachine.ChangeState(RushState);
        }
    }

    public void CancelRushAttack()
    {
        RushCharges = 0;
        if (StateMachine.CurrentState == RushState)
        {
            StateMachine.ChangeState(FollowState);
        }
    }

    private void OnSavePointRest(Transform savePoint)
    {
        CancelRushAttack();
    }
    
    public PetState GetDefaultPetState()
    {
        return RushCharges > 0 ? RushState : FollowState;
    }

    public Collider2D FindNearestEnemyInSight()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, PetData.rushDetectRadius, PetData.targetLayer);
        Collider2D nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (var hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null || damageable.IsInvincible) continue;
            
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            Vector2 dir = (hit.transform.position - transform.position).normalized;
            
            // 시야 검사
            RaycastHit2D rayHit = Physics2D.Raycast(transform.position, dir, dist, PetData.wallLayer);
            if (rayHit.collider == null)
            {
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = hit;
                }
            }
        }
        return nearest;
    }
    #endregion

    /// <summary>
    /// 텔레포트 메서드
    /// </summary>
    public void Teleport()
    {
        // 쿨다운 체크 (간단 구현)
        if (Time.time - lastTeleportTime < 1.0f)
            return;
        
        // 플레이어 위치 + 오프셋으로 이동
        Vector2 targetPos = (Vector2)Player.transform.position + PetData.anchorOffset;
        transform.position = targetPos;
        
        // 속도 초기화
        RB.linearVelocity = Vector2.zero;
        
        // 기본 상태로 강제 전환 (Rush 상태면 복구)
        StateMachine.ChangeState(GetDefaultPetState());
        
        // 쿨다운 갱신
        lastTeleportTime = Time.time;
        
#if UNITY_EDITOR
        SetTeleporting(true);
#endif
    }
    
    /// <summary>
    /// 투명도 설정
    /// </summary>
    public void SetAlpha(float alpha)
    {
        Color color = SpriteRenderer.color;
        color.a = alpha;
        SpriteRenderer.color = color;
    }
    
    /// <summary>
    /// 둥실거림 활성화/비활성화
    /// </summary>
    public void SetFloating(bool value)
    {
        isFloating = value;
    }
    
    public bool IsFloating => isFloating;
    
    /// <summary>
    /// 플레이어와의 시야 확보 여부 체크 (2/3 Rule, Y축 보정 없음, 디버그 강화)
    /// </summary>
    public bool HasLineOfSightToPlayer(out float hitDistance)
    {
        hitDistance = Mathf.Infinity;
        if (Player == null) return false;
        
        Vector2 petCenter = Collider.bounds.center;
        
        // 플레이어 중심
        Collider2D playerCol = Player.GetComponent<Collider2D>();
        Vector2 playerCenter = playerCol != null ? (Vector2)playerCol.bounds.center : (Vector2)Player.transform.position;
        
        Vector2 direction = playerCenter - petCenter;
        
        // [수정] 수직 성분 제거 로직(ignoreY) 삭제.
        // 항상 플레이어를 향한 직선 Ray를 사용합니다. 
        // 2/3 Rule 덕분에 계단(Bottom Ray Blocked)은 자동으로 걸러집니다.

        float distToPlayer = direction.magnitude; 
        Vector2 dirNormalized = direction.normalized;
        
        // 3-Ray 세팅
        const float RAYCAST_HEIGHT_RATIO = 0.8f;
        float halfHeight = Collider.bounds.extents.y * RAYCAST_HEIGHT_RATIO;
        
        Vector2[] rayOrigins = new Vector2[]
        {
            petCenter + Vector2.up * halfHeight,    // 상단
            petCenter,                               // 중심
            petCenter + Vector2.down * halfHeight   // 하단
        };
        
        int blockCount = 0;
        float minHitDist = Mathf.Infinity;
        
        // [개선] 거리 제한 적용 (너무 먼 벽은 무시)
        float checkDist = Mathf.Min(distToPlayer, PetData.maxBlockDistance);
        
        foreach (Vector2 origin in rayOrigins)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                origin,
                dirNormalized,
                checkDist,
                PetData.wallLayer
            );
            
            if (hit.collider != null)
            {
                blockCount++;
                if (hit.distance < minHitDist) minHitDist = hit.distance;
                
                // 개별 Ray 디버그 (빨강: 막힘)
                Debug.DrawRay(origin, dirNormalized * hit.distance, Color.red);
            }
            else
            {
                // 개별 Ray 디버그 (초록: 뚫림)
                Debug.DrawRay(origin, dirNormalized * checkDist, Color.green);
            }
        }
        
        // [개선 2] 3개 중 2개 이상 막혀야 진짜 막힌 것 (계단/모서리 관용)
        if (blockCount >= 2)
        {
            hitDistance = minHitDist;
            return false; // Blocked
        }
        else
        {
            hitDistance = distToPlayer;
            return true; // Pass
        }
    }
    
    // 오버로딩: 하위 호환성 유지 (필요없을 수도 있지만 안전을 위해)
    public bool HasLineOfSightToPlayer()
    {
        return HasLineOfSightToPlayer(out float _);
    }
    
    public void Flip(bool faceRight)
    {
        if (IsFacingRight == faceRight) return;
        
        IsFacingRight = faceRight;
        Vector3 scale = transform.localScale;
        scale.x = faceRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
    

    #region Follow Control (Respawn)
    
    /// <summary>
    /// Follow AI 정지 (부활 시 사용)
    /// </summary>
    public void StopFollow()
    {
        isFollowEnabled = false;
        
        // 물리 관성 제거
        if (RB != null)
        {
            RB.linearVelocity = Vector2.zero;
        }
    }
    
    /// <summary>
    /// Follow AI 재개 (부활 완료 시 사용)
    /// </summary>
    public void ResumeFollow()
    {
        isFollowEnabled = true;
    }
    
    #endregion

    /// <summary>
    /// 목표 앵커 위치 계산 (Follow/Ghost 공용)
    /// </summary>
    public Vector2 CalculateDesiredAnchor()
    {
        if (Player == null) return transform.position;

        // 주인공의 등 뒤쪽 상공
        float dir = Player.IsFacingRight ? 1f : -1f;
        return (Vector2)Player.transform.position 
             + new Vector2(-dir * Mathf.Abs(PetData.anchorOffset.x), 
                           PetData.anchorOffset.y);
    }
}
