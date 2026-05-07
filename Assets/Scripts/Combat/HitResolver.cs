using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ?�트 ?�정???�일 책임??(3?�계 ?�이?�라??
/// 1. Detection: OverlapBox�??�???�집
/// 2. Validation: 무적/?�링/?�퍼?�머 체크
/// 3. Application: ?��?지 ?�용 �?반응 ?�출
/// 
/// Stateless ?�계: AlreadyHit?� AttackSession???�유
/// </summary>
public class HitResolver : MonoBehaviour
{
    public static HitResolver Instance { get; private set; }
    
    [Header("피드백")]
    [SerializeField] private CombatFeedback feedback;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
        
        // ?�드�??�동 찾기
        if (feedback == null)
        {
            feedback = FindFirstObjectByType<CombatFeedback>();
        }
    }
    
    /// <summary>
    /// 공격 ?�정 처리 (?�일 진입??
    /// </summary>
    /// <param name="session">공격 ?�션 (?�명 관�??�위)</param>
    /// <returns>?�중 결과</returns>
    public HitResult ProcessAttack(AttackSession session)
    {
        // Stage 1: Detection
        var candidates = DetectTargets(session);
        
        // Stage 2: Validation
        var validTargets = ValidateTargets(candidates, session);
        
        // Stage 3: Application
        var result = ApplyDamage(validTargets, session);
        
        // [Environment] ?�경 ?�브?�트 ?�캔 �?가�?(BreakableWall ??
        CheckEnvironmentHits(session);
        
        // [Recoil] 반동(?�백) ?��? 검??
        result.TriggerRecoil = CheckRecoilTriggers(session);
        
        // ?�드�??�리�?
        if (result.hitCount > 0 && feedback != null)
        {
            feedback.TriggerHitFeedback(session.attack);
        }
        
        return result;
    }

    /// <summary>
    /// ?�정??Layer ?�는 SurfaceInfo�?기반?�로 ?�백 발생 ?��?�??�정?�니??
    /// </summary>
    private bool CheckRecoilTriggers(AttackSession session)
    {
        Vector2 boxCenter = session.origin + new Vector2(
            session.attack.hitboxOffset.x * session.facing * session.rangeMultiplier,
            session.attack.hitboxOffset.y
        );
        Vector2 boxSize = session.attack.hitboxSize * session.rangeMultiplier;

        // 1. 지?�된 ?�이??recoilTargetLayer) 충돌 검??
        if (session.recoilTargetLayer != 0)
        {
            Collider2D col = Physics2D.OverlapBox(boxCenter, boxSize, 0f, session.recoilTargetLayer);
            if (col != null) return true;
        }

        // 2. 지?�된 SurfaceInfo 검??
        if (session.recoilTargetSurfaces != null && session.recoilTargetSurfaces.Count > 0)
        {
            LayerMask worldMask = DimensionManager.Instance != null
                ? DimensionManager.Instance.CurrentWorldMask
                : ~0; // 모든 ?�이??
                
            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, worldMask);
            foreach (var col in hits)
            {
                SurfaceInfo surface = col.GetComponentInParent<SurfaceInfo>();
                if (surface != null && session.recoilTargetSurfaces.Contains(surface.type))
                {
                    return true;
                }
            }
        }

        return false;
    }
    
    #region Stage 1: Detection
    
    /// <summary>
    /// OverlapBox�?충돌 ?�???�집
    /// </summary>
    private List<Collider2D> DetectTargets(AttackSession session)
    {
        // 방향 ?�용???�트박스 중심 계산
        Vector2 boxCenter = session.origin + new Vector2(
            session.attack.hitboxOffset.x * session.facing * session.rangeMultiplier,
            session.attack.hitboxOffset.y
        );
        
        // 범위 배율 ?�용
        Vector2 boxSize = session.attack.hitboxSize * session.rangeMultiplier;
        
        // OverlapBox�?충돌 검??
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, session.targetLayer);
        
#if UNITY_EDITOR
        // ?�버�? ?�트박스 ?�각??
        DebugDrawHitbox(boxCenter, boxSize, hits.Length > 0 ? Color.red : Color.yellow);
