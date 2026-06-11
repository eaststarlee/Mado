using UnityEngine;

public enum GhostReason { None, FarAway, Stuck }

public class PetController : MonoBehaviour
{
    #region State Machine Variables
    public PetStateMachine StateMachine { get; private set; }
    public PetFollowState FollowState { get; private set; }
    public PetGhostState GhostState { get; private set; }
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
    
    #region Logic & Cache Variables
    public GhostReason CurrentGhostReason { get; set; } = GhostReason.None;
    public float LastGhostExitTime { get; set; } = -999f;
    
    // [Optimization] 프레임 공통 데이터 캐싱
    public float DistanceToPlayer { get; private set; }
    public Vector2 DesiredAnchor { get; private set; }
    public bool HasLOS { get; private set; }
    #endregion

    #region Visual
    public bool IsFacingRight { get; private set; } = true;
    private bool isFloating = true;
    #endregion
    
    #region Teleport
    private float lastTeleportTime = -999f;
    #endregion
    
    #region Follow Control
    private bool isFollowEnabled = true; 
    #endregion
    
#if UNITY_EDITOR
    #region Debug Info
    [Header("Debug Info")]
    [SerializeField] private string currentStateName;
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
    }
    
    private void Start()
    {
        StateMachine.Initialize(FollowState);
    }

    private void OnDestroy()
    {
    }

    private void Update()
    {
        if (!isFollowEnabled || Player == null) return;
        
        // 1. 매 프레임 공통 데이터 갱신 (1회만 계산하여 모든 상태가 공유)
        UpdateCommonData();
        
        // 2. 텔레포트 체크 (캐싱된 거리 사용)
        if (DistanceToPlayer > PetData.ghostTransitionRadius * 2f) 
        {
            Teleport();
        }
        
        // 3. 상태 머신 업데이트
        StateMachine.CurrentState?.LogicUpdate();
    }
    
    private void FixedUpdate()
    {
        if (!isFollowEnabled || Player == null) return;
        StateMachine.CurrentState?.PhysicsUpdate();
    }
    
    private void UpdateCommonData()
    {
        DistanceToPlayer = Vector2.Distance(transform.position, Player.transform.position);
        DesiredAnchor = CalculateDesiredAnchorInternal();
        HasLOS = HasLineOfSightToPlayerInternal(out _);
    }



    public void Teleport()
    {
        if (Time.time - lastTeleportTime < 1.0f) return;
        
        transform.position = (Vector2)Player.transform.position + PetData.anchorOffset;
        RB.linearVelocity = Vector2.zero;
        StateMachine.ChangeState(FollowState);
        lastTeleportTime = Time.time;
    }
    
    public void SetAlpha(float alpha)
    {
        Color color = SpriteRenderer.color;
        color.a = alpha;
        SpriteRenderer.color = color;
    }
    
    public void SetFloating(bool value) => isFloating = value;
    public bool IsFloating => isFloating;

    /// <summary>
    /// 공통 방향 전환 로직 (상태에서 호출)
    /// </summary>
    public void HandleFacing()
    {
        float distX = Player.transform.position.x - transform.position.x;
        if (Mathf.Abs(distX) > 0.5f)
        {
            Flip(distX > 0);
        }
    }
    
    public void Flip(bool faceRight)
    {
        if (IsFacingRight == faceRight) return;
        IsFacingRight = faceRight;
        Vector3 scale = transform.localScale;
        scale.x = faceRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    #region Internal Calculation Methods (Cashing Helpers)
    private Vector2 CalculateDesiredAnchorInternal()
    {
        if (Player == null) return transform.position;
        float dir = Player.IsFacingRight ? 1f : -1f;
        return (Vector2)Player.transform.position + new Vector2(-dir * Mathf.Abs(PetData.anchorOffset.x), PetData.anchorOffset.y);
    }

    private bool HasLineOfSightToPlayerInternal(out float hitDistance)
    {
        hitDistance = Mathf.Infinity;
        if (Player == null) return false;
        
        Vector2 petCenter = Collider.bounds.center;
        Collider2D playerCol = Player.GetComponent<Collider2D>();
        Vector2 playerCenter = playerCol != null ? (Vector2)playerCol.bounds.center : (Vector2)Player.transform.position;
        Vector2 direction = playerCenter - petCenter;
        float distToPlayer = direction.magnitude; 
        Vector2 dirNormalized = direction.normalized;
        
        const float RAYCAST_HEIGHT_RATIO = 0.8f;
        float halfHeight = Collider.bounds.extents.y * RAYCAST_HEIGHT_RATIO;
        Vector2[] rayOrigins = { petCenter + Vector2.up * halfHeight, petCenter, petCenter + Vector2.down * halfHeight };
        
        int blockCount = 0;
        float minHitDist = Mathf.Infinity;
        float checkDist = Mathf.Min(distToPlayer, PetData.maxBlockDistance);
        
        foreach (Vector2 origin in rayOrigins)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, dirNormalized, checkDist, PetData.wallLayer);
            if (hit.collider != null) { blockCount++; if (hit.distance < minHitDist) minHitDist = hit.distance; }
        }
        
        if (blockCount >= 2) { hitDistance = minHitDist; return false; }
        hitDistance = distToPlayer;
        return true;
    }
    #endregion

    #region Public Helper Methods (States Call These)
    public bool HasLineOfSightToPlayer(out float hitDistance)
    {
        // 최신 정보를 위해 out 파라미터가 필요한 경우 새로 계산하거나, 필요없다면 캐시 활용 가능
        // 여기서는 상태 머신의 Stuck 체크를 위해 실시간 검사를 유지하되, 내부 로직을 재활용함
        return HasLineOfSightToPlayerInternal(out hitDistance);
    }
    #endregion

    #region Follow Control (Respawn)
    public void StopFollow() { isFollowEnabled = false; if (RB != null) RB.linearVelocity = Vector2.zero; }
    public void ResumeFollow() => isFollowEnabled = true;
    #endregion

#if UNITY_EDITOR
    private void UpdateDebugInfo()
    {
        if (StateMachine?.CurrentState != null) currentStateName = StateMachine.CurrentState.GetType().Name;
    }
#endif
}
