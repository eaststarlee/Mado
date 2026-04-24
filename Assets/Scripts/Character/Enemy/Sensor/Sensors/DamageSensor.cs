using UnityEngine;

/// <summary>
/// 피격 시 Aggro를 유지해주는 특수 센서 (Event Driven).
/// EnemyEntity의 HandleHit에서 ReportDamage()를 호출받아 동작.
/// </summary>
public class DamageSensor : SensorBase
{
    [Header("Settings")]
    [SerializeField] private float damageAlertDuration = 5.0f; // 피격 후 추적 유지 시간
    
    private float lastDamageTime = -999f;
    private bool isDamageAggroActive = false;

    // DamageSensor는 Tick에서 시간을 체크하여 플래그를 해제하는 역할만 수행
    protected override void Evaluate(EnemyBlackboard bb)
    {
        if (!isDamageAggroActive) return;

        // 지속 시간 초과 시 플래그 해제
        if (Time.time - lastDamageTime > damageAlertDuration)
        {
            isDamageAggroActive = false;
            bb.Target.source &= ~DetectionSource.Damage;
        }
        else
        {
            // 아직 유효하면 플래그 유지 (매 프레임 켜주는 게 안전)
            bb.Target.source |= DetectionSource.Damage;
        }
    }

    /// <summary>
    /// 외부(EnemyEntity)에서 피격 사실을 알림
    /// </summary>
    /// <summary>
    /// 외부(EnemyEntity)에서 피격 사실을 알림
    /// </summary>
    public void ReportDamage(DamageInfo info, EnemyBlackboard bb)
    {
        if (!isEnabled || info.source == null)
        {
            // Debug.LogWarning($"[DamageSensor] Source is null or disabled. Enabled: {isEnabled}");
            return;
        }

        // Debug.Log($"[DamageSensor] ReportDamage! Source: {info.source.name}");

        lastDamageTime = Time.time;
        isDamageAggroActive = true;

        Transform attackerParams = info.source.transform;
        bb.Target.target = attackerParams;
        bb.Target.source |= DetectionSource.Damage; // 즉시 설정
        
        // 거리/방향 갱신 (중요: 이것이 없으면 ChaseModule이 방향을 못 잡음)
        Vector2 myPos = transform.position;
        Vector2 targetPos = attackerParams.position;
        bb.Target.distance = Vector2.Distance(myPos, targetPos);
        bb.Target.direction = (targetPos - myPos).normalized;
        
        // 메모리 갱신
        bb.Target.lastKnownPosition = targetPos;
        bb.Target.lastDetectedTime = Time.time;
    }
}
