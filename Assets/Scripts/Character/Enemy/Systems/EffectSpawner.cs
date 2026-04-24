using UnityEngine;

/// <summary>
/// 이펙트 스폰 시스템.
/// 피격/사망/공격 이펙트를 생성하고 관리.
/// Module → EffectSpawner API로 이펙트 요청.
/// </summary>
public class EffectSpawner
{
    private EnemyEntity entity;
    
    public EffectSpawner(EnemyEntity entity)
    {
        this.entity = entity;
    }
    
    /// <summary>
    /// 이펙트 타입별 스폰.
    /// </summary>
    public void SpawnEffect(EffectType type, Vector2 position)
    {
        SpawnEffect(type, position, Quaternion.identity);
    }
    
    /// <summary>
    /// 이펙트 스폰 (회전 포함).
    /// </summary>
    public void SpawnEffect(EffectType type, Vector2 position, Quaternion rotation)
    {
        GameObject prefab = GetEffectPrefab(type);
        if (prefab == null) return;
        
        var instance = Object.Instantiate(prefab, position, rotation);
        
        // 자동 삭제 (ParticleSystem 완료 후)
        var ps = instance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            Object.Destroy(instance, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            // ParticleSystem이 없으면 3초 후 삭제
            Object.Destroy(instance, 3f);
        }
    }
    
    /// <summary>
    /// 사망 이펙트 스폰. DeathModule에서 호출.
    /// VisualProfile.deathEffect 사용.
    /// </summary>
    public void SpawnDeathEffect()
    {
        var visual = entity.Definition?.VisualSettings;
        if (visual?.deathEffect == null) return;
        
        Object.Instantiate(visual.deathEffect, entity.transform.position, Quaternion.identity);
    }
    
    /// <summary>
    /// 피격 이펙트 스폰. HitReactionModule에서 호출 가능.
    /// </summary>
    public void SpawnHitEffect(Vector2 hitPoint, Vector2 hitDirection)
    {
        SpawnEffect(EffectType.Hit, hitPoint);
    }
    
    /// <summary>
    /// 이펙트 타입에 따른 프리팹 반환.
    /// 향후 VisualProfile에 이펙트 프리팹 추가 시 확장.
    /// </summary>
    private GameObject GetEffectPrefab(EffectType type)
    {
        var visual = entity.Definition?.VisualSettings;
        if (visual == null) return null;
        
        switch (type)
        {
            case EffectType.Death:
                return visual.deathEffect;
            default:
                return null;
        }
    }
}

/// <summary>
/// 이펙트 종류.
/// </summary>
public enum EffectType
{
    Hit,
    Death,
    Attack,
    Dash,
    Stun
}
