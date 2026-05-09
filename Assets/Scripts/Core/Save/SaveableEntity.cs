using UnityEngine;

/// <summary>
/// World State 시스템에서 상태를 시각적으로 반영(View)하는 베이스 클래스입니다.
/// </summary>
public abstract class SaveableEntity : MonoBehaviour
{
    [SerializeField] 
    protected EntityIdSO entityId;

    protected virtual void Start()
    {
        if (entityId == null)
        {
            Debug.LogWarning($"[SaveableEntity] {gameObject.name} 에 EntityIdSO 가 할당되지 않았습니다.");
            return;
        }

        // GameProgressManager(-100)는 항상 먼저 실행되어 있으므로 안전합니다.
        EntityState state = GameProgressManager.Instance.GetEntityState(entityId);
        ApplyState(state);
    }

    /// <summary>
    /// 로드된 상태에 맞춰 자신의 모습을 초기화/변경합니다.
    /// (예: state.active가 true면 문을 연 모습으로 변경)
    /// </summary>
    protected abstract void ApplyState(EntityState state);

    /// <summary>
    /// 상태가 변경될 때 호출하여 중앙 저장소에 즉시 갱신합니다.
    /// </summary>
    protected void UpdateState(EntityState newState)
    {
        if (entityId == null) return;
        GameProgressManager.Instance.SetEntityState(entityId, newState);
        ApplyState(newState);
    }
}
