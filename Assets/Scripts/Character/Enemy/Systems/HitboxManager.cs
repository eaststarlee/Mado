using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 히트박스 관리 시스템.
/// Module은 HitboxManager에 명령을 발행하고,
/// HitboxManager가 OverlapBox로 직접 판정을 수행.
/// 적 전용 — 플레이어의 HitResolver 파이프라인과 분리.
/// </summary>
public class HitboxManager
{
    private EnemyEntity entity;
    
    // 히트박스 정의
    private Dictionary<string, HitboxDefinition> hitboxes = new Dictionary<string, HitboxDefinition>();
    
    // 현재 활성 히트박스
    private HashSet<string> activeHitboxes = new HashSet<string>();
    
    // 활성 히트박스 별 DamageInfo
    private Dictionary<string, DamageInfo> activeDamageInfos = new Dictionary<string, DamageInfo>();
    
    // 중복 판정 방지
    private Dictionary<string, HashSet<GameObject>> alreadyHit = new Dictionary<string, HashSet<GameObject>>();
    
    public HitboxManager(EnemyEntity entity)
    {
        this.entity = entity;
    }
    
    /// <summary>
    /// 히트박스 정의 등록.
    /// </summary>
    public void RegisterHitbox(string id, HitboxDefinition definition)
    {
        hitboxes[id] = definition;
    }
    
    /// <summary>
    /// 히트박스 활성화 요청.
    /// </summary>
    public void RequestEnable(string hitboxId, DamageInfo damageInfo)
    {
        if (!hitboxes.ContainsKey(hitboxId))
        {
            Debug.LogWarning($"[HitboxManager] 히트박스 '{hitboxId}' 미등록");
            return;
        }
        
        activeHitboxes.Add(hitboxId);
        activeDamageInfos[hitboxId] = damageInfo;
        
        if (!alreadyHit.ContainsKey(hitboxId))
            alreadyHit[hitboxId] = new HashSet<GameObject>();
        else
            alreadyHit[hitboxId].Clear();
    }
    
    /// <summary>
    /// 히트박스 비활성화.
    /// </summary>
    public void RequestDisable(string hitboxId)
    {
        activeHitboxes.Remove(hitboxId);
        activeDamageInfos.Remove(hitboxId);
        if (alreadyHit.ContainsKey(hitboxId))
            alreadyHit[hitboxId].Clear();
    }
    
    /// <summary>
    /// 모든 히트박스 비활성화 (안전장치).
    /// </summary>
    public void DisableAll()
    {
        activeHitboxes.Clear();
        activeDamageInfos.Clear();
        foreach (var set in alreadyHit.Values) set.Clear();
    }
    
    /// <summary>
    /// 매 프레임 활성 히트박스 판정 실행.
    /// EnemyEntity.Update()에서 호출.
    /// </summary>
    public void Tick()
    {
        if (activeHitboxes.Count == 0) return;
        
        // activeHitboxes를 복사 (iteration 중 수정 방지)
        var activeList = new List<string>(activeHitboxes);
        
        foreach (var hitboxId in activeList)
        {
            if (!hitboxes.ContainsKey(hitboxId)) continue;
            if (!activeDamageInfos.ContainsKey(hitboxId)) continue;
            
            var def = hitboxes[hitboxId];
            var damageInfo = activeDamageInfos[hitboxId];
            
            // 히트박스 위치 계산 (적 위치 + 오프셋 * 방향)
            Vector2 center = (Vector2)entity.transform.position + 
                             new Vector2(def.offset.x * entity.Motor.FacingDirection, def.offset.y);
            
            // OverlapBox로 대상 검출
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, def.size, 0f, def.targetLayer);
            
            foreach (var hit in hits)
            {
                if (hit.gameObject == entity.gameObject) continue; // 자기 자신 제외
                
                // 중복 판정 방지
                if (alreadyHit.ContainsKey(hitboxId) && alreadyHit[hitboxId].Contains(hit.gameObject))
                    continue;
                
                // IDamageable 체크
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null && !damageable.IsInvincible)
                {
                    // DamageInfo 갱신 (히트 포인트)
                    var info = damageInfo;
                    info.hitPoint = hit.transform.position;
                    info.hitDirection = new Vector2(entity.Motor.FacingDirection, 0f);
                    
                    damageable.TakeDamage(info);
                    
                    // 중복 방지 등록
                    if (alreadyHit.ContainsKey(hitboxId))
                        alreadyHit[hitboxId].Add(hit.gameObject);
                }
            }
        }
    }
    
    /// <summary>
    /// 히트박스 활성 여부 확인.
    /// </summary>
    public bool IsActive(string hitboxId)
    {
        return activeHitboxes.Contains(hitboxId);
    }
}

/// <summary>
/// 히트박스 형상 정의.
/// </summary>
[System.Serializable]
public struct HitboxDefinition
{
    public Vector2 offset;
    public Vector2 size;
    public LayerMask targetLayer;
}