#endif
        
        // Game �??�트박스 ?�각??(?��???
        HitboxDebugRenderer.Instance?.RegisterPlayerHitbox(boxCenter, boxSize, 0.1f);
        
        return hits.ToList();
    }
    
    /// <summary>
    /// ?�경 ?�브?�트(BreakableWall ?? ?�캔 �??��?처리
    /// </summary>
    private void CheckEnvironmentHits(AttackSession session)
    {
        // 공격 주체가 ?�레?�어?��? ?�인
        if (session.attacker == null) return;
        PlayerController player = session.attacker.GetComponent<PlayerController>();
        if (player == null) return;
        
        // ??체크 로직 ??��: ??검증�? 개별 ?�경 ?�브?�트(DestructibleEntity ??가 ?�스�??�단?�도�??�임
        
        // ?�트박스 계산 (DetectTargets?� ?�일)
        Vector2 boxCenter = session.origin + new Vector2(
            session.attack.hitboxOffset.x * session.facing * session.rangeMultiplier,
            session.attack.hitboxOffset.y
        );
        Vector2 boxSize = session.attack.hitboxSize * session.rangeMultiplier;
        
        // ?�재 ?�계 ?�이?�마?�크 (DimensionManager가 ?�으�??�체 ?�이??
        LayerMask worldMask = DimensionManager.Instance != null
            ? DimensionManager.Instance.CurrentWorldMask
            : ~0;
            
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, worldMask);
        
        // ?�경 ?��??�보�??�집
        List<Collider2D> environmentTargets = new List<Collider2D>();
        
        foreach (var col in hits)
        {
            // 부모까지 ?�함??SurfaceInfo ?�색
            SurfaceInfo surface = col.GetComponentInParent<SurfaceInfo>();
            if (surface == null) continue;

            // ?�괴 가?�한 �?감�? ???�보�?추�? (추후 ?�른 ?�괴 가???�브?�트 추�? ???�장 가??
            if (surface.type == SurfaceType.BreakableWall || surface.type == SurfaceType.Devil_BreakableWall)
            {
                environmentTargets.Add(col);
            }
        }

        // ?�선?�위 ?�렬 �??�일 ?��?처리
        if (environmentTargets.Count > 0)
        {
            // ?�레?�어(공격 ?�점)로�???가??가까운 ?�서�??�렬
            environmentTargets.Sort((a, b) => 
                Vector2.Distance(session.origin, a.transform.position)
                .CompareTo(Vector2.Distance(session.origin, b.transform.position)));

            // [?�심] 가??가까운 1개의 ?�브?�트�??�격하????번의 ?�윙???�러 벽이 부?��????�상 방�?
            var closestHit = environmentTargets[0];
            
            IDamageable damageable = closestHit.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsInvincible)
            {
                // ?��? 맞�? ?�?�인지 체크
                if (!session.alreadyHit.Contains(closestHit.gameObject))
                {
                    int finalDamage = Mathf.RoundToInt(session.attack.baseDamage * session.damageMultiplier);

                    DamageInfo info = new DamageInfo
                    {
                        damage              = finalDamage,
                        hitPoint            = boxCenter,
                        hitDirection        = new Vector2(session.facing, 0f),
                        damageSource        = session.attacker.transform.position,
                        knockbackForce      = Vector2.zero,
                        stunDuration        = session.attack.stunDuration,
                        damageType          = DamageType.Physical,
                        hitType             = HitType.Heavy,
                        ignoreInvincibility = false,
                        ignoreArmor         = false,
                        canBeParried        = false,
                        source              = session.attacker
                    };

                    damageable.TakeDamage(info);
                    
                    // 중복 ?�트 방�?
                    session.alreadyHit.Add(closestHit.gameObject);
                }
            }
        }
    }
    
    #endregion
    
    #region Stage 2: Validation
    
    /// <summary>
    /// ?�효???�???�터�?(무적, 중복 ??
    /// </summary>
    private List<ValidatedTarget> ValidateTargets(List<Collider2D> candidates, AttackSession session)
    {
        var validTargets = new List<ValidatedTarget>();
        
        foreach (var col in candidates)
        {
            // ?��? ?�번 공격??맞�? ?�???�킵
            if (session.alreadyHit.Contains(col.gameObject))
            {
                continue;
            }
            
            // IDamageable ?�인
            var damageable = col.GetComponent<IDamageable>();
            if (damageable == null)
            {
                continue;
            }
            
            // 무적 체크
            if (damageable.IsInvincible)
            {
                continue;
            }
            
            // IEnemyReaction ?�인 (?�택??
            var reaction = col.GetComponent<IEnemyReaction>();
            
            // ?�효 ?�??추�?
            validTargets.Add(new ValidatedTarget
            {
                gameObject = col.gameObject,
                damageable = damageable,
                reaction = reaction
            });
            
            // 중복 ?�트 방�?
            session.alreadyHit.Add(col.gameObject);
        }
        
        return validTargets;
    }
    
    #endregion
    
    #region Stage 3: Application
    
    /// <summary>
    /// ?��?지 ?�용 �?반응 ?�출
    /// </summary>
    private HitResult ApplyDamage(List<ValidatedTarget> targets, AttackSession session)
    {
        var result = new HitResult();
        result.hitTargets = new List<GameObject>();
        
        foreach (var target in targets)
        {
            // 최종 ?��?지 계산 (??보정 ?�용)
            int finalDamage = Mathf.RoundToInt(session.attack.baseDamage * session.damageMultiplier);
            
            // ?�백 방향 계산
            Vector2 knockbackDir;
            
            if (session.attack.knockbackMode == KnockbackMode.RadialFromOrigin)
            {
                // 방사??(Origin -> Target)
                knockbackDir = (target.gameObject.transform.position - (Vector3)session.origin).normalized;
                
                // Z�??�이�??�거 �??�전?�치
                if (knockbackDir == Vector2.zero)
                {
                    // ?�치가 겹치�??�덤 ?�는 Facing 방향?�로 밀?�냄
                    knockbackDir = new Vector2(session.facing, 0);
                }
            }
            else
            {
                // 고정 방향 (Facing 기�?)
                knockbackDir = new Vector2(session.facing, 0);
            }
            
            // DamageInfo ?�성
            DamageInfo info = new DamageInfo
            {
                damage = finalDamage,
                knockbackForce = session.attack.baseKnockback, // Pass Vector2 directly
                damageSource = session.origin,
                hitDirection = knockbackDir,
                stunDuration = session.attack.stunDuration, // Pass stun duration
                damageType = DamageType.Physical,
                hitType = HitType.Light, // Defaulting for now
                source = session.attacker,
                ignoreArmor = false,
                ignoreInvincibility = false,
                canBeParried = true
            };
            
            // ?��?지 ?�용
            target.damageable.TakeDamage(info);
            
            // Reaction ?�출 (attacker ?�함)
            target.reaction?.OnHitReaction(info, session.attacker);
            
            // 결과 기록
            result.hitCount++;
            result.hitTargets.Add(target.gameObject);
        }
        
        // [New] ?�레?�어 공격 명중 ???�킬 게이지(?�울) ?�득
        if (result.hitCount > 0 && session.attacker != null)
        {
            var skillResource = session.attacker.GetComponent<PlayerSkillResource>();
            var playerCtrl = session.attacker.GetComponent<PlayerController>();
            
            if (skillResource != null && playerCtrl != null)
            {
                skillResource.AddGauge(playerCtrl.ActiveFormData.skillResource.gainOnAttack);
            }
        }
        
        return result;
    }
    
    #endregion
    
