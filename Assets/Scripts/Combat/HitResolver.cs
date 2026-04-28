using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 히트 판정의 단일 책임자 (3단계 파이프라인)
/// 1. Detection: OverlapBox로 대상 수집
/// 2. Validation: 무적/패링/슈퍼아머 체크
/// 3. Application: 데미지 적용 및 반응 호출
/// 
/// Stateless 설계: AlreadyHit은 AttackSession이 소유
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
        
        // 피드백 자동 찾기
        if (feedback == null)
        {
            feedback = FindFirstObjectByType<CombatFeedback>();
        }
    }
    
    /// <summary>
    /// 공격 판정 처리 (단일 진입점)
    /// </summary>
    /// <param name="session">공격 세션 (수명 관리 단위)</param>
    /// <returns>적중 결과</returns>
    public HitResult ProcessAttack(AttackSession session)
    {
        // Stage 1: Detection
        var candidates = DetectTargets(session);
        
        // Stage 2: Validation
        var validTargets = ValidateTargets(candidates, session);
        
        // Stage 3: Application
        var result = ApplyDamage(validTargets, session);
        
        // [Environment] 환경 오브젝트 스캔 및 가격 (BreakableWall 등)
        CheckEnvironmentHits(session);
        
        // [Recoil] 반동(넉백) 여부 검사
        result.TriggerRecoil = CheckRecoilTriggers(session);
        
        // 피드백 트리거
        if (result.hitCount > 0 && feedback != null)
        {
            feedback.TriggerHitFeedback(session.attack);
        }
        
        return result;
    }

    /// <summary>
    /// 설정된 Layer 또는 SurfaceInfo를 기반으로 넉백 발생 여부를 판정합니다.
    /// </summary>
    private bool CheckRecoilTriggers(AttackSession session)
    {
        Vector2 boxCenter = session.origin + new Vector2(
            session.attack.hitboxOffset.x * session.facing * session.rangeMultiplier,
            session.attack.hitboxOffset.y
        );
        Vector2 boxSize = session.attack.hitboxSize * session.rangeMultiplier;

        // 1. 지정된 레이어(recoilTargetLayer) 충돌 검사
        if (session.recoilTargetLayer != 0)
        {
            Collider2D col = Physics2D.OverlapBox(boxCenter, boxSize, 0f, session.recoilTargetLayer);
            if (col != null) return true;
        }

        // 2. 지정된 SurfaceInfo 검사
        if (session.recoilTargetSurfaces != null && session.recoilTargetSurfaces.Count > 0)
        {
            LayerMask worldMask = DimensionManager.Instance != null
                ? DimensionManager.Instance.CurrentWorldMask
                : ~0; // 모든 레이어
                
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
    /// OverlapBox로 충돌 대상 수집
    /// </summary>
    private List<Collider2D> DetectTargets(AttackSession session)
    {
        // 방향 적용된 히트박스 중심 계산
        Vector2 boxCenter = session.origin + new Vector2(
            session.attack.hitboxOffset.x * session.facing * session.rangeMultiplier,
            session.attack.hitboxOffset.y
        );
        
        // 범위 배율 적용
        Vector2 boxSize = session.attack.hitboxSize * session.rangeMultiplier;
        
        // OverlapBox로 충돌 검사
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, session.targetLayer);
        
#if UNITY_EDITOR
        // 디버그: 히트박스 시각화
        DebugDrawHitbox(boxCenter, boxSize, hits.Length > 0 ? Color.red : Color.yellow);
#endif
        
        // Game 뷰 히트박스 시각화 (런타임)
        HitboxDebugRenderer.Instance?.RegisterPlayerHitbox(boxCenter, boxSize, 0.1f);
        
        return hits.ToList();
    }
    
    /// <summary>
    /// 환경 오브젝트(BreakableWall 등) 스캔 및 타격 처리
    /// </summary>
    private void CheckEnvironmentHits(AttackSession session)
    {
        // 공격 주체가 플레이어인지 확인
        if (session.attacker == null) return;
        PlayerController player = session.attacker.GetComponent<PlayerController>();
        if (player == null) return;
        
        // 폼 체크 로직 삭제: 폼 검증은 개별 환경 오브젝트(DestructibleEntity 등)가 스스로 판단하도록 위임
        
        // 히트박스 계산 (DetectTargets와 동일)
        Vector2 boxCenter = session.origin + new Vector2(
            session.attack.hitboxOffset.x * session.facing * session.rangeMultiplier,
            session.attack.hitboxOffset.y
        );
        Vector2 boxSize = session.attack.hitboxSize * session.rangeMultiplier;
        
        // 현재 세계 레이어마스크 (DimensionManager가 없으면 전체 레이어)
        LayerMask worldMask = DimensionManager.Instance != null
            ? DimensionManager.Instance.CurrentWorldMask
            : ~0;
            
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, worldMask);
        
        // 환경 타격 후보군 수집
        List<Collider2D> environmentTargets = new List<Collider2D>();
        
        foreach (var col in hits)
        {
            // 부모까지 포함해 SurfaceInfo 탐색
            SurfaceInfo surface = col.GetComponentInParent<SurfaceInfo>();
            if (surface == null) continue;

            // 파괴 가능한 벽 감지 시 후보군 추가 (추후 다른 파괴 가능 오브젝트 추가 시 확장 가능)
            if (surface.type == SurfaceType.BreakableWall || surface.type == SurfaceType.Devil_BreakableWall)
            {
                environmentTargets.Add(col);
            }
        }

        // 우선순위 정렬 및 단일 타격 처리
        if (environmentTargets.Count > 0)
        {
            // 플레이어(공격 원점)로부터 가장 가까운 순서로 정렬
            environmentTargets.Sort((a, b) => 
                Vector2.Distance(session.origin, a.transform.position)
                .CompareTo(Vector2.Distance(session.origin, b.transform.position)));

            // [핵심] 가장 가까운 1개의 오브젝트만 타격하여 한 번의 스윙에 여러 벽이 부서지는 현상 방지
            var closestHit = environmentTargets[0];
            
            IDamageable damageable = closestHit.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsInvincible)
            {
                // 이미 맞은 대상인지 체크
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
                    
                    // 중복 히트 방지
                    session.alreadyHit.Add(closestHit.gameObject);
                }
            }
        }
    }
    
    #endregion
    
    #region Stage 2: Validation
    
    /// <summary>
    /// 유효한 대상 필터링 (무적, 중복 등)
    /// </summary>
    private List<ValidatedTarget> ValidateTargets(List<Collider2D> candidates, AttackSession session)
    {
        var validTargets = new List<ValidatedTarget>();
        
        foreach (var col in candidates)
        {
            // 이미 이번 공격에 맞은 대상 스킵
            if (session.alreadyHit.Contains(col.gameObject))
            {
                continue;
            }
            
            // IDamageable 확인
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
            
            // IEnemyReaction 확인 (선택적)
            var reaction = col.GetComponent<IEnemyReaction>();
            
            // 유효 대상 추가
            validTargets.Add(new ValidatedTarget
            {
                gameObject = col.gameObject,
                damageable = damageable,
                reaction = reaction
            });
            
            // 중복 히트 방지
            session.alreadyHit.Add(col.gameObject);
        }
        
        return validTargets;
    }
    
    #endregion
    
    #region Stage 3: Application
    
    /// <summary>
    /// 데미지 적용 및 반응 호출
    /// </summary>
    private HitResult ApplyDamage(List<ValidatedTarget> targets, AttackSession session)
    {
        var result = new HitResult();
        result.hitTargets = new List<GameObject>();
        
        foreach (var target in targets)
        {
            // 최종 데미지 계산 (폼 보정 적용)
            int finalDamage = Mathf.RoundToInt(session.attack.baseDamage * session.damageMultiplier);
            
            // 넉백 방향 계산
            Vector2 knockbackDir;
            
            if (session.attack.knockbackMode == KnockbackMode.RadialFromOrigin)
            {
                // 방사형 (Origin -> Target)
                knockbackDir = (target.gameObject.transform.position - (Vector3)session.origin).normalized;
                
                // Z축 노이즈 제거 및 안전장치
                if (knockbackDir == Vector2.zero)
                {
                    // 위치가 겹치면 랜덤 또는 Facing 방향으로 밀어냄
                    knockbackDir = new Vector2(session.facing, 0);
                }
            }
            else
            {
                // 고정 방향 (Facing 기준)
                knockbackDir = new Vector2(session.facing, 0);
            }
            
            // DamageInfo 생성
            DamageInfo info = new DamageInfo
            {
                damage = finalDamage,
                knockbackForce = session.attack.baseKnockback, // Pass Vector2 directly
                damageSource = session.origin,
                hitDirection = knockbackDir,
                stunDuration = session.attack.stunDuration, // Pass stun duration
                damageType = DamageType.Physical,
                hitType = HitType.Light, // Defaulting for now
                source = session.attacker, // [Fix] Source 할당 (Aggro 시스템 핵심)
                ignoreArmor = false,
                ignoreInvincibility = false,
                canBeParried = true
            };
            
            // 데미지 적용
            target.damageable.TakeDamage(info);
            
            // Reaction 호출 (attacker 포함)
            target.reaction?.OnHitReaction(info, session.attacker);
            
            // 결과 기록
            result.hitCount++;
            result.hitTargets.Add(target.gameObject);
        }
        
        // [New] 플레이어 공격 명중 시 스킬 게이지(소울) 획득
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
/// 공격 세션: 단일 공격의 수명 관리 단위
/// HitResolver는 Stateless, 상태는 Session이 소유
/// </summary>
public class AttackSession
{
    public AttackData attack;
    public Vector2 origin;
    public int facing;  // 1: 오른쪽, -1: 왼쪽
    public LayerMask targetLayer;
    public GameObject attacker;
    
    // 폼 보정값
    public float damageMultiplier = 1f;
    public float rangeMultiplier = 1f;
    
    // 반동(Recoil) 감지용
    public LayerMask recoilTargetLayer;
    public List<SurfaceType> recoilTargetSurfaces;
    
    // 중복 히트 방지 (세션이 소유)
    public HashSet<GameObject> alreadyHit = new HashSet<GameObject>();
    
    // 수명 관리
    public float lifetime;
}

/// <summary>
/// 검증된 대상 정보
/// </summary>
public struct ValidatedTarget
{
    public GameObject gameObject;
    public IDamageable damageable;
    public IEnemyReaction reaction;
}

/// <summary>
/// 적중 결과
/// </summary>
public struct HitResult
{
    public int hitCount;
    public List<GameObject> hitTargets;
    public bool TriggerRecoil;
    
    public bool HasHit => hitCount > 0;
}
