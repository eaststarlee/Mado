using System;
using System.Collections.Generic;

/// <summary>
/// 특수 행동 인터페이스 (Slam, Charge, Parry 등)
/// PlayerActionSystem이 관리하는 단일 Action 단위
/// </summary>
public interface ISpecialAction
{
    /// <summary>
    /// Action 시작
    /// </summary>
    /// <param name="handle">이벤트 구독/해제 관리용 핸들</param>
    void Begin(ActionHandle handle);
    
    /// <summary>
    /// 프레임 업데이트 (PlayerActionSystem.Update에서 호출)
    /// </summary>
    /// <param name="deltaTime">Time.deltaTime</param>
    void Update(float deltaTime);
    
    /// <summary>
    /// 강제 취소 (피격, 폼 변경 등)
    /// Cleanup은 Cancel 내부에서 처리
    /// </summary>
    void Cancel();

    /// <summary>
    /// 입력 잠금 여부 (Action 실행 중 이동/점프 차단)
    /// </summary>
    bool LocksInput { get; }
}

/// <summary>
/// Action 핸들: 이벤트 구독 관리 + 자동 해제
/// Dispose 패턴으로 이벤트 누수 방지
/// </summary>
public class ActionHandle : IDisposable
{
    private List<Action> cleanupActions = new();
    private bool isDisposed;
    
    public bool IsDisposed => isDisposed;
    
    public ActionHandle()
    {
    }
    
    /// <summary>
    /// Action 이벤트 구독 (해제 자동 등록)
    /// </summary>
    public void Subscribe(Action eventToSubscribe, Action unsubscribeAction)
    {
        eventToSubscribe?.Invoke();
        cleanupActions.Add(unsubscribeAction);
    }
    
    /// <summary>
    /// 모든 구독 해제 + 핸들 무효화
    /// </summary>
    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        
        foreach (var cleanup in cleanupActions)
        {
            try { cleanup?.Invoke(); }
            catch { /* 해제 실패 무시 */ }
        }
        cleanupActions.Clear();
    }
    
    /// <summary>
    /// Action 정상 종료 알림
    /// </summary>
    /// <summary>
    /// Action 정상 종료 알림
    /// </summary>
    public void NotifyEnded()
    {
        onEnded?.Invoke();
    }
    
    private Action onEnded;
    
    public void SetOnEndedCallback(Action callback)
    {
        onEnded = callback;
    }
}
