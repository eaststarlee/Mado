/// <summary>
/// 모듈 데이터 마커 인터페이스.
/// 모든 ModuleData ScriptableObject가 구현해야 하는 인터페이스.
/// Module은 이 인터페이스를 통해 SO 데이터를 참조한다 (읽기 전용).
/// </summary>
public interface IModuleData
{
    /// <summary>
    /// 이 데이터가 속한 모듈 이름 (디버그용).
    /// </summary>
    string ModuleName { get; }
}
