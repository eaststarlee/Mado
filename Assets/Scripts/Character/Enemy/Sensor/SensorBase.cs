using UnityEngine;

/// <summary>
/// 센서 추상 베이스 클래스.
/// Sensor는 Brain 루프에 참여하지 않는다.
/// EnemyEntity.Update() 초반에 실행되어 Blackboard를 갱신만 한다.
/// 규칙: Sensor는 Blackboard에 "쓰기만", Module은 Blackboard를 "읽기만".
/// </summary>
public abstract class SensorBase : MonoBehaviour
{
    /// <summary>
    /// 센서 활성화 여부.
    /// </summary>
    [SerializeField] protected bool isEnabled = true;

    /// <summary>
    /// 센서 체크 주기 (초 단위). 0이면 매 프레임.
    /// </summary>
    [SerializeField] protected float checkInterval = 0.1f;

    protected float timer;

    /// <summary>
    /// 센서 갱신 (EnemyEntity가 호출).
    /// 내부 타이머에 따라 Evaluate 실행.
    /// </summary>
    public void Tick(EnemyBlackboard bb)
    {
        if (!isEnabled) return;

        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            Evaluate(bb);
        }
    }

    /// <summary>
    /// 실제 감지 로직 구현부.
    /// </summary>
    protected abstract void Evaluate(EnemyBlackboard bb);
    
    /// <summary>
    /// 센서 활성화/비활성화.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
    }
}