#if UNITY_EDITOR
    private void DebugDrawHitbox(Vector2 center, Vector2 size, Color color)
    {
        Vector2 halfSize = size * 0.5f;
        
        Debug.DrawLine(center + new Vector2(-halfSize.x, -halfSize.y), 
                       center + new Vector2(halfSize.x, -halfSize.y), color, 0.1f);
        Debug.DrawLine(center + new Vector2(halfSize.x, -halfSize.y), 
                       center + new Vector2(halfSize.x, halfSize.y), color, 0.1f);
        Debug.DrawLine(center + new Vector2(halfSize.x, halfSize.y), 
                       center + new Vector2(-halfSize.x, halfSize.y), color, 0.1f);
        Debug.DrawLine(center + new Vector2(-halfSize.x, halfSize.y), 
                       center + new Vector2(-halfSize.x, -halfSize.y), color, 0.1f);
    }
#endif
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

/// <summary>
/// 공격 ?�션: ?�일 공격???�명 관�??�위
/// HitResolver??Stateless, ?�태??Session???�유
/// </summary>
public class AttackSession
{
    public AttackData attack;
    public Vector2 origin;
    public int facing;  // 1: ?�른�? -1: ?�쪽
    public LayerMask targetLayer;
    public GameObject attacker;
    
    // ??보정�?
    public float damageMultiplier = 1f;
    public float rangeMultiplier = 1f;
    
    // 반동(Recoil) 감�???
    public LayerMask recoilTargetLayer;
    public List<SurfaceType> recoilTargetSurfaces;
    
    // 중복 ?�트 방�? (?�션???�유)
    public HashSet<GameObject> alreadyHit = new HashSet<GameObject>();
    
    // ?�명 관�?
    public float lifetime;
}

/// <summary>
/// 검증된 ?�???�보
/// </summary>
public struct ValidatedTarget
{
    public GameObject gameObject;
    public IDamageable damageable;
    public IEnemyReaction reaction;
}

/// <summary>
/// ?�중 결과
/// </summary>
public struct HitResult
{
    public int hitCount;
    public List<GameObject> hitTargets;
    public bool TriggerRecoil;
    
    public bool HasHit => hitCount > 0;
}


