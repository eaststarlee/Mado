/// <summary>
/// 세이브 시스템에 자신의 상태를 저장/복원할 수 있는 컴포넌트 인터페이스.
///
/// ■ 타이밍 계약 (반드시 준수):
///   - Awake() 에서 SaveManager.Instance.Register(this) 호출
///   - OnDestroy() 에서 SaveManager.Instance?.Unregister(this) 호출
///   - OnLoad는 씬 로드 완료 후 모든 Start()가 끝난 시점에 브로드캐스트됩니다.
///     (BootSequencer → WaitForEndOfFrame → BroadcastLoad 순서 보장)
///
/// ■ 적용 대상:
///   - PlayerController, PlayerHealth, PlayerSkillResource
///   - 수집품, 문 상태 등 씬별 영구 상태를 가진 오브젝트
/// </summary>
public interface ISaveable
{
    /// <summary>현재 컴포넌트 상태를 SaveData에 기록합니다.</summary>
    void OnSave(SaveData data);

    /// <summary>SaveData에서 이 컴포넌트의 상태를 복원합니다.</summary>
    void OnLoad(SaveData data);
}
