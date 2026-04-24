using UnityEngine;

/// <summary>
/// 테스트용 데미지 딜러 컴포넌트
/// 충돌 시 플레이어에게 데미지를 전달합니다.
/// </summary>
public class DamageDealer : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damage = 1;                    // 데미지 양 (하트 개수)
    [SerializeField] private Vector2 knockbackForce = new Vector2(8f, 6f); // 넉백 힘
    [SerializeField] private float knockbackDuration = 0.2f;               // 넉백 시간 (조작 불가 시간)
    [SerializeField] private DamageType damageType = DamageType.Physical;  // 데미지 타입
    [SerializeField] private HitType hitType = HitType.Light;              // 포이즈/히트 타입 (Light/Heavy)
    [SerializeField] private bool ignoreInvincibility = false;  // 무적 무시 여부
    
    [Header("Hit Direction (Optional)")]
    [SerializeField] private bool useCustomDirection = false;   // 커스텀 방향 사용 여부
    [SerializeField] private Vector2 customHitDirection = Vector2.up; // 커스텀 넉백 방향
    
    [Header("Collision Settings")]
    [SerializeField] private LayerMask targetLayer;             // 추가: 공격 대상 레이어 (아군 오폭 방지)
    [SerializeField] private bool damageOnce = false;           // 한 번만 데미지를 주는지
    [SerializeField] private float damageCooldown = 0.5f;       // 연속 데미지 쿨다운
    
    private float lastDamageTime = -999f;
    private bool hasDealtDamage = false;
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDealDamage(collision.gameObject, collision.GetContact(0).point);
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!damageOnce)
        {
            TryDealDamage(collision.gameObject, collision.GetContact(0).point);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDealDamage(collision.gameObject, collision.transform.position);
    }
    
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!damageOnce)
        {
            TryDealDamage(collision.gameObject, collision.transform.position);
        }
    }
    
    private void TryDealDamage(GameObject target, Vector2 contactPoint)
    {
        // 1. 레이어 체크 (아군 오폭 방지)
        int layerBit = 1 << target.layer;
        if ((targetLayer.value & layerBit) == 0)
        {
            // 타겟 레이어가 아니면 무시
            return;
        }

        // 한 번만 데미지를 주는 경우 체크
        if (damageOnce && hasDealtDamage)
        {
            return;
        }
        
        // 쿨다운 체크
        if (Time.time < lastDamageTime + damageCooldown)
        {
            return;
        }
        
        // IDamageable 인터페이스 확인
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // 데미지 정보 생성
            DamageInfo damageInfo = new DamageInfo
            {
                damage = this.damage,
                hitPoint = contactPoint,
                hitDirection = useCustomDirection ? customHitDirection.normalized : ((Vector2)target.transform.position - contactPoint).normalized,
                knockbackForce = this.knockbackForce, 
                damageSource = this.transform.position,
                stunDuration = this.knockbackDuration, // Use exposed duration
                damageType = this.damageType,
                hitType = this.hitType,
                ignoreInvincibility = this.ignoreInvincibility,
                source = this.gameObject,
                canBeParried = true
            };
            
            // 데미지 전달
            damageable.TakeDamage(damageInfo);
            
            // 상태 업데이트
            lastDamageTime = Time.time;
            hasDealtDamage = true;
        }
    }
    
    /// <summary>
    /// 데미지 딜러 리셋 (재사용 가능하게)
    /// </summary>
    public void ResetDamageDealer()
    {
        hasDealtDamage = false;
        lastDamageTime = -999f;
    }
}
