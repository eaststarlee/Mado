using UnityEngine;

/// <summary>
/// 가시(Spike) 위험 지형 컴포넌트 (SurfaceInfo 기반)
///
/// [설정 방법]
/// 1. Collider2D 추가 후 IsTrigger = true 설정
/// 2. SurfaceInfo 컴포넌트 추가 → type = Spike 설정
/// 3. SpikeHazard.cs 추가
/// ※ 별도 레이어 추가 불필요 - 레이어 대신 PlayerHealth 컴포넌트로 플레이어 감지
///
/// [동작]
/// - 폼 무관하게 플레이어가 닿으면 즉시 피격
/// - PlayerHealth 컴포넌트 유무로 플레이어 판별
/// - 쿨다운 기반 연속 피격 제한 (PlayerHealth 무적 시스템과 협력)
/// - 넉백 방향: 가시 중심 → 플레이어 방향으로 자동 계산
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SurfaceInfo))]
public class SpikeHazard : MonoBehaviour
{
    [Header("피해 설정")]
    [Tooltip("피격 시 데미지 (기본 1)")]
    [SerializeField] private int damage = 1;
    [Tooltip("연속 피격 방지 쿨다운 (초). PlayerHealth 무적 시간보다 길게 설정 권장")]
    [SerializeField] private float damageCooldown = 0.5f;

    [Header("넉백 설정")]
    [Tooltip("넉백 힘 (X: 횡방향, Y: 수직방향). hitDirection 기준으로 자동 반영")]
    [SerializeField] private Vector2 knockbackForce = new Vector2(6f, 5f);
    [Tooltip("피격 경직 시간 (초)")]
    [SerializeField] private float stunDuration = 0.15f;

    // 마지막 피격 시각 (쿨다운 계산)
    private float lastDamageTime = -999f;

    #region Unity Lifecycle

    private void Awake()
    {
        // Collider가 Trigger인지 확인 (경고)
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("[SpikeHazard] Collider2D의 IsTrigger가 false입니다. Spike는 Trigger Collider를 사용해야 합니다.", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    #endregion

    #region Damage Logic

    /// <summary>
    /// 플레이어 감지 및 데미지 시도
    /// 레이어 체크 없음 - PlayerHealth 컴포넌트 유무로 플레이어 판별
    /// </summary>
    private void TryDamagePlayer(Collider2D other)
    {
        // PlayerHealth 탐색 (플레이어인지 판별)
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        // 쿨다운 체크
        if (Time.time < lastDamageTime + damageCooldown) return;

        // 무적 중이면 스킵 (단, 쿨다운은 갱신하지 않아 무적 해제 직후 재피격 가능)
        if (playerHealth.IsInvincible) return;

        // 넉백 방향: 가시 중심 → 플레이어 (자동 계산)
        Vector2 spikeCenter = transform.position;
        Vector2 playerPos   = other.transform.position;
        Vector2 hitDir = (playerPos - spikeCenter).normalized;

        // 방향 없는 경우(위치 겹침) 기본값: 위쪽
        if (hitDir == Vector2.zero) hitDir = Vector2.up;

        // 넉백 힘 방향 반영
        Vector2 finalKnockback = new Vector2(
            knockbackForce.x * hitDir.x,
            knockbackForce.y
        );

        // DamageInfo 생성
        DamageInfo info = new DamageInfo
        {
            damage              = this.damage,
            hitPoint            = spikeCenter,
            hitDirection        = hitDir,
            damageSource        = spikeCenter,
            knockbackForce      = finalKnockback,
            stunDuration        = this.stunDuration,
            damageType          = DamageType.Environment,
            hitType             = HitType.Light,
            ignoreInvincibility = false,
            ignoreArmor         = false,
            canBeParried        = false,   // 가시는 패링 불가
            source              = this.gameObject
        };

        // 피격 전달
        playerHealth.TakeDamage(info);

        // 쿨다운 갱신
        lastDamageTime = Time.time;
    }

    #endregion
}
