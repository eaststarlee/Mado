#if false // TODO: Unused Script (주석 처리됨)
using UnityEngine;

/// <summary>
/// 구역 기반 감지 센서.
/// Trigger Collider를 사용하여 특정 영역 진입을 감지합니다.
/// 보스 아레나, 둥지 방어 등에 사용됩니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ColliderSensor : SensorBase
{

    // ZoneSensor는 이벤트(OnTrigger) 기반이므로 Polling(Tick) 불필요.
    // 하지만 SensorBase를 상속받으므로 Evaluate를 구현해야 함.
    // Evaluate는 빈 상태로 두거나, 안전장치로 활용 가능.
    // 여기서는 Frequency 0으로 설정하여 Tick 오버헤드 최소화 권장.

    private void Awake()
    {
        // 최적화를 위해 Tick 비활성화 (이벤트 기반이므로)
        checkInterval = float.MaxValue; 
    }

    protected override void Evaluate(EnemyBlackboard bb)
    {
        // Trigger 기반이므로 Polling 로직 없음
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var bb = GetComponentInParent<EnemyEntity>()?.Blackboard;
        if (bb == null) return;

        // 플레이어인지 확인 (태그 또는 레이어 기반)
        // 여기서는 간단히 Tag 체크 또는 Layer 체크
        if (other.CompareTag("Player"))
        {
            bb.Target.target = other.transform;
            bb.Target.source |= DetectionSource.Zone;
            bb.Target.lastKnownPosition = other.transform.position; // 진입 위치 기억
            
            // Zone 감지는 즉시 Aggro
            bb.Target.lastDetectedTime = Time.time;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // OnTriggerEnter를 놓칠 경우를 대비하거나,
        // 존 내부에서의 위치 갱신을 위해 사용 가능
        var bb = GetComponentInParent<EnemyEntity>()?.Blackboard;
        if (bb == null) return;
    
        if (other.CompareTag("Player"))
        {
            // 존 내부에 있는 동안 계속 위치 갱신
            // Vision이나 Proximity가 없어도 존 안에 있으면 위치를 앎 (마법적 감지?)
            // 필요 없다면 제거 가능. 여기서는 위치 갱신 지원.
            bb.Target.lastKnownPosition = other.transform.position;
            bb.Target.source |= DetectionSource.Zone; // 안전하게 다시 켜줌
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var bb = GetComponentInParent<EnemyEntity>()?.Blackboard;
        if (bb == null) return;

        if (other.CompareTag("Player"))
        {
            bb.Target.source &= ~DetectionSource.Zone;
        }
    }

    private void OnDisable()
    {
        // 비활성화 시 플래그 강제 해제 (중요 안전장치)
        var bb = GetComponentInParent<EnemyEntity>()?.Blackboard;
        if (bb != null)
        {
             bb.Target.source &= ~DetectionSource.Zone;
        }
    }
}
#endif
