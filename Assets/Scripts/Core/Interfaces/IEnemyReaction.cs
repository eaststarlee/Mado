using UnityEngine;

/// <summary>
/// 적의 피격 반응 규약
/// 공격 시스템과 쌍으로 설계되어 AI 행동 결정에 사용
/// </summary>
public interface IEnemyReaction
{
    /// <summary>
    /// 경직 가능 여부
    /// </summary>
    bool CanBeStaggered { get; }
    
    /// <summary>
    /// 슈퍼아머 보유 (공격 중 경직 무시)
    /// </summary>
    bool HasSuperArmor { get; }
    
    /// <summary>
    /// 넉백 무시 여부
    /// </summary>
    bool IgnoreKnockback { get; }
    
    /// <summary>
    /// 피격 반응 타입
    /// </summary>
    HitReactionType ReactionType { get; }
    
    /// <summary>
    /// 피격 시 호출 (attacker 정보 포함)
    /// </summary>
    /// <param name="info">데미지 정보</param>
    /// <param name="attacker">공격자 GameObject</param>
    void OnHitReaction(DamageInfo info, GameObject attacker);
}

/// <summary>
/// 피격 반응 타입
/// </summary>
public enum HitReactionType
{
    Normal,     // 일반 경직
    Heavy,      // 강한 경직
    Parry,      // 패링 (반격)
    Immune      // 완전 무시
}
