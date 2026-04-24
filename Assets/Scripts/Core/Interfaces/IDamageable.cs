/// <summary>
/// 데미지를 받을 수 있는 객체의 인터페이스
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 데미지를 받는 메서드
    /// </summary>
    /// <param name="damageInfo">데미지 정보</param>
    void TakeDamage(DamageInfo damageInfo);

    /// <summary>
    /// 현재 무적 상태 여부
    /// </summary>
    bool IsInvincible { get; }
}
