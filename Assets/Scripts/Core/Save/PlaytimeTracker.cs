using UnityEngine;

/// <summary>
/// 누적 플레이 시간을 메모리에서 관리하는 싱글톤.
/// totalPlayTime을 매 프레임 SaveData에 기록하면 데이터가 항상 dirty 상태가 되므로,
/// 이 트래커가 메모리에서 누적하고 SaveAsync() 시점에만 SaveData에 반영합니다.
/// </summary>
public class PlaytimeTracker : MonoBehaviour
{
    public static PlaytimeTracker Instance { get; private set; }

    /// <summary>현재 세션의 누적 플레이 시간 (초)</summary>
    public float ElapsedSeconds { get; private set; }

    private bool _isTracking;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // 일시정지 상태에서도 실제 경과 시간을 누적 (unscaledDeltaTime)
        if (_isTracking) ElapsedSeconds += Time.unscaledDeltaTime;
    }

    // ── 공개 제어 메서드 ──────────────────────────────────

    /// <summary>슬롯 로드 직후 저장된 플레이 시간으로 초기화합니다.</summary>
    public void SetInitial(float savedSeconds)
    {
        ElapsedSeconds = savedSeconds;
    }

    /// <summary>누적 시작 (BootSequencer가 씬 로드 완료 후 호출)</summary>
    public void StartTracking() => _isTracking = true;

    /// <summary>누적 중단 (메인 메뉴 복귀, 일시정지 등)</summary>
    public void StopTracking() => _isTracking = false;

    /// <summary>누적값 초기화 (새 게임 시작 시)</summary>
    public void ResetTracking()
    {
        ElapsedSeconds = 0f;
        _isTracking = false;
    }
}
